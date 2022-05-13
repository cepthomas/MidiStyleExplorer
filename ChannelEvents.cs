using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MidiStyleExplorer
{
    /// <summary>
    /// All the events associated with a channel. They have been adjusted from the original file
    /// to work with this application.
    /// </summary>
    public class ChannelEvents //TODOX remove 
    {
        /// <summary>Contains data?</summary>
        public bool Valid { get { return MidiEvents.Count > 0; } }

        /// <summary>Music or control/meta.</summary>
        public bool HasNotes { get; private set; } = false;

        ///<summary>The main collection of playable events for a channel/pattern. The key is the internal subdiv/time.</summary>
        public Dictionary<int, List<MidiEvent>> MidiEvents { get; set; } = new();

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;

        /// <summary>Add an event at the given subdiv.</summary>
        /// <param name="subdiv"></param>
        /// <param name="evt"></param>
        public void Add(int subdiv, MidiEvent evt)
        {
            if (!MidiEvents.ContainsKey(subdiv))
            {
                MidiEvents.Add(subdiv, new List<MidiEvent>());
            }

            MidiEvents[subdiv].Add(evt);
            MaxSubdiv = Math.Max(MaxSubdiv, subdiv);
            HasNotes |= evt is NoteEvent;
        }

        /// <summary>
        /// Clean up before reading another pattern.
        /// </summary>
        public void Reset()
        {
            MidiEvents.Clear();
            HasNotes = false;
            MaxSubdiv = 0;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"ChannelEvents: Valid:{Valid} Events:{MidiEvents.Count} MaxSubdiv:{MaxSubdiv}";
        }
    }
}
