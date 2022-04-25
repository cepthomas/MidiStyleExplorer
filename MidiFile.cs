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
    /// <summary>
    /// Reads in and processes standard midi or yamaha style files. Timestamps are from original file.
    /// </summary>
    public class MidiFile
    {
        #region Types
        /// <summary>Queryable descriptor for a midi event.</summary>
        public record EventDesc(string Pattern, int Channel, long AbsoluteTime, int ScaledTime, MidiEvent MidiEvent);
        #endregion

        #region Properties - file specific
        /// <summary>From where.</summary>
        public string Filename { get; private set; } = "";

        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int Tracks { get; private set; } = 0;

        /// <summary>Resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;
        #endregion

        #region Properties - patterns and events
        /// <summary>All file pattern sections. Plain midi files will have only one/unnamed.</summary>
        public List<PatternInfo> Patterns { get; private set; } = new();

        /// <summary>All the midi events. This is the verbatim content of the file.</summary>
        public List<EventDesc> AllEvents { get; private set; } = new();

        /// <summary>Just the music notes.</summary>
        public List<EventDesc> NoteEvents { get; private set; } = new();
        #endregion

        #region Properties set by client
        /// <summary>Sometimes drums are not on the default channel.</summary>
        public int DrumChannel { get; set; } = MidiDefs.DEFAULT_DRUM_CHANNEL;

        #endregion

        #region Fields
        /// <summary>Include events like controller changes, pitch wheel, ...</summary>
        bool _includeNoisy = false;

        /// <summary>Save this for logging/debugging.</summary>
        long _lastStreamPos = 0;

        /// <summary>Default values if not supplied in pattern. Mainly for managing patches.</summary>
        PatternInfo _patternDefaults = new() { Tempo = 100 };
        #endregion

        #region Public methods
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fn"></param>
        public void Read(string fn, bool includeNoisy = false)
        {
            //TODO: auto-determine which channels have drums? adjust quiet drums. see 2 non-std drums in "C:\\Users\\cepth\\OneDrive\\Audio\\Midi\\styles\\Gary USB\\g-70 styles\\G-70 #1\\ContempBeat_G70.S423.STY",

            _includeNoisy = includeNoisy;

            // Init everything.
            AllEvents.Clear();
            Patterns.Clear();
            Patterns.Add(new()); // always at least one
            Filename = fn;
            DeltaTicksPerQuarterNote = 0;
            _lastStreamPos = 0;

            using var br = new BinaryReader(File.OpenRead(fn));
            bool done = false;

            while (!done)
            {
                var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));

                Debug.WriteLine($"{sectionName}:{_lastStreamPos}");

                switch (sectionName)
                {
                    case "MThd":
                        ReadMThd(br);
                        break;

                    case "MTrk":
                        ReadMTrk(br);
                        break;

                    case "CASM":
                        ReadCASM(br);
                        break;

                    case "CSEG":
                        ReadCSEG(br);
                        break;

                    case "Sdec":
                        ReadSdec(br);
                        break;

                    case "Ctab":
                        ReadCtab(br);
                        break;

                    case "Cntt":
                        ReadCntt(br);
                        break;

                    case "OTSc": // One Touch Setting section
                        ReadOTSc(br);
                        break;

                    case "FNRc": // MDB (Music Finder) section
                        ReadFNRc(br);
                        break;

                    default:
                        done = true;
                        break;
                }
            }

            // Last one.
            CleanUpPattern();
        }
        #endregion

        #region Section readers
        /// <summary>
        /// Read the midi header section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadMThd(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            // Midi file type.
            MidiFileType = (int)ReadStream(br, 2);
            //if (MidiFileType != 0)
            //{
            //    throw new FormatException($"This is type {MidiFileType} - must be 0");
            //}

            // Number of tracks.
            Tracks = (int)ReadStream(br, 2);
            // if (Tracks != 1)
            // {
            //     throw new FormatException($"This has {Tracks} tracks - must be 1");
            // }

            // Resolution.
            DeltaTicksPerQuarterNote = (int)ReadStream(br, 2);
        }

        /// <summary>
        /// Read a midi track chunk.
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        int ReadMTrk(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            long startPos = br.BaseStream.Position;
            int absoluteTime = 0;

            // Scale to time increments used by application.
            MidiTime mt = new()
            {
                InternalPpq = Common.PPQ,
                MidiPpq = DeltaTicksPerQuarterNote
            };

            // Read all midi events.
            MidiEvent? me = null; // current
            while (br.BaseStream.Position < startPos + chunkSize)
            {
                _lastStreamPos = br.BaseStream.Position;

                me = MidiEvent.ReadNextEvent(br, me);
                absoluteTime += me.DeltaTime;
                me.AbsoluteTime = absoluteTime;

                switch (me)
                {
                    ///// Standard midi events /////
                    case NoteOnEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case NoteEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case ControlChangeEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    case PitchWheelChangeEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    case PatchChangeEvent evt:
                        var index = evt.Channel - 1;
                        _patternDefaults.Patches[index] = evt.Patch;
                        Patterns.Last().Patches[index] = evt.Patch;
                        AddMidiEvent(evt);
                        break;

                    case SysexEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    ///// Meta events /////
                    case TrackSequenceNumberEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case TempoEvent evt:
                        var tempo = (int)Math.Round(evt.Tempo);
                        _patternDefaults.Tempo = tempo;
                        Patterns.Last().Tempo = tempo;
                        AddMidiEvent(evt);
                        break;

                    case TimeSignatureEvent evt:
                        var tsig = evt.TimeSignature;
                        _patternDefaults.TimeSig = tsig;
                        Patterns.Last().TimeSig = tsig;
                        AddMidiEvent(evt);
                        break;

                    case KeySignatureEvent evt:
                        var ksig = evt.ToString();
                        _patternDefaults.KeySig = ksig;
                        Patterns.Last().KeySig = ksig;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // Indicates start of a new midi pattern.
                        if(Patterns.Last().Name == "")
                        {
                            // It's the default/single pattern so update its name.
                            Patterns.Last().Name = evt.Text;
                        }
                        else
                        {
                            // Tidy up missing parts of current info.
                            CleanUpPattern();

                            // Add a new pattern with defaults set to previous one.
                            Patterns.Add(new PatternInfo() { Name = evt.Text });
                        }

                        absoluteTime = 0;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.TextEvent:
                        AddMidiEvent(evt);
                        break;

                    case MetaEvent evt when evt.MetaEventType == MetaEventType.EndTrack:
                        // Indicates end of current midi track.
                        AddMidiEvent(evt);
                        //_currentPattern = "";
                        break;

                    default:
                        // Other MidiCommandCodes: AutoSensing, ChannelAfterTouch, ContinueSequence, Eox, KeyAfterTouch, StartSequence, StopSequence, TimingClock
                        // Other MetaEventType: Copyright, CuePoint, DeviceName, Lyric, MidiChannel, MidiPort, ProgramName, SequencerSpecific, SmpteOffset, TrackInstrumentName
                        break;
                }
            }

            ///// Local function. /////
            void AddMidiEvent(MidiEvent evt)
            {
                int scaledTime = mt.MidiToInternal(evt.AbsoluteTime);
                var pi = Patterns.Last();
                AllEvents.Add(new EventDesc(pi.Name, evt.Channel, evt.AbsoluteTime, scaledTime, evt));
            }

            return absoluteTime;
        }

        /// <summary>
        /// Read the CASM section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCASM(BinaryReader br)
        {
            /*uint chunkSize =*/ ReadStream(br, 4);
        }

        /// <summary>
        /// Read the CSEG section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCSEG(BinaryReader br)
        {
            /*uint chunkSize =*/ ReadStream(br, 4);
        }

        /// <summary>
        /// Read the Sdec section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadSdec(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Ctab section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCtab(BinaryReader br)
        {
            // Has some key and chord info.
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Cntt section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCntt(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the OTSc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadOTSc(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the FNRc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadFNRc(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }
        #endregion

        #region Output formatters
        /// <summary>
        /// Dump the contents in a csv readable form.
        /// This is as the events appear in the original file.
        /// </summary>
        /// <returns></returns>
        public List<string> DumpSequentialEvents()
        {
            List<string> contents = new();
            contents.Add("AbsoluteTime,Event,Pattern,Channel,Content");

            AllEvents.OrderBy(v => v.AbsoluteTime).
                ForEach(evt => contents.Add($"{evt.AbsoluteTime},{evt.MidiEvent.GetType().ToString().Replace("NAudio.Midi.", "")}," +
                $"{evt.Pattern},{evt.Channel},{evt.MidiEvent}"));

            return contents;
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel.
        /// </summary>
        /// <returns></returns>
        public List<string> DumpGroupedEvents()
        {
            bool includeOther = false;

            List<string> meta = new()
            {
                $"---------------------Meta---------------------",
                $"Meta,Value",
                $"MidiFileType,{MidiFileType}",
                $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                $"Tracks,{Tracks}"
            };

            List<string> patterns = new()
            {
                "",
                "---------------------Patterns---------------------",
                "Name,Tempo,TimeSig,KeySig,Patches",
            };

            foreach(var pattern in Patterns)
            {
                StringBuilder sb = new();
                for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                {
                    if (pattern.Patches[i] >= 0)
                    {
                        string spatch = (i + 1 == DrumChannel) ? "Drums" : MidiDefs.GetInstrumentDef(pattern.Patches[i]);
                        sb.Append($"Ch{i + 1}:{spatch} ");
                    }
                }
                patterns.Add($"{pattern.Name},{pattern.Tempo},{pattern.TimeSig},{pattern.KeySig},{sb}");
            }

            List<string> notes = new()
            {
                "",
                "---------------------Notes---------------------",
                "AbsoluteTime,Pattern,Channel,Event,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> other = new()
            {
                "",
                "---------------------Other---------------------",
                "AbsoluteTime,Pattern,Channel,Event,Val1,Val2,Val3",
            };

            string lastPattern = "";
            foreach (var me in AllEvents)
            {
                if(me.Pattern != lastPattern)
                {
                    notes.Add($"---------------------{me.Pattern}-------------------------");
                    other.Add($"---------------------{me.Pattern}-------------------------");
                    lastPattern = me.Pattern;
                }

                // Boilerplate.
                string ntype = me.MidiEvent.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{me.Pattern},{me.MidiEvent.Channel},{ntype}";

                switch (me.MidiEvent)
                {
                    case NoteOnEvent evt:
                        int len = evt.OffEvent is null ? 0 : evt.NoteLength; // NAudio NoteLength bug.
                        string nname = evt.Channel == DrumChannel ? $"{MidiDefs.GetDrumDef(evt.NoteNumber)}" : $"{MidiDefs.NoteNumberToName(evt.NoteNumber)}";
                        notes.Add($"{sc},{evt.NoteNumber},{nname},{evt.Velocity},{len}");
                        break;

                    case NoteEvent evt: // used for NoteOff
                        notes.Add($"{sc},{evt.NoteNumber},,{evt.Velocity},");
                        break;

                    case TempoEvent evt:
                        meta.Add($"Tempo,{evt.Tempo}");
                        other.Add($"{sc},{evt.Tempo},{evt.MicrosecondsPerQuarterNote}");
                        break;

                    case TimeSignatureEvent evt:
                        other.Add($"{sc},{evt.TimeSignature},,");
                        break;

                    case KeySignatureEvent evt:
                        other.Add($"{sc},{evt.SharpsFlats},{evt.MajorMinor},");
                        break;

                    case PatchChangeEvent evt:
                        string pname = evt.Channel == DrumChannel ? $"" : $"{MidiDefs.GetInstrumentDef(evt.Patch)}"; // drum kit?
                        other.Add($"{sc},{evt.Patch},{pname},");
                        break;

                    case ControlChangeEvent evt:
                        other.Add($"{sc},{(int)evt.Controller},{MidiDefs.GetControllerDef((int)evt.Controller)},{evt.ControllerValue}");
                        break;

                    case PitchWheelChangeEvent evt:
                        other.Add($"{sc},{evt.Pitch},,");
                        break;

                    case TextEvent evt:
                        other.Add($"{sc},{evt.Text},,,");
                        break;

                    //case ChannelAfterTouchEvent:
                    //case SysexEvent:
                    //case MetaEvent:
                    //case RawMetaEvent:
                    //case SequencerSpecificEvent:
                    //case SmpteOffsetEvent:
                    //case TrackSequenceNumberEvent:
                    default:
                        break;
                }
            }

            List<string> ret = new();
            ret.AddRange(meta);
            ret.AddRange(patterns);
            ret.AddRange(notes);
            if (includeOther)
            {
                ret.AddRange(other);
            }

            return ret;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Fill in missing info using defaults.
        /// </summary>
        void CleanUpPattern()
        {
            var pi = Patterns.Last();
            if (pi.Tempo == 0) pi.Tempo = _patternDefaults.Tempo;
            if (pi.TimeSig == "") pi.TimeSig = _patternDefaults.TimeSig;
            if (pi.KeySig == "") pi.KeySig = _patternDefaults.KeySig;
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if (pi.Patches[i] == PatternInfo.NO_PATCH && _patternDefaults.Patches[i] >= 0)
                {
                    pi.Patches[i] = _patternDefaults.Patches[i];
                }
            }
        }

        /// <summary>
        /// Read a number from stream and adjust endianess.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        uint ReadStream(BinaryReader br, int size)
        {
            uint i;

            _lastStreamPos = br.BaseStream.Position;

            switch (size)
            {
                case 2:
                    i = br.ReadUInt16();
                    if (BitConverter.IsLittleEndian)
                    {
                        i = (UInt16)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8));
                    }
                    break;

                case 4:
                    i = br.ReadUInt32();
                    if (BitConverter.IsLittleEndian)
                    {
                        i = ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24);
                    }
                    break;

                default:
                    throw new FormatException("Unsupported read size");
            }

            return i;
        }
        #endregion
    }
}
