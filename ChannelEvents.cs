using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiStyleExplorer
{
    public class ChannelEvents
    {
        /// <summary>Channel events and other properties.</summary>

        /// <summary>Contains data?</summary>
        public bool Valid { get { return Events.Count > 0; } }

        /// <summary>Music or control/meta.</summary>
        public bool HasNotes { get; private set; } = false;

        ///<summary>The main collection of events. The key is the subdiv/time.</summary>
        public Dictionary<int, List<MidiFile.Event>> Events { get; set; } = new();

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;


        /// <summary>Add an event at the given subdiv.</summary>
        /// <param name="subdiv"></param>
        /// <param name="evt"></param>
        public void AddEvent(int subdiv, MidiFile.Event evt)
        {
            if (!Events.ContainsKey(subdiv))
            {
                Events.Add(subdiv, new List<MidiFile.Event>());
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
            Events.Clear();
            HasNotes = false;
            MaxSubdiv = 0;
        }

        /// <summary>For viewing pleasure.</summary>
        //public override string ToString()
        //{
        //    return $"xxxxxxx: Name:{Name} ChannelNumber:{ChannelNumber} Mode:{Mode} Events:{Events.Count} MaxSubdiv:{MaxSubdiv}";
        //}


    }
}
