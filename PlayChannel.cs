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
    public partial class PlayChannel : UserControl
    {
        // TODDO UI aspects:
        // patch -> selector. event to host.
        // map S/M buttons to State. event to host
        // map vol to control

        ///////////////////////////////////////////////////////////////////////////
        ////////////////// TODO need home for these ///////////////////////////////
        ///////////////////////////////////////////////////////////////////////////

        /// <summary>Contains data?</summary>
        public bool Valid { get { return EventsXXX.Count > 0; } }

        /// <summary>Music or control/meta.</summary>
        public bool HasNotes { get; private set; } = false;

        ///<summary>The main collection of events. The key is the subdiv/time.</summary>
        public Dictionary<int, List<MidiFile.EventXXX>> EventsXXX { get; set; } = new();

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;

        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////





        /// <summary>Actual 1-based midi channel number for UI.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = value; lblNumber.Text = _channelNumber.ToString(); }
        }
        int _channelNumber = -1;

        /// <summary>For muting/soloing.</summary>
        public PlayMode Mode
        {
            get { return _mode; }
            set { _mode = value; SetModeUi(); }
        }
        PlayMode _mode = PlayMode.Normal;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public PlayChannel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PlayChannel_Load(object? sender, EventArgs e)
        {
            sldVolume.DrawColor = Common.Settings.ControlColor;
            chkSolo.BackColor = Common.Settings.ControlColor;
            chkMute.BackColor = Common.Settings.ControlColor;

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
            switch (_mode)
            {
                case PlayMode.Normal:
                    chkSolo.Checked = false;
                    chkMute.Checked = false;
                    break;
                case PlayMode.Solo:
                    chkSolo.Checked = true;
                    chkMute.Checked = false;
                    break;
                case PlayMode.Mute:
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

                _mode = PlayMode.Normal;
                if (chkSolo.Checked) { _mode = PlayMode.Solo; }
                else if (chkMute.Checked) { _mode = PlayMode.Mute; }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"PlayChannel: Patch:{Patch} ChannelNumber:{ChannelNumber} Mode:{Mode}";
        }
    }
}
