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
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using MidiLib;


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
        /// <summary>Current user settings.</summary>
        UserSettings _settings = new();

        /// <summary>Midi player.</summary>
        Player _player;

        /// <summary>The internal channel objects.</summary>
        ChannelCollection _allChannels = new();

        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        readonly MidiData _mdata = new();

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Where to export to.</summary>
        string _exportPath = "";

        /// <summary>Supported file types in OpenFileDialog form.</summary>
        readonly string _fileTypes = "Style Files|*.sty;*.pcs;*.sst;*.prs|Midi Files|*.mid";

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            _player = new();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            Icon = Properties.Resources.Morso;

            // Get settings and set up paths.
            string appDir = MiscUtils.GetAppDataDir("MidiStyleExplorer", "Ephemera");
            _settings = (UserSettings)Settings.Load(appDir, typeof(UserSettings));
            _exportPath = Path.Combine(appDir, "export");
            DirectoryInfo di = new(_exportPath);
            di.Create();
            _mdata.ExportPath = _exportPath;

            try
            {
                _player = new(_settings.MidiOutDevice, _allChannels, _exportPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Environment.Exit(1);
            }

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.ControlColor };

            // Init main form from settings
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            WindowState = FormWindowState.Normal;
            KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown
            Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - No file loaded";

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN:", Color.Plum);

            // Toolbar configs.
            btnAutoplay.Checked = _settings.Autoplay;
            btnLoop.Checked = _settings.Loop;

            // Other UI configs.
            chkPlay.FlatAppearance.CheckedBackColor = _settings.ControlColor;
            sldVolume.DrawColor = _settings.ControlColor;
            sldVolume.Value = _settings.Volume;
            sldTempo.DrawColor = _settings.ControlColor;
            sldTempo.Resolution = _settings.TempoResolution;

            // Time controller.
            barBar.ZeroBased = _settings.ZeroBased;
            barBar.BeatsPerBar = BEATS_PER_BAR;
            barBar.SubdivsPerBeat = PPQ;
            barBar.Snap = _settings.Snap;
            barBar.ProgressColor = _settings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Initialize tree from user settings.
            ftree.FilterExts = _fileTypes.SplitByTokens("|;*").Where(s => s.StartsWith(".")).ToList();
            ftree.RootDirs = _settings.RootDirs;
            ftree.SingleClickSelect = true; // not !_settings.Autoplay;

            // Init channel selectors.
            cmbDrumChannel1.Items.Add("NA");
            cmbDrumChannel2.Items.Add("NA");
            for (int i = 1; i <= MidiDefs.NUM_CHANNELS; i ++)
            {
                cmbDrumChannel1.Items.Add(i);
                cmbDrumChannel2.Items.Add(i);
            }

            // Hook up some simple UI handlers.
            chkPlay.CheckedChanged += (_, __) => { UpdateState(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            sldTempo.ValueChanged += (_, __) => { SetTimer(); };

            // Set up timer.
            sldTempo.Value = _settings.DefaultTempo;
            SetTimer();

            LogMessage("INF", "Hello. C=clear, W=wrap");

            try
            {
                ftree.Init();
            }
            catch (DirectoryNotFoundException)
            {
                LogMessage("WRN", "No tree directories");
            }

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
            Stop();

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
            _mmTimer.Stop();
            _mmTimer.Dispose();

            _player?.Dispose();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region State management
        /// <summary>
        /// General state management. Triggered by play button or the player via mm timer function.
        /// </summary>
        void UpdateState()
        {
            // Suppress recursive updates caused by manually pressing the play button.
            if (_guard)
            {
                return;
            }

            _guard = true;

            //LogMessage($"DBG State:{_player.State}  btnLoop{btnLoop.Checked}  TotalSubdivs:{_player.TotalSubdivs}");

            switch (_player.State)
            {
                case RunState.Complete:
                    Rewind();

                    if (btnLoop.Checked)
                    {
                        chkPlay.Checked = true;
                        Play();
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        Stop();
                    }
                    break;

                case RunState.Playing:
                    if (!chkPlay.Checked)
                    {
                        Stop();
                    }
                    break;

                case RunState.Stopped:
                    if (chkPlay.Checked)
                    {
                        Play();
                    }
                    break;
            }

            // Update UI.
            barBar.Current = new(_player.CurrentSubdiv);

            _guard = false;
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                        {
                            int chnum = i + 1;
                            if (chnum != chc.ChannelNumber && chc.State != ChannelState.Solo)
                            {
                                _player.Kill(chnum);
                            }
                        }
                        break;

                    case ChannelState.Mute:
                        _player.Kill(chc.ChannelNumber);
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                _player.SendPatch(chc.ChannelNumber, chc.Patch);
            }
        }
        #endregion

        #region Transport control
        /// <summary>
        /// Internal handler.
        /// </summary>
        void Play()
        {
            // Start or restart?
            if (!_mmTimer.Running)
            {
                _mmTimer.Start();
            }
            _player.Run(true);
        }

        /// <summary>
        /// Internal handler.
        /// </summary>
        void Stop()
        {
            _mmTimer.Stop();
            _player.Run(false);
        }

        /// <summary>
        /// Go back Jack. Doesn't affect the run state.
        /// </summary>
        void Rewind()
        {
            // Might come from another thread.
            this.InvokeIfRequired(_ =>
            {
                _player.CurrentSubdiv = 0;
                barBar.Current = BarSpan.Zero;
            });
        }
        #endregion

        #region File handling
        /// <summary>
        /// Common file opener. Initializes pattern list from contents.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        public bool OpenFile(string fn)
        {
            bool ok = true;
            _fn = "";

            LogMessage("INF", $"Reading file: {fn}");

            if(chkPlay.Checked)
            {
                chkPlay.Checked = false; // ==> Stop()
            }

            try
            {
                // Reset stuff.
                cmbDrumChannel1.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL;
                cmbDrumChannel2.SelectedIndex = 0;
                _allChannels.Reset();

                // Process the file. Set the default tempo from preferences.
                _mdata.Read(fn, _settings.DefaultTempo, false);

                // Init new stuff with contents of file/pattern.
                lbPatterns.Items.Clear();

                if(_mdata.AllPatterns.Count == 0)
                {
                    LogMessage("ERR", $"Something wrong with this file: {fn}");
                    ok = false;
                }
                else if(_mdata.AllPatterns.Count == 1) // plain midi
                {
                    var pinfo = _mdata.AllPatterns[0];
                    LoadPattern(pinfo);
                }
                else // style - multiple patterns.
                {
                    foreach (var p in _mdata.AllPatterns)
                    {
                        switch (p.PatternName)
                        {
                            // These don't contain a pattern.
                            case "SFF1": // initial patches are in here
                            case "SFF2":
                            case "SInt":
                                break;

                            case "":
                                LogMessage("ERR", "Well, this should never happen!");
                                break;

                            default:
                                lbPatterns.Items.Add(p.PatternName);
                                break;
                        }
                    }

                    if (lbPatterns.Items.Count > 0)
                    {
                        lbPatterns.SelectedIndex = 0;
                    }
                }

                Rewind();

                if (ok && btnAutoplay.Checked)
                {
                    chkPlay.Checked = true; // ==> Start()
                }

                _fn = fn;
                Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - {fn}";
            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"Couldn't open the file: {fn} because: {ex.Message}");
                Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - No file loaded";
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Tree has selected a file to play.
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
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export All", null, Export_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export Pattern", null, Export_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export Midi", null, Export_Click));
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
            if (sender is not null)
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
        #endregion

        #region Process patterns
        /// <summary>
        /// Load the requested pattern and create controls.
        /// </summary>
        /// <param name="pinfo"></param>
        void LoadPattern(PatternInfo pinfo)
        {
            _player.Reset();

            // Clean out our controls collection.
            _channelControls.ForEach(c => Controls.Remove(c));
            _channelControls.Clear();

            // Create the new controls.
            int lastSubdiv = 0;
            int x = sldTempo.Right + 5;
            int y = sldTempo.Top;

            // For scaling subdivs to internal.
            MidiLib.MidiTime mt = new()
            {
                InternalPpq = PPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = _settings.DefaultTempo
            };

            sldTempo.Value = pinfo.Tempo;

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                var chEvents = _mdata.AllEvents.
                    Where(e => e.PatternName == pinfo.PatternName && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                // Is this channel pertinent?
                if (chEvents.Any())
                {
                    _allChannels.SetEvents(chnum, chEvents, mt);

                    // Make new control.
                    ChannelControl control = new() { Location = new(x, y) };

                    // Bind to internal channel object.
                    _allChannels.Bind(chnum, control);

                    // Now init the control - after binding!
                    control.Patch = pinfo.Patches[i];
                    //control.IsDrums = GetDrumChannels().Contains(chnum);

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _channelControls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SendPatch(chnum, pinfo.Patches[i]);
                }
            }

            barBar.Length = new BarSpan(lastSubdiv);
            barBar.Start = BarSpan.Zero;
            barBar.End = barBar.Length - BarSpan.OneSubdiv;
            barBar.Current = BarSpan.Zero;

            UpdateDrumChannels();
        }

        /// <summary>
        /// Load pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = _mdata.AllPatterns.Where(p => p.PatternName == lbPatterns.SelectedItem.ToString()).First();

            LoadPattern(pinfo!);

            Rewind();

            if (btnAutoplay.Checked)
            {
                chkPlay.Checked = true; // ==> Start()
            }
        }

        /// <summary>
        /// Pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AllOrNone_Click(object? sender, EventArgs e)
        {
            bool check = sender == btnAllPatterns;
            for(int i = 0; i < lbPatterns.Items.Count; i++)
            {
                lbPatterns.SetItemChecked(i, check);                
            }
        }
        #endregion

        #region Process tick
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            try
            {
                //if(--_report <= 0)
                //{
                //    //this.InvokeIfRequired(_ => LogMessage($"DBG CurrentSubdiv:{_player.CurrentSubdiv}"));
                //    _report = 100;
                //}

                _player.DoNextStep();

                // Bump update over to main thread.
                this.InvokeIfRequired(_ => UpdateState());

            }
            catch (Exception ex)
            {
                // This sometimes blows up on shutdown with ObjectDisposedException.
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Drum channel
        /// <summary>
        /// User changed the drum channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateDrumChannels();
        }

        /// <summary>
        /// Update all channels based on current UI.
        /// </summary>
        void UpdateDrumChannels()
        {
            _channelControls.ForEach(ctl => ctl.IsDrums =
                (ctl.ChannelNumber == cmbDrumChannel1.SelectedIndex) ||
                (ctl.ChannelNumber == cmbDrumChannel2.SelectedIndex));
        }
        #endregion

        #region Export
        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            var stext = ((ToolStripMenuItem)sender!).Text;

            try
            {
                // Collect filters.
                List<string> patternNames = new();
                foreach (var p in lbPatterns.CheckedItems)
                {
                    patternNames.Add(p.ToString()!);
                }

                List<int> channels = new();
                foreach (var cc in _channelControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                switch (stext)
                {
                    case "Export All":
                        {
                            var s = _mdata.ExportAllEvents(channels);
                            LogMessage("INF", $"Exported to {s}");
                        }
                        break;

                    case "Export Pattern":
                        {
                            if (_mdata.AllPatterns.Count == 1)
                            {
                                var s = _mdata.ExportGroupedEvents("", channels, true);
                                LogMessage("INF", $"Exported default to {s}");
                            }
                            else
                            {
                                foreach (var patternName in patternNames)
                                {
                                    var s = _mdata.ExportGroupedEvents(patternName, channels, true);
                                    LogMessage("INF", $"Exported pattern {patternName} to {s}");
                                }
                            }
                        }
                        break;

                    case "Export Midi":
                        {
                            if (_mdata.AllPatterns.Count == 1)
                            {
                                // Use original ppq.
                                var s = _mdata.ExportMidi("", channels, _mdata.DeltaTicksPerQuarterNote, false);
                                LogMessage("INF", $"Export midi to {s}");
                            }
                            else
                            {
                                foreach (var patternName in patternNames)
                                {
                                    // Use original ppq.
                                    var s = _mdata.ExportMidi(patternName, channels, _mdata.DeltaTicksPerQuarterNote, false);
                                    LogMessage("INF", $"Export midi to {s}");
                                }
                            }
                        }
                        break;

                    default:
                        LogMessage("ERR", $"Ooops: {stext}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"{ex.Message}");
            }
        }
        #endregion

        #region Misc handlers
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
                    if (e.Modifiers == 0)
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
        #endregion

        #region User settings
        /// <summary>
        /// Collect and save user settings.
        /// </summary>
        void SaveSettings()
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Width, Height);
            _settings.Volume = sldVolume.Value;
            _settings.Autoplay = btnAutoplay.Checked;
            _settings.Loop = btnLoop.Checked;
            _settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = _settings.Edit("User Settings");

            // Detect changes of interest.
            bool restart = false;

            // Figure out what changed - each handled differently.
            foreach (var (name, cat) in changes)
            {
                restart |= name == "MidiOutDevice";
                restart |= name == "ControlColor";
                restart |= name == "RootDirs";
                restart |= name == "ZeroBased";
            }

            // Figure out what changed.
            if (restart)
            {
                MessageBox.Show("Restart required for changes to take effect");
            }

            // Benign changes.
            barBar.Snap = _settings.Snap;
            barBar.ZeroBased = _settings.ZeroBased;
            btnLoop.Checked = _settings.Loop;
            sldTempo.Resolution = _settings.TempoResolution;

            SaveSettings();
        }
        #endregion

        #region Info
        /// <summary>
        /// All about me.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            MiscUtils.ShowReadme("MidiStyleExplorer");
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

        #region Utilities
        /// <summary>
        /// Convert tempo to period and set mm timer.
        /// </summary>
        void SetTimer()
        {
            MidiLib.MidiTime mt = new()
            {
                InternalPpq = PPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = sldTempo.Value
            };

            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }
        #endregion
    }
}
