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
        #region Constants
        /// <summary>Only 4/4 time supported.</summary>
        const int BEATS_PER_BAR = 4;

        /// <summary>Our internal ppq aka resolution - used for sending realtime midi messages.</summary>
        const int PPQ = 32;
        #endregion

        #region Fields
        /// <summary>The settings.</summary>
        UserSettings _settings = new();

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Indicates whether or not the midi is playing.</summary>
        bool _running = false;

        /// <summary>Midi events from the input file.</summary>
        MidiFile? _mfile;// = new();

        /// <summary>All the channels. Index is 0-based channel number.</summary>
        readonly PlayChannel[] _playChannels = new PlayChannel[MidiDefs.NUM_CHANNELS];

        /// <summary>Some midi files have drums on a different channel so allow the user to re-map.</summary>
        int _drumChannel = MidiDefs.DEFAULT_DRUM_CHANNEL;
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

            // Get the settings.
            string appDir = MiscUtils.GetAppDataDir("MidiStyleExplorer", "Ephemera");
            DirectoryInfo di = new(appDir);
            di.Create();
            _settings = UserSettings.Load(appDir);

            // Init main form from settings
            Location = new Point(_settings.MainFormInfo.X, _settings.MainFormInfo.Y);
            Size = new Size(_settings.MainFormInfo.Width, _settings.MainFormInfo.Height);
            WindowState = FormWindowState.Normal;
            KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown
            Text = $"Clip Explorer {MiscUtils.GetVersionString()} - No file loaded";

            // The text output.
            txtViewer.WordWrap = true;
            txtViewer.BackColor = Color.Cornsilk;
            txtViewer.Colors.Add("ERROR", Color.LightPink);
            txtViewer.Colors.Add("WARN:", Color.Plum);
            txtViewer.Font = new Font("Lucida Console", 9);

            // Init internal structure.
            for (int i = 0; i < _playChannels.Length; i++)
            {
                _playChannels[i] = new PlayChannel() { ChannelNumber = i + 1 };
            }

            // Fill patch list.
            for (int i = 0; i <= MidiDefs.MAX_MIDI; i++)
            {
                cmbPatchList.Items.Add(MidiDefs.GetInstrumentDef(i));
            }
            cmbPatchList.SelectedIndex = 0;

            // Other UI configs.

            chkDrumsOn1.FlatAppearance.CheckedBackColor = _settings.ControlColor;
            chkLogMidi.FlatAppearance.CheckedBackColor = _settings.ControlColor;
            sldVolume.Value = _settings.Volume;
            sldVolume.DrawColor = _settings.ControlColor;
            sldTempo.DrawColor = _settings.ControlColor;
            sldTempo.Value = _settings.DefaultTempo;

            // Time controller.
            barBar.BeatsPerBar = BEATS_PER_BAR;
            barBar.SubdivsPerBeat = PPQ;
            barBar.Snap = _settings.Snap;
            barBar.ProgressColor = _settings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Figure out the midi output device.
            for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
            {
                if (_settings.MidiOutDevice == MidiOut.DeviceInfo(devindex).ProductName)
                {
                    _midiOut = new MidiOut(devindex);
                    break;
                }
            }
            if (_midiOut is null)
            {
                MessageBox.Show($"Invalid midi device: {_settings.MidiOutDevice}");
            }

            // Set up the channel/mute/solo grid.
            cgChannels.AddStateType((int)PlayChannel.PlayMode.Normal, Color.Black, Color.AliceBlue);
            cgChannels.AddStateType((int)PlayChannel.PlayMode.Solo, Color.Black, Color.LightGreen);
            cgChannels.AddStateType((int)PlayChannel.PlayMode.Mute, Color.Black, Color.Salmon);
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
            _settings.Volume = sldVolume.Value;
            _settings.MainFormInfo = new Rectangle(Location.X, Location.Y, Width, Height);

            _settings.Save();
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

            PropertyGridEx pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = _settings
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

            barBar.Snap = _settings.Snap;

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
        /// <param name="sender"></param>
        /// <param name="ea"></param>
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

            _settings.RecentFiles.ForEach(f =>
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
                if (fn != _fn)
                {
                    OpenFile(fn);
                    _fn = fn;
                }
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

            if (openDlg.ShowDialog() == DialogResult.OK && openDlg.FileName != _fn)
            {
                OpenFile(openDlg.FileName);
                _fn = openDlg.FileName;
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

            LogMessage("INFO", $"Opening file: {fn}");

            try
            {
                // Clean up first.
                cgChannels.Clear();
                Rewind();

                // Process the file.
                _mfile = new MidiFile { IgnoreNoisy = true };
                _mfile.ProcessFile(fn);

                // Do things with things.
                _mfile.Channels.ForEach(ch => _playChannels[ch.Key - 1].Patch = ch.Value);

                lbPatterns.Items.Clear();
                foreach (var p in _mfile.AllPatterns)
                {
                    switch (p)
                    {
                        case "SFF1": // patches are in here
                        case "SFF2":
                        case "SInt":
                            break;

                        default:
                            lbPatterns.Items.Add(p);
                            break;
                    }
                }

                if (lbPatterns.Items.Count > 0)
                {
                    lbPatterns.SelectedIndex = 0;
                }
                else
                {
                    GetPatternEvents(null);
                }

                InitChannelsGrid();

                _fn = fn;
                Text = $"MidiStyleExplorer {MiscUtils.GetVersionString()} - {fn}";
                _settings.RecentFiles.UpdateMru(fn);
            }
            catch (Exception ex)
            {
                LogMessage("ERROR", $"Couldn't open the file: {fn} because: {ex.Message}");
                _fn = "";
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
                    InternalPpq = PPQ,
                    MidiPpq = _mfile.DeltaTicksPerQuarterNote,
                    Tempo = _mfile.Tempo
                };

                // Create periodic timer.
                double period = mt.InternalToMsec(1);
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
                if (chkLoop.Checked)
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
                bool solo = _playChannels.Where(c => c.Mode == PlayChannel.PlayMode.Solo).Any();

                // Process each channel.
                foreach (var ch in _playChannels)
                {
                    if (ch.Valid)
                    {
                        // Look for events to send.
                        if (ch.Mode == PlayChannel.PlayMode.Solo || (!solo && ch.Mode == PlayChannel.PlayMode.Normal))
                        {
                            // Process any sequence steps.
                            if (ch.Events.ContainsKey(barBar.Current.TotalSubdivs))
                            {
                                foreach (var mevt in ch.Events[barBar.Current.TotalSubdivs])
                                {
                                    switch (mevt)
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
                                            MidiSend(mevt);
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
                    Player_PlaybackCompleted(this, new EventArgs());//TODO
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
        #endregion


        //////////////////////// TODO all this leftover stuf ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this leftover stuf ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this leftover stuf ////////////////////////////////////////////////////////////////////////
        //////////////////////// TODO all this leftover stuf ////////////////////////////////////////////////////////////////////////


        #region Utilities
        /// <summary>
        /// Dump current file.
        /// </summary>
        void Dump_Click(object? sender, EventArgs e)
        {
            bool clip = true;

            var ds = Dump();
            if (ds.Count > 0)
            {
               if (clip)//_settings.DumpToClip)
               {
                   Clipboard.SetText(string.Join(Environment.NewLine, ds));
                   LogMessage("INFO", "File dumped to clipboard");
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Export_Click(object? sender, EventArgs e)
        {
            Export();
        }

        public List<string> Dump()
        {
            _mfile.DrumChannel = _drumChannel;
            //return _mfile.GetReadableGrouped();
            return _mfile.GetReadableContents();
        }

        public void Export()
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
                _mfile.ExportMidi(newfn, pattern, info);
            }
        }
        #endregion





        /// <summary>
        /// Get requested events.
        /// </summary>
        /// <param name="pattern">Specific pattern.</param>
        void GetPatternEvents(string? pattern)
        {
            // Init internal structure.
            _playChannels.ForEach(pc => pc.Reset());

            // Downshift to time increments compatible with this application.
            MidiTime mt = new()
            {
                InternalPpq = PPQ,
                MidiPpq = _mfile.DeltaTicksPerQuarterNote,
                Tempo = sldTempo.Value
            };

            // Bin events by channel. Scale to internal ppq.
            foreach (var ch in _mfile.Channels)
            {
                _playChannels[ch.Key - 1].Patch = ch.Value;
                var pevts = _mfile.GetEvents(pattern, ch.Key);

                foreach (var te in pevts)
                {
                    if (te.Channel - 1 < MidiDefs.NUM_CHANNELS) // midi is one-based
                    {
                        // Scale to internal.
                        long subdiv = mt.MidiToInternal(te.AbsoluteTime);

                        // Add to our collection.
                        _playChannels[te.Channel - 1].AddEvent((int)subdiv, te);
                    }
                };
            }

            // Figure out times.
            int lastSubdiv = _playChannels.Max(pc => pc.MaxSubdiv);
            // Round up to bar.
            int floor = lastSubdiv / (PPQ * 4); // 4/4 only.
            lastSubdiv = (floor + 1) * (PPQ * 4);
            // TODO????sldTempo.Value = _tempo;

            barBar.Length = new BarSpan(lastSubdiv);
            barBar.Start = BarSpan.Zero;
            barBar.End = barBar.Length - BarSpan.OneSubdiv;
            barBar.Current = BarSpan.Zero;
        }


        /// <summary>
        /// Populate the click grid.
        /// </summary>
        void InitChannelsGrid()
        {
            cgChannels.Clear();

            for (int i = 0; i < _playChannels.Length; i++)
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
                    cgChannels.AddIndicator(pc.Name, i);
                }
            }

            cgChannels.Show(2, cgChannels.Width / 2, 20);
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
                case PlayChannel.PlayMode.Normal:
                    pch.Mode = PlayChannel.PlayMode.Solo;
                    // Mute any other non-solo channels.
                    for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                    {
                        if (i != channel && _playChannels[i].Valid && _playChannels[i].Mode != PlayChannel.PlayMode.Solo)
                        {
                            Kill(i);
                        }
                    }
                    break;

                case PlayChannel.PlayMode.Solo:
                    pch.Mode = PlayChannel.PlayMode.Mute;
                    Kill(channel);
                    break;

                case PlayChannel.PlayMode.Mute:
                    pch.Mode = PlayChannel.PlayMode.Normal;
                    break;
            }

            cgChannels.SetIndicator(channel, (int)pch.Mode);
        }





        /// <summary>
        /// Validate selections and send patch now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object? sender, EventArgs e)
        {
            bool valid = int.TryParse(txtPatchChannel.Text, out int pch);
            if (valid && pch >= 1 && pch <= MidiDefs.NUM_CHANNELS)
            {
                PatchChangeEvent evt = new(0, pch, cmbPatchList.SelectedIndex);
                MidiSend(evt);

                // Update UI.
                _playChannels[pch - 1].Patch = cmbPatchList.SelectedIndex;
                InitChannelsGrid();
            }
            else
            {
                //txtPatchChannel.Text = "";
                LogMessage("ERROR", "Invalid patch channel");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            GetPatternEvents(lbPatterns.SelectedItem.ToString()!);
            InitChannelsGrid();

            // Might need to update the patches.
            foreach (var ch in _mfile.Channels)
            {
                if (ch.Value != -1)
                {
                    PatchChangeEvent evt = new(0, ch.Key, ch.Value);
                    MidiSend(evt);
                }
            }

            Rewind();
            Play();
        }
    }
}
