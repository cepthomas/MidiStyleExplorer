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

        /// <summary>All the channel controls. Index is 0-based channel number.</summary>
        //readonly PlayChannel[] _playChannels = new PlayChannel[MidiDefs.NUM_CHANNELS];


        List<PlayChannel> _playChannels = new();


        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        int _drumChannel = MidiDefs.DEFAULT_DRUM_CHANNEL;
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
            toolStrip1.Renderer = new TsRenderer(); //TODO { SelectedColor = Common.Settings.ControlColor };
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

            // Init internal structure.
            for (int i = 0; i < _playChannels.Count; i++)
            {
                _playChannels[i] = new PlayChannel() { ChannelNumber = i + 1 };
            }

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
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();

            // Stop and destroy mmtimer.
            Stop();
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
            int catSize = 5;
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
                // Clean up first.
                //cgChannels.Clear();
                Rewind();

                // Process the file.
                _mfile = new MidiFile { IgnoreNoisy = true };
                _mfile.ProcessFile(fn);

                if (_mfile.Filename.EndsWith(".mid"))
                {
                    var pinfo = _mfile.Patterns[0];
                    InitFromPattern(pinfo);
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
                                lbPatterns.Items.Add(p);
                                break;
                        }
                    }

                    if (lbPatterns.Items.Count > 0)
                    {
                        var pinfo = _mfile.GetPatternInfo(lbPatterns.Items[0].ToString()!);
                        InitFromPattern(pinfo!);
                    }
                }

                Text = $"MidiStyleExplorer {MiscUtils.GetVersionString()} - {fn}";
                Common.Settings.RecentFiles.UpdateMru(fn);
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
            // Start or restart?
            if (!_running)
            {
                // Downshift to time increments compatible with this system.
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_PlaybackCompleted(object? sender, EventArgs e)
        {
            // Usually comes from a different thread.
            this.InvokeIfRequired(_ =>
            {
                if (btnLoop.Checked)
                {
                    Play();
                }
                else
                {
                    Rewind();
                }
            });
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
            Stop();
            barBar.Current = BarSpan.Zero;
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
                bool solo = _playChannels.Where(c => c.Mode == PlayMode.Solo).Any();

                // Process each channel.
                foreach (var ch in _playChannels)
                {
                    if (ch.Valid)
                    {
                        // Look for events to send.
                        if (ch.Mode == PlayMode.Solo || (!solo && ch.Mode == PlayMode.Normal))
                        {
                            // Process any sequence steps.
                            if (ch.EventsXXX.ContainsKey(barBar.Current.TotalSubdivs))
                            {
                                foreach (var mevt in ch.EventsXXX[barBar.Current.TotalSubdivs])
                                {
                                    switch (mevt.Event)
                                    {
                                        case NoteOnEvent evt:
                                            if (ch.ChannelNumber == _drumChannel && evt.Velocity == 0)
                                            {
                                                // Skip drum noteoffs as windows GM doesn't like them.
                                            }
                                            else
                                            {
                                                // Adjust volume and maybe drum channel. Also NAudio NoteLength bug.
                                                NoteOnEvent ne = new(
                                                    evt.AbsoluteTime,
                                                    ch.ChannelNumber == _drumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                                    evt.NoteNumber,
                                                    (int)(evt.Velocity * sldVolume.Value),
                                                    evt.OffEvent is null ? 0 : evt.NoteLength);

                                                MidiSend(ne);
                                            }
                                            break;

                                        case NoteEvent evt:
                                            if (ch.ChannelNumber == _drumChannel)
                                            {
                                                // Skip drum noteoffs as windows GM doesn't like them.
                                            }
                                            else
                                            {
                                                MidiSend(evt);
                                            }
                                            break;

                                        default:
                                            MidiSend(mevt.Event);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Bump time. Check for end of play. Client will take care of transport control.
                if (barBar.IncrementCurrent(1))
                {
                    _running = false;
                    Player_PlaybackCompleted(this, new EventArgs()); // TODO
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
            InitChannelsGrid();
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
            var pinfo = _mfile.GetPatternInfo(lbPatterns.SelectedItem.ToString()!);
            InitFromPattern(pinfo!);
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
            /////Export();

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
                _mfile.ExportMidi(newfn, pattern, info);
            }
        }
        #endregion





        //////////////////////// TODO all this stuff ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this stuff ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this stuff ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this stuff ////////////////////////////////////////////////////////////////////////


        void hoohoo()
        {


            //// Set up the channel/mute/solo grid.
            //cgChannels.AddStateType((int)PlayMode.Normal, Color.Black, Color.AliceBlue);
            //cgChannels.AddStateType((int)PlayMode.Solo, Color.Black, Color.LightGreen);
            //cgChannels.AddStateType((int)PlayMode.Mute, Color.Black, Color.Salmon);

        }

        void InitFromPattern(PatternInfo pinfo)
        {
            var evts = _mfile.GetEvents(pinfo.Name);

            InitChannelsGrid();

            // Might need to update the patches.
            for (int i = 0; i < pinfo.Patches.Length; i++)
            {
                if (pinfo.Patches[i] != -1)
                {
                    PatchChangeEvent evt = new(0, i + 1, pinfo.Patches[i]);
                    MidiSend(evt);
                }
            }

            Rewind();

            if (btnAutoplay.Checked)
            {
                Play();
            }
        }


        /// <summary>
        /// Populate the click grid.
        /// </summary>
        void InitChannelsGrid()
        {
 //           cgChannels.Clear();

            for (int i = 0; i < _playChannels.Count; i++)
            {
                var pc = _playChannels[i];

                // Make a name for UI.
                pc.Name = $"Ch:({i + 1}) ";

                if (i + 1 == _drumChannel)
                {
                    pc.Name += $"Drums";
                }
                else if (pc.Patch == -1)
                {
                    pc.Name += $"NoPatch";
                }
                else
                {
                    pc.Name += MidiDefs.GetInstrumentDef(pc.Patch);
                }

                // Maybe add to UI.
                if (pc.Valid && pc.HasNotes)
                {
//                    cgChannels.AddIndicator(pc.Name, i);
                }
            }

//            cgChannels.Show(2, cgChannels.Width / 2, 20);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Channels_IndicatorEvent(object? sender, IndicatorEventArgs e)
        {
            int channel = e.Id;
            PlayChannel pch = _playChannels[channel];

            switch (pch.Mode)
            {
                case PlayMode.Normal:
                    pch.Mode = PlayMode.Solo;
                    // Mute any other non-solo channels.
                    for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                    {
                        if (i != channel && _playChannels[i].Valid && _playChannels[i].Mode != PlayMode.Solo)
                        {
                            Kill(i);
                        }
                    }
                    break;

                case PlayMode.Solo:
                    pch.Mode = PlayMode.Mute;
                    Kill(channel);
                    break;

                case PlayMode.Mute:
                    pch.Mode = PlayMode.Normal;
                    break;
            }

//            cgChannels.SetIndicator(channel, (int)pch.Mode);
        }





        /// <summary>
        /// Validate selections and send patch now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object? sender, EventArgs e) //TODO
        {
            //bool valid = int.TryParse(txtPatchChannel.Text, out int pch);
            //if (valid && pch >= 1 && pch <= MidiDefs.NUM_CHANNELS)
            //{
            //    PatchChangeEvent evt = new(0, pch, cmbPatchList.SelectedIndex);
            //    MidiSend(evt);

            //    // Update UI.
            //    _playChannels[pch - 1].Patch = cmbPatchList.SelectedIndex;
            //    InitChannelsGrid();
            //}
            //else
            //{
            //    //txtPatchChannel.Text = "";
            //    LogMessage("ERR", "Invalid patch channel");
            //}
        }
    }
}
