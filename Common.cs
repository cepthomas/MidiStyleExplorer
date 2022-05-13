using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiStyleExplorer
{
    #region Types
    /// <summary>Channel state.</summary>
    public enum PlayState { Normal = 0, Solo = 1, Mute = 2 }
    #endregion

    public class Common //TODOX clean this file.
    {
        #region Constants
        /// <summary>Only 4/4 time supported.</summary>
        public const int BEATS_PER_BAR = 4;

        /// <summary>Our internal ppq aka resolution - used for sending realtime midi messages.</summary>
        public const int PPQ = 32;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();
    }
}
