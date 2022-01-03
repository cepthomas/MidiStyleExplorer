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
        public int Tempo { get; set; } = 100;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based channel number.</summary>
        public int[] Patches { get; set; } = new int[MidiDefs.NUM_CHANNELS];

        /// <summary>Normal constructor.</summary>
        public PatternInfo()
        {
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>Copy constructor for defaults in case the new pattern doesn't change any.</summary>
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

        /// <summary>
        /// Readable version.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> content = new();
            content.Add($"Name:{(Name == "" ? "None" : Name)}");
            content.Add($"Tempo:{Tempo}");

            if (TimeSig != "")
            {
                content.Add($"TimeSig:{TimeSig}");
            }

            if (KeySig != "")
            {
                content.Add($"KeySig:{KeySig}");
            }

            for(int i = 0; i <MidiDefs.NUM_CHANNELS; i++)
            {
                if(Patches[i] != -1)
                {
                    content.Add($"Ch:{i + 1} Patch:{MidiDefs.GetInstrumentDef(Patches[i])}");
                }
            }

            return string.Join(' ', content);
        }
    }
}