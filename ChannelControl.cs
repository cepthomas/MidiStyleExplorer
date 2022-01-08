using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MidiStyleExplorer
{
    /// <summary>Channel events and other properties.</summary>
    public partial class ChannelControl : UserControl
    {
        #region Events
        /// <summary>Notify client of asynchronous changes.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        public class ChannelChangeEventArgs : EventArgs
        {
            public bool PatchChange { get; set; } = false;
            public bool StateChange { get; set; } = false;
        }
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number for UI.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = value; lblChannelNumber.Text = $"Ch:{_channelNumber}"; }
        }
        int _channelNumber = -1;

        /// <summary>For muting/soloing.</summary>
        public PlayState State
        {
            get { return _state; }
            set { _state = value; SetModeUi(); }
        }
        PlayState _state = PlayState.Normal;

        /// <summary>Current patch.</summary>
        public int Patch
        {
            get { return _patch; }
            set { _patch = Math.Min(value, MidiDefs.MAX_MIDI); lblPatch.Text = FormatPatch(); }
        }
        int _patch = PatternInfo.NO_CHANNEL;

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = Math.Min(value, 1.0); }
        }

        /// <summary>User has selected this channel.</summary>
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; FormatSelect(); }
        }
        bool _selected = false;

        /// <summary>This is the drum channel.</summary>
        public bool IsDrums
        {
            get { return _isDrums; }
            set { _isDrums = value; FormatPatch(); }
        }
        bool _isDrums = false;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_Load(object? sender, EventArgs e)
        {
            sldVolume.DrawColor = Common.Settings.ControlColor;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;
            lblChannelNumber.Click += ChannelNumber_Click;

            SetModeUi();
        }

        /// <summary>
        /// Draw mode checkboxes.
        /// </summary>
        void SetModeUi()
        {
            switch (_state)
            {
                case PlayState.Normal:
                    lblSolo.BackColor = SystemColors.Control;
                    lblMute.BackColor = SystemColors.Control;
                    break;
                case PlayState.Solo:
                    lblSolo.BackColor = Common.Settings.ControlColor;
                    lblMute.BackColor = SystemColors.Control;
                    break;
                case PlayState.Mute:
                    lblSolo.BackColor = SystemColors.Control;
                    lblMute.BackColor = Common.Settings.ControlColor;
                    break;
            }
        }

        /// <summary>
        /// Handles solo and mute.
        /// </summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                var lbl = sender as Label;

                // Figure out state.
                _state = PlayState.Normal; // default

                // Toggle control. Get current.
                bool soloSel = lblSolo.BackColor == Common.Settings.ControlColor;
                bool muteSel = lblMute.BackColor == Common.Settings.ControlColor;

                if (lbl == lblSolo)
                {
                    if(soloSel) // unselect
                    {
                        if(muteSel)
                        {
                            _state = PlayState.Mute;
                        }
                    }
                    else // select
                    {
                        _state = PlayState.Solo;
                    }
                }
                else // lblMute
                {
                    if (muteSel) // unselect
                    {
                        if (soloSel)
                        {
                            _state = PlayState.Solo;
                        }
                    }
                    else // select
                    {
                        _state = PlayState.Mute;
                    }
                }

                SetModeUi();
                ChannelChange?.Invoke(this, new ChannelChangeEventArgs() { StateChange = true });
            }
        }

        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "Select Patch",
                Size = new Size(300, 350),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            ListView lv = new()
            {
                Dock = DockStyle.Fill,
                View = View.List,
                HideSelection = false
            };

            lv.Items.Add("NoPatch");
            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                lv.Items.Add(MidiDefs.GetInstrumentDef(i));
            }

            lv.Click += (object? sender, EventArgs e) =>
            {
                int ind = lv.SelectedIndices[0];
                Patch = ind - 1; // skip NoPatch entry
                ChannelChange?.Invoke(this, new() { PatchChange = true });
                f.Close();
            };

            f.Controls.Add(lv);
            f.ShowDialog();
        }

        /// <summary>
        /// 
        /// </summary>
        string FormatPatch()
        {
            string spatch = "NoPatch"; // default

            if(IsDrums)
            {
                spatch = "Drums";
            }
            else if(_patch == PatternInfo.NO_CHANNEL)
            {
                spatch = "NoChannel";
            }
            else if (_patch >= 0 && _patch < MidiDefs.MAX_MIDI)
            {
                spatch = MidiDefs.GetInstrumentDef(_patch);
            }

            return spatch;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelNumber_Click(object? sender, EventArgs e)
        {
            _selected = !_selected;
            FormatSelect();
        }

        /// <summary>
        /// 
        /// </summary>
        void FormatSelect()
        {
            lblChannelNumber.BackColor = _selected ? Common.Settings.ControlColor : SystemColors.Control;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ChannelControl: ChannelNumber:{ChannelNumber} Patch:{FormatPatch()} State:{State}";
        }
    }
}
