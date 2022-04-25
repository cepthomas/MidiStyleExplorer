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
            set { _channelNumber = value; lblChannelNumber.Text = $"{_channelNumber}:"; }
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
        int _patch = PatternInfo.NO_PATCH;

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
            get { return lblDrums.BackColor == _selColor; }
            set { lblDrums.BackColor = value ? _selColor : _unselColor; lblPatch.Text = FormatPatch(); }
        }
        #endregion

        #region Fields
        Color _selColor = Common.Settings.ControlColor;
        Color _unselColor = SystemColors.Control;
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
            sldVolume.DrawColor = _selColor;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;
            lblChannelNumber.Click += ChannelNumber_Click;

            lblDrums.Click += Drums_Click;

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
                    lblSolo.BackColor = _unselColor;
                    lblMute.BackColor = _unselColor;
                    break;
                case PlayState.Solo:
                    lblSolo.BackColor = _selColor;
                    lblMute.BackColor = _unselColor;
                    break;
                case PlayState.Mute:
                    lblSolo.BackColor = _unselColor;
                    lblMute.BackColor = _selColor;
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
                bool soloSel = lblSolo.BackColor == _selColor;
                bool muteSel = lblMute.BackColor == _selColor;

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
        /// Handles drums button.
        /// </summary>
        void Drums_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                if (lblDrums.BackColor == _selColor)
                {
                    lblDrums.BackColor = _unselColor;
                }
                else
                {
                    lblDrums.BackColor = _selColor;
                }
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
            //string spatch = IsDrums ? "Drums" : _patch == PatternInfo.NO_PATCH ? "NoPatch" : MidiDefs.GetInstrumentDef(_patch);
            string spatch = _patch == PatternInfo.NO_PATCH ? "NoPatch" : MidiDefs.GetInstrumentDef(_patch);
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
            lblChannelNumber.BackColor = _selected ? _selColor : _unselColor;
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
