using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NBagOfTricks;
using NBagOfUis;
using NAudio.Midi;

// FUTURE solo/mute individual drums.

namespace MidiStyleExplorer
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Indicates whether or not the midi is playing.</summary>
        bool _running = false;

        /// <summary>Midi events from the input file.</summary>
        MidiFile _mfile = new();

        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        int _drumChannel = MidiDefs.DEFAULT_DRUM_CHANNEL;

        /// <summary>All the channel controls and data for current pattern.</summary>
        readonly List<(ChannelControl control, ChannelEvents events)> _channels = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = MiscUtils.GetAppDataDir("MidiStyleExplorer", "Ephemera");
            DirectoryInfo di = new(appDir);
            di.Create();
            Common.Settings = UserSettings.Load(appDir);

            InitializeComponent();
            toolStrip1.Renderer = new TsRenderer() { SelectedColor = Common.Settings.ControlColor };
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            Icon = Properties.Resources.Morso;

            // Init main form from settings
            Location = new Point(Common.Settings.MainFormInfo.X, Common.Settings.MainFormInfo.Y);
            Size = new Size(Common.Settings.MainFormInfo.Width, Common.Settings.MainFormInfo.Height);
            WindowState = FormWindowState.Normal;
            KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown
            Text = $"Clip Explorer {MiscUtils.GetVersionString()} - No file loaded";

            // The text output.
            txtViewer.WordWrap = true;
            txtViewer.BackColor = Color.Cornsilk;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN:", Color.Plum);
            txtViewer.Font = new Font("Lucida Console", 9);

            // Other UI configs.
            chkDrumsOn1.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            chkLogMidi.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            chkPlay.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            btnAutoplay.Checked = Common.Settings.Autoplay;
            btnLoop.Checked = Common.Settings.Loop;
            sldVolume.Value = Common.Settings.Volume;
            sldVolume.DrawColor = Common.Settings.ControlColor;
            sldTempo.DrawColor = Common.Settings.ControlColor;
            sldTempo.Value = Common.Settings.DefaultTempo;

            // Time controller.
            barBar.BeatsPerBar = Common.BEATS_PER_BAR;
            barBar.SubdivsPerBeat = Common.PPQ;
            barBar.Snap = Common.Settings.Snap;
            barBar.ProgressColor = Common.Settings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Figure out the midi output device.
            for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
            {
                if (Common.Settings.MidiOutDevice == MidiOut.DeviceInfo(devindex).ProductName)
                {
                    _midiOut = new MidiOut(devindex);
                    break;
                }
            }
            if (_midiOut is null)
            {
                MessageBox.Show($"Invalid midi device: {Common.Settings.MidiOutDevice}");
            }

            LogMessage("INF", "C to clear, W to wrap");
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Stop();
            SaveSettings();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Resources.
            _midiOut?.Dispose();
            _midiOut = null;

            _mmTimer.Stop();
            _mmTimer.Dispose();

            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region User settings
        /// <summary>
        /// Collect and save user settings.
        /// </summary>
        void SaveSettings()
        {
            Common.Settings.Volume = sldVolume.Value;
            Common.Settings.MainFormInfo = new Rectangle(Location.X, Location.Y, Width, Height);
            Common.Settings.Autoplay = btnAutoplay.Checked;
            Common.Settings.Loop = btnLoop.Checked;

            Common.Settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "User Settings",
                Size = new Size(450, 450),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            PropertyGrid pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = Common.Settings
            };

            // Detect changes of interest.
            bool restart = false;

            pg.PropertyValueChanged += (sdr, args) =>
            {
                restart |= args.ChangedItem.PropertyDescriptor.Name.EndsWith("Device");
            };

            f.Controls.Add(pg);
            f.ShowDialog();

            // Figure out what changed - each handled differently.
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            barBar.Snap = Common.Settings.Snap;

            SaveSettings();
        }
        #endregion

        #region Info
        /// <summary>
        /// All about me.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            Tools.MarkdownToHtml(File.ReadAllLines(@".\README.md").ToList(), "lightcyan", "helvetica", true);
        }

        /// <summary>
        /// Something you should know.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMessage(string cat, string msg)
        {
            int catSize = 3;
            cat = cat.Length >= catSize ? cat.Left(catSize) : cat.PadRight(catSize);

            // May come from a different thread.
            this.InvokeIfRequired(_ =>
            {
                string s = $"{DateTime.Now:mm\\:ss\\.fff} {cat} {msg}";
                txtViewer.AddLine(s);
            });
        }
        #endregion

        #region File handling
        /// <summary>
        /// Organize the file menu item drop down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void File_DropDownOpening(object? sender, EventArgs e)
        {
            fileDropDownButton.DropDownItems.Clear();

            // Always:
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Open...", null, Open_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Dump...", null, Dump_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export...", null, Export_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripSeparator());

            Common.Settings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f, null, new EventHandler(Recent_Click));
                fileDropDownButton.DropDownItems.Add(menuItem);
            });
        }

        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object? sender, EventArgs e)
        {
            if(sender is not null)
            {
                string fn = sender.ToString()!;
                OpenFile(fn);
            }
        }

        /// <summary>
        /// Allows the user to select a midi or style from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            string sext = "Clip Files | *.mid;*.sty";

            using OpenFileDialog openDlg = new()
            {
                Filter = sext,
                Title = "Select a file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// Common file opener.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        /// <returns>Status.</returns>
        public void OpenFile(string fn)
        {
            Stop();

            LogMessage("INF", $"Opening file: {fn}");

            try
            {
                Rewind();

                // Clean out old.
                foreach (var (control, events) in _channels)
                {
                    Controls.Remove(control);
                }
                _channels.Clear();

                // Process the file.
                _mfile = new MidiFile { IgnoreNoisy = true };
                _mfile.Read(fn);
                // All channel numbers.
                var channels = _mfile.AllEvents.Select(e => e.Channel).Distinct().OrderBy(e => e);

                // Make new controls and data holders.
                int x = barBar.Left;
                int y = lbPatterns.Top;
                foreach(var ch in channels)
                {
                    ChannelControl ctrl = new()
                    {
                        ChannelNumber = ch + 1,
                        Location = new(x, y)
                    };

                    ctrl.ChannelChange += ChannelChange;
                    Controls.Add(ctrl);
                    _channels.Add((ctrl, new()));

                    if(_channels.Count % 8 == 0)
                    {
                        x = ctrl.Right + 5;
                        y = lbPatterns.Top;
                    }
                    else
                    {
                        y += ctrl.Height + 5;
                    }
                }

                // Init new stuff with contents of file/pattern.
                if (_mfile.Filename.EndsWith(".mid"))
                {
                    var pinfo = _mfile.Patterns[0];
                    GetPatternEvents(pinfo);
                }
                else // .sty
                {
                    lbPatterns.Items.Clear();
                    foreach (var p in _mfile.Patterns)
                    {
                        switch (p.Name)
                        {
                            case "SFF1": // patches are in here
                            case "SFF2":
                            case "SInt":
                                break;

                            case "":
                                LogMessage("ERR", "Well, this should never happen!");
                                break;

                            default:
                                lbPatterns.Items.Add(p.Name);
                                break;
                        }
                    }

                    if (lbPatterns.Items.Count > 0)
                    {
                        var pinfo = GetPatternInfo(lbPatterns.Items[0].ToString()!);
                        GetPatternEvents(pinfo!);
                    }
                }

                Text = $"MidiStyleExplorer {MiscUtils.GetVersionString()} - {fn}";
                Common.Settings.RecentFiles.UpdateMru(fn);

                Rewind();
                if (btnAutoplay.Checked)
                {
                    Play();
                }
            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"Couldn't open the file: {fn} because: {ex.Message}");
                Text = $"MidiStyleExplorer {MiscUtils.GetVersionString()} - No file loaded";
            }
        }
        #endregion

        #region Transport control
        /// <summary>
        /// Internal handler.
        /// </summary>
        /// <returns></returns>
        bool Play()
        {
            this.InvokeIfRequired(_ =>
            {
                // Start or restart?
                if (!_running)
                {
                    // Convert tempo to period.
                    MidiTime mt = new()
                    {
                        InternalPpq = Common.PPQ,
                        MidiPpq = _mfile.DeltaTicksPerQuarterNote,
                        Tempo = sldTempo.Value
                    };

                    // Create periodic timer.
                    double period = mt.RoundedInternalPeriod();
                    _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
                    _mmTimer.Start();

                    _running = true;
                }
                else
                {
                    Rewind();
                }

                SetPlayCheck(true);
            });

            return true;
        }

        /// <summary>
        /// Internal handler.
        /// </summary>
        /// <returns></returns>
        bool Stop()
        {
            _mmTimer.Stop();
            _running = false;
            // Send midi stop all notes just in case.
            KillAll();
            SetPlayCheck(false);
            return true;
        }

        /// <summary>
        /// Need to temporarily suppress CheckedChanged event.
        /// </summary>
        /// <param name="on"></param>
        void SetPlayCheck(bool on)
        {
            chkPlay.CheckedChanged -= Play_CheckedChanged;
            chkPlay.Checked = on;
            chkPlay.CheckedChanged += Play_CheckedChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Play_CheckedChanged(object? sender, EventArgs e)
        {
            var _ = chkPlay.Checked ? Play() : Stop();
        }

        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    // Toggle.
                    bool _ = chkPlay.Checked ? Stop() : Play();
                    e.Handled = true;
                    break;

                case Keys.C:
                    txtViewer.Clear();
                    e.Handled = true;
                    break;

                case Keys.W:
                    txtViewer.WordWrap = !txtViewer.WordWrap;
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rewind_Click(object? sender, EventArgs e)
        {
            Rewind();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Rewind()
        {
            this.InvokeIfRequired(_ =>
            {
                Stop();
                barBar.Current = BarSpan.Zero;
            });
        }
        #endregion

        #region Midi I/O
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            if (_running)
            {
                // Any soloes?
                bool solo = _channels.Where(c => c.control.State == PlayState.Solo).Any();

                // Process each channel.
                foreach (var (control, events) in _channels)
                {
                    if (events.Valid)
                    {
                        // Look for events to send.
                        if (control.State == PlayState.Solo || (!solo && control.State == PlayState.Normal))
                        {
                            // Process any sequence steps.
                            if (events.MidiEvents.ContainsKey(barBar.Current.TotalSubdivs))
                            {
                                foreach (var mevt in events.MidiEvents[barBar.Current.TotalSubdivs])
                                {
                                    switch (mevt)
                                    {
                                        case NoteOnEvent evt:
                                            if (control.ChannelNumber == _drumChannel && evt.Velocity == 0)
                                            {
                                                // Skip drum noteoffs as windows GM doesn't like them.
                                            }
                                            else
                                            {
                                                // Adjust volume and maybe drum channel. Also NAudio NoteLength bug.
                                                NoteOnEvent ne = new(
                                                    evt.AbsoluteTime,
                                                    control.ChannelNumber == _drumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                                    evt.NoteNumber,
                                                    (int)(evt.Velocity * sldVolume.Value),
                                                    evt.OffEvent is null ? 0 : evt.NoteLength);

                                                MidiSend(ne);
                                            }
                                            break;

                                        case NoteEvent evt:
                                            if (control.ChannelNumber == _drumChannel)
                                            {
                                                // Skip drum noteoffs as windows GM doesn't like them.
                                            }
                                            else
                                            {
                                                MidiSend(evt);
                                            }
                                            break;

                                        default:
                                            MidiSend(mevt);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Bump time. Check for end of play.
                if (barBar.IncrementCurrent(1))
                {
                    if (btnLoop.Checked)
                    {
                        Play();
                    }
                    else
                    {
                        _running = false;
                        Rewind();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void MidiSend(MidiEvent evt)
        {
            _midiOut?.Send(evt.GetAsShortMessage());

            if (chkLogMidi.Checked)
            {
                LogMessage("MIDI_SEND", evt.ToString());
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channel"></param>
        void Kill(int channel)
        {
            ControlChangeEvent nevt = new(0, channel + 1, MidiController.AllNotesOff, 0);
            MidiSend(nevt);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        void KillAll()
        {
            // Send midi stop all notes just in case.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Kill(i);
            }
        }
        #endregion

        #region Misc handlers
        /// <summary>
        /// User changed tempo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Tempo_ValueChanged(object? sender, EventArgs e)
        {
            if (_running)
            {
                Stop();
                Play();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e) // TODO
        {
        }

        /// <summary>
        /// Sometimes drums are on channel 1, usually if it's the only channel in a clip file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumsOn1_CheckedChanged(object? sender, EventArgs e)
        {
            _drumChannel = chkDrumsOn1.Checked ? 1 : MidiDefs.DEFAULT_DRUM_CHANNEL;
            // Update UI.
            _channels.ForEach(ch => ch.control.IsDrums = ch.control.ChannelNumber == _drumChannel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Kill_Click(object? sender, EventArgs e)
        {
            KillAll();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = GetPatternInfo(lbPatterns.SelectedItem.ToString()!);
            GetPatternEvents(pinfo!);

            Rewind();
            if (btnAutoplay.Checked)
            {
                Play();
            }
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case PlayState.Normal:
                        break;

                    case PlayState.Solo:
                        // Mute any other non-solo channels.
                        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                        {
                            if (i != chc.ChannelNumber && chc.State != PlayState.Solo)
                            {
                                Kill(i);
                            }
                        }
                        break;

                    case PlayState.Mute:
                        Kill(chc.ChannelNumber);
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                PatchChangeEvent evt = new(0, chc.ChannelNumber, chc.Patch);
                MidiSend(evt);
            }
        }
        #endregion

        #region Process patterns and events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pname"></param>
        /// <returns></returns>
        public PatternInfo? GetPatternInfo(string pname)
        {
            var ret = _mfile.Patterns.Where(p => p.Name == pname).First();
            return ret;
        }

        /// <summary>
        /// Get midi file contents for this pattern and convert into the internal format for playing.
        /// </summary>
        /// <param name="pinfo"></param>
        void GetPatternEvents(PatternInfo pinfo)
        {
            // Quiet.
            KillAll();

            // Scale to time increments used by application.
            MidiTime mt = new()
            {
                InternalPpq = Common.PPQ,
                MidiPpq = _mfile.DeltaTicksPerQuarterNote
            };

            // Update patches.
            foreach (var (control, events) in _channels)
            {
                // Patches.
                control.Patch = pinfo.Patches[control.ChannelNumber];
                if (control.Patch != -1)
                {
                    PatchChangeEvent evt = new(0, control.ChannelNumber + 1, control.Patch);
                    MidiSend(evt);
                }
            }

            // Process pattern events.
            foreach (var (control, events) in _channels)
            {
                events.Reset();

                var evts = _mfile.AllEvents.Where(e => e.Pattern == pinfo.Name && e.Channel == control.ChannelNumber).OrderBy(e => e.AbsoluteTime);

                foreach (var evt in evts)
                {
                    int scaledTime = mt.MidiToInternal(evt.AbsoluteTime);
                    events.Add(scaledTime, evt.MidiEvent);
                }
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Dump current file.
        /// </summary>
        void Dump_Click(object? sender, EventArgs e)
        {
            //_mfile.DrumChannel = _drumChannel;
            //return _mfile.GetReadableGrouped();
            var ds = _mfile.GetReadableContents();

            if (ds.Count == 0)
            {
                ds.Add("No data");
            }

            if (Common.Settings.DumpToClip)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, ds));
                LogMessage("INF", "File dumped to clipboard");
            }
            else
            {
                using SaveFileDialog dumpDlg = new() { Title = "Dump to file", FileName = "dump.csv" };
                if (dumpDlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllLines(dumpDlg.FileName, ds.ToArray());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Export_Click(object? sender, EventArgs e)
        {
            string dir = Path.GetDirectoryName(_mfile.Filename)!;
            string newfn = Path.GetFileNameWithoutExtension(_mfile.Filename);
            string pattern;
            string info;

            if (_mfile.Filename.EndsWith(".sty"))
            {
                pattern = lbPatterns.SelectedItem.ToString()!.Replace(' ', '_');
                newfn = $"{newfn}_{pattern}.mid";
                info = $"Export {pattern} from {_mfile.Filename}";
            }
            else // .mid
            {
                pattern = "";
                newfn = $"{newfn}_export.mid";
                info = $"Export from {_mfile.Filename}";
            }

            using SaveFileDialog dumpDlg = new() { Title = "Export midi", FileName = newfn, InitialDirectory = dir };
            if (dumpDlg.ShowDialog() == DialogResult.OK)
            {
                ExportMidi(newfn, pattern, info);
            }
        }

        /// <summary>
        /// Output part or all of the file to a new midi file.
        /// </summary>
        /// <param name="fn">Where to put the midi.</param>
        /// <param name="pattern">Specific pattern if a style file.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        void ExportMidi(string fn, string pattern, string info) //TODO probably find a new home with more editing features.  
        {
            // Get pattern info. This should work for the simple midi file case too.
            PatternInfo pinfo = _mfile.Patterns.First(p => p.Name == pattern);

            // Init output file contents.
            MidiEventCollection outColl = new(1, _mfile.DeltaTicksPerQuarterNote);
            IList<MidiEvent> outEvents = outColl.AddTrack();
            
            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pinfo.Tempo });

            // General info.
            outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pinfo.TimeSig != "")
            {
                //mevents.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, 8));
            }
            if (pinfo.KeySig != "")
            {
                //mevents.Add(new KeySignatureEvent(0, 0, 0));
            }

            // Patches.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if (pinfo.Patches[i] != -1)
                {
                    outEvents.Add(new PatchChangeEvent(0, i + 1, pinfo.Patches[i]));
                }
            }

            // Combine the midi events for current pattern ordered by timestamp.
            List<MidiEvent> allEvts = new();
            foreach (var (control, events) in _channels)
            {
                events.MidiEvents.ForEach(kv =>
                {
                    kv.Value.ForEach(e =>
                    {
                        e.AbsoluteTime = kv.Key;
                        e.Channel = control.ChannelNumber;
                        allEvts.Add(e);
                    });
                });

                foreach (var channelEventTime in events.MidiEvents)
                {
                    var theEvents = channelEventTime.Value;
                    foreach (var outEvent in theEvents)
                    {
                        outEvent.AbsoluteTime = channelEventTime.Key;
                        outEvent.Channel = control.ChannelNumber;
                        allEvts.Add(outEvent);
                    }
                }
            }

            // Copy to output.
            allEvts.OrderBy(e => e.AbsoluteTime).ForEach(e =>
            {
                outEvents.Add(e);
            });

            // End track.
            long ltime = allEvts.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            NAudio.Midi.MidiFile.Export(fn, outColl);
        }
        #endregion
    }
}
