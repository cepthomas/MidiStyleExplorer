using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiStyleExplorer
{
    /// <summary>Properties associated with a pattern.</summary>
    public class PatternInfo
    {
        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string Name { get; set; } = "";

        /// <summary>Tempo, if supplied by file. Defaults to 100 if missing.</summary>
        public double Tempo { get; set; } = 100.0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based channel number.</summary>
        public int[] Patches = new int[MidiDefs.NUM_CHANNELS];

        /// <summary>Normal constructor.</summary>
        public PatternInfo()
        {
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>Special copy constructor.</summary>
        public PatternInfo(PatternInfo src, string name)
        {
            Name = name;
            Tempo = src.Tempo;
            TimeSig = src.TimeSig;
            KeySig = src.KeySig;

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = src.Patches[i];
            }
        }
    }
}