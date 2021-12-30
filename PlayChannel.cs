using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiStyleExplorer
{
    /// <summary>Channel events and other properties.</summary>
    public class PlayChannel
    {
        // TODO UI stuff
        /// <summary>Actual 1-based midi channel number for UI.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For UI.</summary>
        public string Name { get; set; } = "";

        /// <summary>For muting/soloing.</summary>
        public PlayMode Mode { get; set; } = PlayMode.Normal;

        /// <summary>Optional patch.</summary>
        public int Patch { get; set; } = -1;



        #region Properties
        /// <summary>Channel used.</summary>
        public bool Valid { get { return Events.Count > 0; } }

        /// <summary>Music or control/meta.</summary>
        public bool HasNotes { get; private set; } = false;

        ///<summary>The main collection of events. The key is the subdiv/time.</summary>
        public Dictionary<int, List<MidiEvent>> Events { get; set; } = new();

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;
        #endregion

        /// <summary>Add an event at the given subdiv.</summary>
        /// <param name="subdiv"></param>
        /// <param name="evt"></param>
        public void AddEvent(int subdiv, MidiEvent evt)
        {
            if (!Events.ContainsKey(subdiv))
            {
                Events.Add(subdiv, new List<MidiEvent>());
            }
            Events[subdiv].Add(evt);
            MaxSubdiv = Math.Max(MaxSubdiv, subdiv);
            HasNotes |= evt is NoteEvent;
        }

        /// <summary>
        /// Clean up before reading another pattern.
        /// </summary>
        public void Reset()
        {
            HasNotes = false;
            Events.Clear();
            MaxSubdiv = 0;
        }

        /// <summary>For viewing pleasure.</summary>
        //public override string ToString()
        //{
        //    return $"PlayChannel: Name:{Name} ChannelNumber:{ChannelNumber} Mode:{Mode} Events:{Events.Count} MaxSubdiv:{MaxSubdiv}";
        //}
    }
}
