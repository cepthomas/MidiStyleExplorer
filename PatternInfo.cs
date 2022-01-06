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
        /// <summary>Special value for Patches.</summary>
        public const int NO_CHANNEL = -2;

        /// <summary>Special value for Patches.</summary>
        public const int NO_PATCH = -1;

        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string Name { get; set; } = "";

        /// <summary>Tempo, if supplied by file. Defaults to 100 if missing.</summary>
        public int Tempo { get; set; } = 0;

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
                Patches[i] = NO_CHANNEL;
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
                string s;

                if (Patches[i] == NO_CHANNEL)
                {
                    s = "NoChannel";
                }
                else if (Patches[i] == NO_PATCH)
                {
                    s = "NoPatch";
                }
                else
                {
                    s = MidiDefs.GetInstrumentDef(Patches[i]);
                }

                if (Patches[i] != -1)
                {
                    content.Add($"Ch:{i + 1} Patch:{s}");
                }
            }

            return string.Join(' ', content);
        }
    }
}