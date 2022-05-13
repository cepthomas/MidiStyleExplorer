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
using static MidiLib.ChannelCollection;


// TODO lbPatterns should be checked lb.

namespace MidiStyleExplorer
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Midi player.</summary>
        readonly Player _player;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiData _mdata = new();

        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        ///// <summary>All the channel controls and data for current pattern. Needs synchronized access.</summary>
        //readonly List<(ChannelControl control, ChannelEvents events)> _channels = new();


        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;


        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Supported file types in OpenFileDialog form.</summary>
        readonly string _fileTypes = "Style Files|*.sty;*.pcs;*.sst;*.prs|Midi Files|*.mid";
        #endregion

        #region Fields - user custom - get from settings TODOX
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiDevice = "VirtualMIDISynth #1";

        /// <summary>Adjust to taste.</summary>
        readonly string _exportPath = @"C:\Dev\repos\MidiLib\out";

        // /// <summary>Reporting interval.</summary>
        // int _report = 0;
        #endregion


ToolStripLabel toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
ToolStripComboBox cmbDrumChannel1 = new System.Windows.Forms.ToolStripComboBox();
ToolStripLabel toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
ToolStripComboBox cmbDrumChannel2 = new System.Windows.Forms.ToolStripComboBox();

Button btnAll = new System.Windows.Forms.Button();
Button btnNone = new System.Windows.Forms.Button();


        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            _player = new(_midiDevice) { MidiTraceFile = @"C:\Dev\repos\MidiLib\out\midi_out.txt" }; //TODOX
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
            Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - No file loaded";

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
            sldVolume.DrawColor = Common.Settings.ControlColor;
            sldVolume.Value = Common.Settings.Volume;
            sldTempo.DrawColor = Common.Settings.ControlColor;
            sldTempo.Value = Common.Settings.DefaultTempo;
            sldTempo.Resolution = Common.Settings.TempoResolution;

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
//            chkPlay.CheckedChanged += (_, __) => { _ = chkPlay.Checked ? Play() : Stop(); };
//            btnRewind.Click += (_, __) => { Rewind(); };

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
            // else
            // {
            //     OpenFile(@"C:\Dev\repos\MidiStyleExplorer\test\_LoveSong.S474.sty");
            //     //OpenFile(@"C:\Users\cepth\OneDrive\Audio\Midi\styles\2kPopRock\60'sRock&Roll.S605.sty");
            //     //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\_drums_ch1.mid");
            //     //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\25jazz.mid");
            //     // This has drums on 9 and 11:
            //     //OpenFile(@"C:\Users\cepth\OneDrive\Audio\Midi\styles\Gary USB\g-70 styles\G-70 #1\ContempBeat_G70.S423.STY");
            // }


            //===============TODOX from lib test

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
            //sldPosition.ValueChanged += (_, __) => { GetPosition(); };
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Stop_X();

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
            barBar.Snap = Common.Settings.Snap;
            btnLoop.Checked = Common.Settings.Loop;
            sldTempo.Resolution = Common.Settings.TempoResolution;

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





        #region State management - new
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
                        Play_X();
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        Stop_X();
                    }
                    break;

                case RunState.Playing:
                    if (!chkPlay.Checked)
                    {
                        Stop_X();
                    }
                    break;

                case RunState.Stopped:
                    if (chkPlay.Checked)
                    {
                        Play_X();
                    }
                    break;
            }

            // Update UI.
            SetPosition();

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

        #region Transport control - new
        /// <summary>
        /// Internal handler.
        /// </summary>
        void Play_X()
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
        void Stop_X()
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

        #region File handling - new
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
                chkPlay.Checked = false; // ==> Stop_X()
            }

            try
            {
                // Reset drums.
                cmbDrumChannel1.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL;
                cmbDrumChannel2.SelectedIndex = 0;

                // Process the file. Set the default tempo from preferences.
                _mdata = new();
                _mdata.Read(fn, _defaultTempo, false);

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
                Text = $"Midi Lib - {fn}";

            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"Couldn't open the file: {fn} because: {ex.Message}");
                Text = "Midi Lib";
                ok = false;
            }

            return ok;
        }
        #endregion

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

            //TODOX these:
            // this.btnExportAll = new System.Windows.Forms.ToolStripButton();
            // this.btnExportPattern = new System.Windows.Forms.ToolStripButton();
            // this.btnExportMidi = new System.Windows.Forms.ToolStripButton();

            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Open...", null, Open_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export All", null, Export_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export Pattern", null, Export_Click));
            fileDropDownButton.DropDownItems.Add(new ToolStripMenuItem("Export Midi", null, Export_Click));
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





        #region Process patterns - new
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
                InternalPpq = Common.PPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = Common.Settings.DefaultTempo
            };

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                var chEvents = _mdata.AllEvents.
                    Where(e => e.PatternName == pinfo.PatternName && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                // Is this channel pertinent?
                if (chEvents.Any())
                {
                    TheChannels.SetEvents(chnum, chEvents, mt);

                    // Make new control.
                    ChannelControl control = new() { Location = new(x, y) };

                    // Bind to internal channel object.
                    TheChannels.Bind(chnum, control);

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
        void AllOrNone_Click(object? sender, EventArgs e)//TODOX hook up
        {
            bool check = sender == btnAll;
            for(int i = 0; i < lbPatterns.Items.Count; i++)
            {
                lbPatterns.SetItemChecked(i, check);                
            }
        }
        #endregion

        #region Process tick - new
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // TODO This sometimes blows up on shutdown with ObjectDisposedException. I am probably doing bad things with threads.
            try
            {
                //if(--_report <= 0)
                //{
                //    //this.InvokeIfRequired(_ => LogMessage($"DBG CurrentSubdiv:{_player.CurrentSubdiv}"));
                //    _report = 100;
                //}

                _player.DoNextStep();
                // Bump over to main thread.
                this.InvokeIfRequired(_ => UpdateState());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Drum channel - new
        /// <summary>
        /// User changed the drum channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannel_SelectedIndexChanged(object sender, EventArgs e)
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

        #region Utilities - new
        /// <summary>
        /// Convert tempo to period and set mm timer.
        /// </summary>
        void SetTimer()
        {
            MidiLib.MidiTime mt = new()
            {
                InternalPpq = Common.PPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = sldTempo.Value
            };

            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            _mdata.ExportPath = _exportPath;

            try
            {
                // Collect filters.
                List<string> patternNames = new();
                foreach(var p in lbPatterns.CheckedItems)
                {
                    patternNames.Add(p.ToString()!);
                }

                List<int> channels = new();
                foreach (var cc in _channelControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                if (sender == btnExportAll)
                {
                    var s = _mdata.ExportAllEvents(channels);
                    LogMessage("INF", $"Exported to {s}");
                }
                else if (sender == btnExportPattern)
                {
                    if(_mdata.AllPatterns.Count == 1)
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
                else if (sender == btnExportMidi)
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
                else
                {
                    LogMessage("ERR", $"Ooops: {sender}");
                }
            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"{ex.Message}");
            }
        }
        #endregion


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
        void Tempo_ValueChanged(object? sender, EventArgs e)//TODOX not like this?
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Kill_Click(object? sender, EventArgs e)
        {
            _player.KillAll();
        }

        /////////////////////////// old below ////////////////

        ///// <summary>
        ///// Common file opener.
        ///// </summary>
        ///// <param name="fn">The file to open.</param>
        ///// <returns>Status.</returns>
        //public void OpenFile(string fn)
        //{
        //    _fn = fn;
        //    chkPlay.Checked = false; // ==> stop

        //    LogMessage("INF", $"Opening file: {fn}");

        //    try
        //    {
        //        // Process the file.
        //        _mdata = new();
        //        _mdata.Read(fn, Common.Settings.DefaultTempo, false);

        //        // All channel numbers.
        //        //var channels = _mfile.AllEvents.Select(e => e.Channel).Distinct().OrderBy(e => e);

        //        // Init new stuff with contents of file/pattern.
        //        lbPatterns.Items.Clear();
        //        if (fn.ToLower().EndsWith(".mid"))
        //        {
        //            var pinfo = _mdata.AllPatterns[0];
        //            LoadPattern(pinfo);
        //        }
        //        else // .sty
        //        {
        //            foreach (var p in _mdata.AllPatterns)
        //            {
        //                switch (p.PatternName)
        //                {
        //                    case "SFF1": // initial patches are in here
        //                    case "SFF2":
        //                    case "SInt":
        //                        break;

        //                    case "":
        //                        LogMessage("ERR", "Well, this should never happen!");
        //                        break;

        //                    default:
        //                        lbPatterns.Items.Add(p.PatternName);
        //                        break;
        //                }
        //            }

        //            if (lbPatterns.Items.Count > 0)
        //            {
        //                lbPatterns.SelectedIndex = 0;
        //            }
        //        }

        //        Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - {fn} File Type:{_mdata.MidiFileType} Tracks:{_mdata.Tracks} PPQ:{_mdata.DeltaTicksPerQuarterNote}";
        //        Common.Settings.RecentFiles.UpdateMru(fn);

        //        Rewind();
        //        if (btnAutoplay.Checked)
        //        {
        //            chkPlay.Checked = true; // ==> play
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage("ERR", $"Couldn't open the file: {fn} because: {ex.Message}");
        //        Text = $"Midi Style Explorer {MiscUtils.GetVersionString()} - No file loaded";
        //    }
        //}

        ///// <summary>
        ///// Internal handler.
        ///// </summary>
        ///// <returns></returns>
        //bool Play()
        //{
        //    this.InvokeIfRequired(_ =>
        //    {
        //        // Start or restart?
        //        if (!_mmTimer.Running)
        //        {
        //            // Convert tempo to period.
        //            MidiLib.MidiTime mt = new()
        //            {
        //                InternalPpq = Common.PPQ,
        //                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
        //                Tempo = sldTempo.Value
        //            };

        //            // Create periodic timer.
        //            double period = mt.RoundedInternalPeriod();
        //            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        //            _mmTimer.Start();
        //        }
        //        else
        //        {
        //            Rewind();
        //        }
        //    });

        //    return true;
        //}

        ///// <summary>
        ///// Internal handler.
        ///// </summary>
        ///// <returns></returns>
        //bool Stop()
        //{
        //    // Might come from another thread.
        //    this.InvokeIfRequired(_ =>
        //    {
        //        _mmTimer.Stop();
        //        // Send midi stop all notes just in case.
        //        KillAll();
        //    });

        //    return true;
        //}

        ///// <summary>
        ///// Wrapper for cross-thread.
        ///// </summary>
        ///// <returns></returns>
        //bool StopReq()
        //{
        //    // Might come from another thread.
        //    this.InvokeIfRequired(_ =>
        //    {
        //        chkPlay.Checked = false;
        //    });

        //    return true;
        //}


        ///// <summary>
        ///// Go back Jack.
        ///// </summary>
        //public void Rewind()
        //{
        //    // Might come from another thread.
        //    this.InvokeIfRequired(_ =>
        //    {
        //        barBar.Current = BarSpan.Zero;
        //    });
        //}



        ///// <summary>
        ///// Multimedia timer callback. Synchronously outputs the next midi events.
        ///// </summary>
        //void MmTimerCallback(double totalElapsed, double periodElapsed)
        //{
        //    if (_mmTimer.Running)
        //    {
        //        lock (_channels)
        //        {
        //            // Any soloes?
        //            bool solo = _channels.Where(c => c.control.State == PlayState.Solo).Any();

        //            // Process each channel.
        //            foreach (var (control, events) in _channels)
        //            {
        //                // Look for events to send.
        //                if (control.State == PlayState.Solo || (!solo && control.State == PlayState.Normal))
        //                {
        //                    // Process any sequence steps.
        //                    if (events.MidiEvents.ContainsKey(barBar.Current.TotalSubdivs))
        //                    {
        //                        foreach (var mevt in events.MidiEvents[barBar.Current.TotalSubdivs])
        //                        {
        //                            switch (mevt)
        //                            {
        //                                case NoteOnEvent evt:
        //                                    if (control.IsDrums && evt.Velocity == 0)
        //                                    {
        //                                        // Skip drum noteoffs as windows GM doesn't like them.
        //                                    }
        //                                    else
        //                                    {
        //                                        // Adjust volume and maybe drum channel.
        //                                        NoteOnEvent ne = new(
        //                                            evt.AbsoluteTime,
        //                                            control.IsDrums ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
        //                                            evt.NoteNumber,
        //                                            Math.Min((int)(evt.Velocity * sldVolume.Value * control.Volume), MidiDefs.MAX_MIDI),
        //                                            evt.OffEvent is null ? 0 : evt.NoteLength); // Fix NAudio NoteLength bug.

        //                                        MidiSend(ne);
        //                                    }
        //                                    break;

        //                                case NoteEvent evt:
        //                                    if (control.IsDrums)
        //                                    {
        //                                        // Skip drum noteoffs as windows GM doesn't like them.
        //                                    }
        //                                    else
        //                                    {
        //                                        MidiSend(evt);
        //                                    }
        //                                    break;

        //                                default:
        //                                    // Everything else as is.
        //                                    MidiSend(mevt);
        //                                    break;
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //            // Bump time. Check for end of play.
        //            if (barBar.IncrementCurrent(1))
        //            {
        //                if (btnLoop.Checked)
        //                {
        //                    Play();
        //                }
        //                else
        //                {
        //                    StopReq();
        //                    Rewind();
        //                }
        //            }
        //        }

        //    }
        //}

        ///// <summary>
        ///// Send midi.
        ///// </summary>
        ///// <param name="evt"></param>
        //void MidiSend(MidiEvent evt)
        //{
        //    _midiOut?.Send(evt.GetAsShortMessage());

        //    if (btnLogMidi.Checked)
        //    {
        //        LogMessage("SND", evt.ToString());
        //    }
        //}

        ///// <summary>
        ///// Send all notes off.
        ///// </summary>
        ///// <param name="channel">1-based channel</param>
        //void Kill(int channel)
        //{
        //    ControlChangeEvent nevt = new(0, channel, MidiController.AllNotesOff, 0);
        //    MidiSend(nevt);
        //}

        ///// <summary>
        ///// Send all notes off.
        ///// </summary>
        //void KillAll()
        //{
        //    // Send midi stop all notes just in case.
        //    for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
        //    {
        //        Kill(i + 1);
        //    }
        //}





        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        //{
        //    var pinfo = GetPatternInfo(lbPatterns.SelectedItem.ToString()!);
        //    LoadPattern(pinfo!);

        //    chkPlay.Checked = false; // ==> stop
        //    Rewind();

        //    if (btnAutoplay.Checked)
        //    {
        //        chkPlay.Checked = true; // ==> play
        //    }
        //}

        ///// <summary>
        ///// The user clicked something in one of the channel controls.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        //{
        //    ChannelControl chc = (ChannelControl)sender!;

        //    if (e.StateChange)
        //    {
        //        switch (chc.State)
        //        {
        //            case PlayState.Normal:
        //                break;

        //            case PlayState.Solo:
        //                // Mute any other non-solo channels.
        //                for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
        //                {
        //                    if (i != chc.ChannelNumber && chc.State != PlayState.Solo)
        //                    {
        //                        Kill(i);
        //                    }
        //                }
        //                break;

        //            case PlayState.Mute:
        //                Kill(chc.ChannelNumber);
        //                break;
        //        }
        //    }

        //    if (e.PatchChange && chc.Patch >= 0)
        //    {
        //        PatchChangeEvent evt = new(0, chc.ChannelNumber, chc.Patch);
        //        MidiSend(evt);
        //    }
        //}


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="pname"></param>
        ///// <returns></returns>
        //public PatternInfo? GetPatternInfo(string pname)
        //{
        //    var ret = _mdata.Patterns.Where(p => p.Name == pname).First();
        //    return ret;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="pinfo"></param>
        //void LoadPattern(PatternInfo pinfo)
        //{
        //    // Quiet.
        //    KillAll();

        //    lock (_channels)
        //    {
        //        // Clean out old. Save current state to restore next.
        //        Dictionary<int, (PlayState, double, bool, bool)> stats = new();
        //        foreach (var (control, events) in _channels)
        //        {
        //            stats.Add(control.ChannelNumber, (control.State, control.Volume, control.Selected, control.IsDrums));
        //            Controls.Remove(control);
        //        }
        //        _channels.Clear();

        //        // Get the new.
        //        int lastSubdiv = 0;
        //        int x = sldVolume.Right + 5;
        //        int y = sldVolume.Top;

        //        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
        //        {
        //            int ch = i + 1;
        //            int patch = pinfo.Patches[i];

        //            // Get pattern events.
        //            var chEvents = new ChannelEvents();
        //            var evts = _mdata.AllEvents.
        //                Where(e => e.Pattern == pinfo.Name && e.Channel == ch && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
        //                OrderBy(e => e.AbsoluteTime);
        //            evts.ForEach(e => chEvents.Add(e.ScaledTime, e.MidiEvent)); // use internal time

        //            if (evts.Any())
        //            {
        //                // Make new controls.
        //                ChannelControl control = new()
        //                {
        //                    ChannelNumber = ch,
        //                    Patch = patch,
        //                    Location = new(x, y),
        //                };

        //                // Sticky previous attributes.
        //                if (stats.ContainsKey(ch))
        //                {
        //                    control.State = stats[ch].Item1;
        //                    control.Volume = stats[ch].Item2;
        //                    control.Selected = stats[ch].Item3;
        //                    control.IsDrums = stats[ch].Item4;
        //                }

        //                control.ChannelChange += ChannelChange;
        //                Controls.Add(control);

        //                lastSubdiv = Math.Max(lastSubdiv, chEvents.MaxSubdiv);

        //                _channels.Add((control, chEvents));

        //                // Adjust positioning.
        //                y += control.Height + 5;

        //                // Send real patches.
        //                if (patch > PatternInfo.NO_PATCH)
        //                {
        //                    PatchChangeEvent evt = new(0, ch, patch);
        //                    MidiSend(evt);
        //                }
        //            }
        //        }

        //        //// Figure out times. Round up to bar.
        //        //int floor = lastSubdiv / (Common.PPQ * 4); // 4/4 only.
        //        //lastSubdiv = (floor + 1) * (Common.PPQ * 4);

        //        barBar.Length = new BarSpan(lastSubdiv);
        //        barBar.Start = BarSpan.Zero;
        //        barBar.End = barBar.Length - BarSpan.OneSubdiv;
        //        barBar.Current = BarSpan.Zero;
        //    }
        //}


        ///// <summary>
        ///// Dump current file to human readable.
        ///// </summary>
        //void Dump_Click(object? sender, EventArgs e)
        //{
        //    //_mfile.DrumChannel = _drumChannel;
        //    //var ds = _mfile.DumpSequentialEvents();
        //    var ds = _mdata.DumpGroupedEvents();

        //    if (ds.Count == 0)
        //    {
        //        ds.Add("No data");
        //    }

        //    if (Common.Settings.DumpToClip)
        //    {
        //        Clipboard.SetText(string.Join(Environment.NewLine, ds));
        //        LogMessage("INF", "File dumped to clipboard");
        //    }
        //    else
        //    {
        //        using SaveFileDialog dumpDlg = new() { Title = "Dump to file", FileName = "dump.csv" };
        //        if (dumpDlg.ShowDialog() == DialogResult.OK)
        //        {
        //            File.WriteAllLines(dumpDlg.FileName, ds.ToArray());
        //        }
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void Export_Click(object? sender, EventArgs e)
        //{
        //    if (Directory.Exists(Common.Settings.ExportPath))
        //    {
        //        string basefn = Path.GetFileNameWithoutExtension(_mdata.FileName);

        //        if (_mdata.FileName.ToLower().EndsWith(".sty"))
        //        {
        //            foreach (var item in lbPatterns.Items)
        //            {
        //                var pattern = item.ToString()!;
        //                var newfn = Path.Join(Common.Settings.ExportPath, $"{basefn}_{pattern.Replace(' ', '_')}.mid");
        //                var info = $"Export {pattern} from {_mdata.FileName}";

        //                ExportMidi(newfn, pattern, info);
        //            }
        //            LogMessage("INF", $"Style file {_mdata.FileName} exported to {Common.Settings.ExportPath}");
        //        }
        //        else // .mid
        //        {
        //            var newfn = Path.Join(Common.Settings.ExportPath, $"{basefn}_export.mid");
        //            var info = $"Export {_mdata.FileName}";

        //            ExportMidi(newfn, "", info);
        //        }
        //        LogMessage("INF", $"Midi file {_mdata.FileName} exported to {Common.Settings.ExportPath}");
        //    }
        //    else
        //    {
        //        LogMessage("ERR", "Invalid export path in your settings");
        //    }
        //}

        ///// <summary>
        ///// Output part of the file to a new midi file.
        ///// </summary>
        ///// <param name="fn">Where to put the midi file.</param>
        ///// <param name="pattern">Specific pattern if a style file.</param>
        ///// <param name="info">Extra info to add to midi file.</param>
        //void ExportMidi(string fn, string pattern, string info)
        //{
        //    // Get pattern info.
        //    PatternInfo pinfo = _mdata.Patterns.First(p => p.Name == pattern);

        //    // Init output file contents.
        //    MidiEventCollection outColl = new(1, Common.PPQ);
        //    IList<MidiEvent> outEvents = outColl.AddTrack();
            
        //    // Tempo.
        //    outEvents.Add(new TempoEvent(0, 0) { Tempo = sldTempo.Value });

        //    // General info.
        //    outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

        //    // Optional.
        //    if (pinfo.TimeSig != "")
        //    {
        //        //mevents.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, 8));
        //    }
        //    if (pinfo.KeySig != "")
        //    {
        //        //mevents.Add(new KeySignatureEvent(0, 0, 0));
        //    }

        //    // Patches.
        //    for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
        //    {
        //        if (pinfo.Patches[i] >= 0)
        //        {
        //            outEvents.Add(new PatchChangeEvent(0, i + 1, pinfo.Patches[i]));
        //        }
        //    }

        //    lock (_channels)
        //    {
        //        // Combine the midi events for current pattern ordered by timestamp.
        //        List<MidiEvent> allEvts = new();
        //        foreach (var (control, events) in _channels)
        //        {
        //            events.MidiEvents.ForEach(kv =>
        //            {
        //                // TODO adjust velocity for noteon based on slider values? or normalize?
        //                kv.Value.ForEach(e =>
        //                {
        //                    e.AbsoluteTime = kv.Key;
        //                    e.Channel = control.ChannelNumber;
        //                    allEvts.Add(e);
        //                });
        //            });

        //            foreach (var channelEventTime in events.MidiEvents)
        //            {
        //                var theEvents = channelEventTime.Value;
        //                foreach (var outEvent in theEvents)
        //                {
        //                    outEvent.AbsoluteTime = channelEventTime.Key;
        //                    outEvent.Channel = control.ChannelNumber;
        //                    allEvts.Add(outEvent);
        //                }
        //            }
        //        }

        //        // Copy to output.
        //        allEvts.OrderBy(e => e.AbsoluteTime).ForEach(e =>
        //        {
        //            outEvents.Add(e);
        //        });
        //    }


        //    // End track.
        //    long ltime = outEvents.Last().AbsoluteTime;
        //    var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
        //    outEvents.Add(endt);

        //    NAudio.Midi.MidiFile.Export(fn, outColl);
        //}

    }
}
