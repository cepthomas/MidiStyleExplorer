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
            set { _channelNumber = value; lblNumber.Text = _channelNumber.ToString(); }
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
            get { return cmbPatch.SelectedIndex; }
            set { cmbPatch.SelectedIndex = Math.Min(value, MidiDefs.MAX_MIDI); }
        }

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = Math.Min(value, 1.0); }
        }
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
            chkSolo.BackColor = Common.Settings.ControlColor;
            chkMute.BackColor = Common.Settings.ControlColor;

            chkSolo.CheckedChanged += Check_Click;
            chkMute.CheckedChanged += Check_Click;
            cmbPatch.SelectedIndexChanged += Patch_SelectedIndexChanged;
            SetModeUi();

            // Fill patch list.
            for (int i = 0; i <= MidiDefs.MAX_MIDI; i++)
            {
                cmbPatch.Items.Add(MidiDefs.GetInstrumentDef(i));
            }
            cmbPatch.SelectedIndex = 0;
        }

        /// <summary>
        /// Draw mode checkboxes.
        /// </summary>
        void SetModeUi()
        {
            switch (_state)
            {
                case PlayState.Normal:
                    chkSolo.Checked = false;
                    chkMute.Checked = false;
                    break;
                case PlayState.Solo:
                    chkSolo.Checked = true;
                    chkMute.Checked = false;
                    break;
                case PlayState.Mute:
                    chkSolo.Checked = false;
                    chkMute.Checked = true;
                    break;
            }
        }

        /// <summary>
        /// Handles solo and mute.
        /// </summary>
        void Check_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                var chk = sender as CheckBox;

                // Fix UI logic.
                if (chk == chkSolo)
                {
                    chkMute.Checked = false;
                }
                else // chkMute
                {
                    chkSolo.Checked = false;
                }

                _state = PlayState.Normal;
                if (chkSolo.Checked) { _state = PlayState.Solo; }
                else if (chkMute.Checked) { _state = PlayState.Mute; }

                ChannelChange?.Invoke(this, new ChannelChangeEventArgs() { StateChange = true });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ChannelChange?.Invoke(this, new ChannelChangeEventArgs() { PatchChange = true });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ChannelControl: Patch:{Patch} ChannelNumber:{ChannelNumber} Mode:{State}";
        }
    }
}
