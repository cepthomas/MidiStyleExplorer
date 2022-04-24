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


namespace MidiStyleExplorer
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiFile _mfile = new();

        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        int _drumChannel = MidiDefs.DEFAULT_DRUM_CHANNEL;

        /// <summary>All the channel controls and data for current pattern.</summary>
        readonly List<(ChannelControl control, ChannelEvents events)> _channels = new();

        /// <summary>Supported file types in OpenFileDialog form.</summary>
        readonly string _fileTypes = "Style Files|*.sty;*.pcs;*.sst;*.prs|Midi Files|*.mid";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            Icon = Properties.Resources.Morso;

            // Get settings.
            string appDir = MiscUtils.GetAppDataDir("MidiStyleExplorer", "Ephemera");
            Common.Settings = (UserSettings)Settings.Load(appDir, typeof(UserSettings));

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = Common.Settings.ControlColor };

            // Init main form from settings
            Location = new Point(Common.Settings.FormGeometry.X, Common.Settings.FormGeometry.Y);
            Size = new Size(Common.Settings.FormGeometry.Width, Common.Settings.FormGeometry.Height);
            WindowState = FormWindowState.Normal;
            KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown
            Text = $"Clip Explorer {MiscUtils.GetVersionString()} - No file loaded";

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN:", Color.Plum);

            // Toolbar configs.
            btnAutoplay.Checked = Common.Settings.Autoplay;
            btnLoop.Checked = Common.Settings.Loop;

            // Other UI configs.
            chkPlay.FlatAppearance.CheckedBackColor = Common.Settings.ControlColor;
            sldVolume.Value = Common.Settings.Volume;
            sldVolume.DrawColor = Common.Settings.ControlColor;
            sldTempo.DrawColor = Common.Settings.ControlColor;
            sldTempo.Value = Common.Settings.DefaultTempo;

            // Time controller.
            barBar.ZeroBased = Common.Settings.ZeroBased;
            barBar.BeatsPerBar = Common.BEATS_PER_BAR;
            barBar.SubdivsPerBeat = Common.PPQ;
            barBar.Snap = Common.Settings.Snap;
            barBar.ProgressColor = Common.Settings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Initialize tree from user settings.
            ftree.FilterExts = _fileTypes.SplitByTokens("|;*").Where(s => s.StartsWith(".")).ToList();
            ftree.RootDirs = Common.Settings.RootDirs;
            ftree.SingleClickSelect = true; // not !Common.Settings.Autoplay;

            try
            {
                ftree.Init();
            }
            catch (DirectoryNotFoundException)
            {
                LogMessage("WRN", "No tree directories");
            }

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

            // Hook up UI handlers.
            chkPlay.CheckedChanged += (_, __) => { _ = chkPlay.Checked ? Play() : Stop(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnDrumsOn1.CheckedChanged += (_, __) => {
                _drumChannel = btnDrumsOn1.Checked ? 1 : MidiDefs.DEFAULT_DRUM_CHANNEL;
                _channels.ForEach(ch => ch.control.IsDrums = ch.control.ChannelNumber == _drumChannel); };

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            chkPlay.Checked = false; // ==> stop
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

            if (disposing && (components is not null))
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
            Common.Settings.FormGeometry = new Rectangle(Location.X, Location.Y, Width, Height);
            Common.Settings.Volume = sldVolume.Value;
            Common.Settings.Autoplay = btnAutoplay.Checked;
            Common.Settings.Loop = btnLoop.Checked;
            Common.Settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = Common.Settings.Edit("User Settings");

            // Detect changes of interest.
            bool restart = false;

            // Figure out what changed - each handled differently.
            foreach (var (name, cat) in changes)
            {
                restart |= name == "MidiOutDevice" || name == "ControlColor" || name == "RootDirs" || name == "ZeroBased";
            }

            // Figure out what changed.
            if (restart)
            {
                MessageBox.Show("Restart required for changes to take effect");
            }

            // Benign changes.
            barBar.Snap = Common.Settings.Snap;
            btnLoop.Checked = Common.Settings.Loop;

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
                // string s = $"{DateTime.Now:mm\\:ss\\.fff} {cat} {msg}";
                string s = $"> {cat} {msg}";
                txtViewer.AppendLine(s);
            });
        }
        #endregion

        #region File handling
        /// <summary>
        /// Tree has seleccted a file to play.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fn"></param>
        void Navigator_FileSelectedEvent(object? sender, string fn)
        {
            OpenFile(fn);
        }

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
            using OpenFileDialog openDlg = new()
            {
                Filter = _fileTypes,
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
            chkPlay.Checked = false; // ==> stop

            LogMessage("INF", $"Opening file: {fn}");

            try
            {
                // Process the file.
                _mfile = new();
                _mfile.Read(fn);

                // All channel numbers.
                //var channels = _mfile.AllEvents.Select(e => e.Channel).Distinct().OrderBy(e => e);

                // Init new stuff with contents of file/pattern.
                lbPatterns.Items.Clear();
                if (_mfile.Filename.ToLower().EndsWith(".mid"))
                {
                    var pinfo = _mfile.Patterns[0];
                    LoadPattern(pinfo);
                }
                else // .sty
                {
                    foreach (var p in _mfile.Patterns)
                    {
                        switch (p.Name)
                        {
                            case "SFF1": // initial patches are in here
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
                        lbPatterns.SelectedIndex = 0;
                    }
                }

                Text = $"MidiStyleExplorer {MiscUtils.GetVersionString()} - {fn} File Type:{_mfile.MidiFileType} Tracks:{_mfile.Tracks} PPQ:{_mfile.DeltaTicksPerQuarterNote}";
                Common.Settings.RecentFiles.UpdateMru(fn);

                Rewind();
                if (btnAutoplay.Checked)
                {
                    chkPlay.Checked = true; // ==> play
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
                if (!_mmTimer.Running)
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
                }
                else
                {
                    Rewind();
                }
            });

            return true;
        }

        /// <summary>
        /// Internal handler.
        /// </summary>
        /// <returns></returns>
        bool Stop()
        {
            // Might come from another thread.
            this.InvokeIfRequired(_ =>
            {
                _mmTimer.Stop();
                // Send midi stop all notes just in case.
                KillAll();
            });

            return true;
        }

        /// <summary>
        /// Wrapper for cross-thread.
        /// </summary>
        /// <returns></returns>
        bool StopReq()
        {
            // Might come from another thread.
            this.InvokeIfRequired(_ =>
            {
                chkPlay.Checked = false;
            });

            return true;
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
                    chkPlay.Checked = !chkPlay.Checked;
                    e.Handled = true;
                    break;

                case Keys.C:
                    if(e.Modifiers == 0)
                    {
                        txtViewer.Clear();
                        e.Handled = true;
                    }
                    break;

                case Keys.W:
                    if (e.Modifiers == 0)
                    {
                        txtViewer.WordWrap = !txtViewer.WordWrap;
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Go back Jack.
        /// </summary>
        public void Rewind()
        {
            // Might come from another thread.
            this.InvokeIfRequired(_ =>
            {
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
            if (_mmTimer.Running)
            {
                // Any soloes?
                bool solo = _channels.Where(c => c.control.State == PlayState.Solo).Any();

                // Process each channel.
                foreach (var (control, events) in _channels)
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
                                            // Adjust volume and maybe drum channel.
                                            NoteOnEvent ne = new(
                                                evt.AbsoluteTime,
                                                control.ChannelNumber == _drumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                                evt.NoteNumber,
                                                (int)(evt.Velocity * sldVolume.Value * control.Volume),
                                                evt.OffEvent is null ? 0 : evt.NoteLength); // Fix NAudio NoteLength bug.

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
                                        // Everything else as is.
                                        MidiSend(mevt);
                                        break;
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
                        StopReq();
                        Rewind();
                    }
                }
            }
        }

        /// <summary>
        /// Send midi.
        /// </summary>
        /// <param name="evt"></param>
        void MidiSend(MidiEvent evt)
        {
            _midiOut?.Send(evt.GetAsShortMessage());

            if (btnLogMidi.Checked)
            {
                LogMessage("SND", evt.ToString());
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channel">1-based channel</param>
        void Kill(int channel)
        {
            ControlChangeEvent nevt = new(0, channel, MidiController.AllNotesOff, 0);
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
                Kill(i + 1);
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
            if (_mmTimer.Running)
            {
                chkPlay.Checked = false; // ==> stop
                chkPlay.Checked = true; // ==> play
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// Sometimes drums are on channel 1, usually if it's the only channel in a clip file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumsOn1_CheckedChanged(object? sender, EventArgs e)
        {
            _drumChannel = btnDrumsOn1.Checked ? 1 : MidiDefs.DEFAULT_DRUM_CHANNEL;
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
            LoadPattern(pinfo!);

            chkPlay.Checked = false; // ==> stop
            Rewind();

            if (btnAutoplay.Checked)
            {
                chkPlay.Checked = true; // ==> play
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
        /// 
        /// </summary>
        /// <param name="pinfo"></param>
        void LoadPattern(PatternInfo pinfo)
        {
            // Quiet.
            KillAll();

            // Clean out old.
            foreach (var (control, events) in _channels)
            {
                Controls.Remove(control);
            }
            _channels.Clear();

            // Get the new.
            int lastSubdiv = 0;
            int x = sldVolume.Right + 5;
            int y = sldVolume.Top;

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int ch = i + 1;
                int patch = pinfo.Patches[i];

                // Get pattern events.
                var chEvents = new ChannelEvents();
                var evts = _mfile.AllEvents.
                    Where(e => e.Pattern == pinfo.Name && e.Channel == ch && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);
                evts.ForEach(e => chEvents.Add(e.ScaledTime, e.MidiEvent)); // use internal time

                if(evts.Any())
                {
                    // Make new controls.
                    ChannelControl control = new()
                    {
                        ChannelNumber = ch,
                        Patch = patch,
                        Location = new(x, y),
                        IsDrums = ch == _drumChannel
                    };

                    control.ChannelChange += ChannelChange;
                    Controls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, chEvents.MaxSubdiv);

                    _channels.Add((control, chEvents));

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send real patches.
                    if (patch > PatternInfo.NO_PATCH)
                    {
                        PatchChangeEvent evt = new(0, ch, patch);
                        MidiSend(evt);
                    }
                }
            }

            //// Figure out times. Round up to bar.
            //int floor = lastSubdiv / (Common.PPQ * 4); // 4/4 only.
            //lastSubdiv = (floor + 1) * (Common.PPQ * 4);

            barBar.Length = new BarSpan(lastSubdiv);
            barBar.Start = BarSpan.Zero;
            barBar.End = barBar.Length - BarSpan.OneSubdiv;
            barBar.Current = BarSpan.Zero;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Dump current file to human readable.
        /// </summary>
        void Dump_Click(object? sender, EventArgs e)
        {
            //_mfile.DrumChannel = _drumChannel;
            //var ds = _mfile.DumpSequentialEvents();
            var ds = _mfile.DumpGroupedEvents();

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
            if(Directory.Exists(Common.Settings.ExportPath))
            {
                string basefn = Path.GetFileNameWithoutExtension(_mfile.Filename);

                if (_mfile.Filename.EndsWith(".sty"))
                {
                    foreach (var item in lbPatterns.Items) // TODO only selected channels or all if none selected.
                    {
                        var pattern = item.ToString()!;
                        var newfn = Path.Join(Common.Settings.ExportPath, $"{basefn}_{pattern.Replace(' ', '_')}.mid");
                        var info = $"Export {pattern} from {_mfile.Filename}";

                        ExportMidi(newfn, pattern, info);
                    }
                    LogMessage("INF", $"Style file {_mfile.Filename} exported to {Common.Settings.ExportPath}");
                }
                else // .mid
                {
                    var newfn = Path.Join(Common.Settings.ExportPath, $"{basefn}_export.mid");
                    var info = $"Export {_mfile.Filename}";

                    ExportMidi(newfn, "", info);
                }
                LogMessage("INF", $"Midi file {_mfile.Filename} exported to {Common.Settings.ExportPath}");
            }
            else
            {
                LogMessage("ERR", "Invalid export path in your settings");
            }
        }

        /// <summary>
        /// Output part of the file to a new midi file.
        /// </summary>
        /// <param name="fn">Where to put the midi file.</param>
        /// <param name="pattern">Specific pattern if a style file.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        void ExportMidi(string fn, string pattern, string info)  // TODO add start/end range?
        {
            // Get pattern info.
            PatternInfo pinfo = _mfile.Patterns.First(p => p.Name == pattern);

            // Init output file contents.
            MidiEventCollection outColl = new(1, Common.PPQ);
            IList<MidiEvent> outEvents = outColl.AddTrack();
            
            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = sldTempo.Value });

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
                if (pinfo.Patches[i] >= 0)
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
                    // TODO adjust velocity for noteon based on slider values? or normalize?
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
            long ltime = outEvents.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            NAudio.Midi.MidiFile.Export(fn, outColl);
        }
        #endregion
    }
}
