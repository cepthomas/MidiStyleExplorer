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




    /// <summary>
    /// Reads in and processes standard midi or yahama style files. Timestamps are from original file.
    /// Note: NAudio midi event channel numbers are 1-based.
    /// FUTURE Doesn't support multiple tracks. Would it be useful?
    /// </summary>
    public class MidiFile
    {
        /// <summary>
        /// Descriptor for one event.
        /// </summary>
        public record Event(string Pattern, int Channel, long AbsoluteTime, MidiEvent MidiEvent);

        #region Properties - file top level
        /// <summary>From where.</summary>
        public string Filename { get; private set; } = "";

        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int Tracks { get; private set; } = 0;

        /// <summary>Resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;
        #endregion

        #region Properties - patterns
        /// <summary>All file contents. Plain midi files will have only one.</summary>
        public List<PatternInfo> Patterns { get; private set; } = new();
        #endregion

        #region Properties set by client
        /// <summary>Sometimes drums are not on the default channel.</summary>
        public int DrumChannel { get; set; } = MidiDefs.DEFAULT_DRUM_CHANNEL;

        /// <summary>Don't include some events.</summary>
        public bool IgnoreNoisy { get; set; } = true;
        #endregion

        #region Fields
        /// <summary>All the midi events. This is the verbatim content of the file with no time scaling.</summary>
        List<Event> _allEvents = new();

        /// <summary>Save this for logging/debugging.</summary>
        long _lastStreamPos = 0;
        #endregion

        //TODO these:::

        /// <summary>
        /// Helper to get channels.
        /// </summary>
        /// <param name="pname">Specific pattern name.</param>
        /// <returns>List of channels in this pattern.</returns>
        public IEnumerable<int> GetChannels(string pname)
        {
            IEnumerable<int> channels = _allEvents.Where(e => e.Pattern == pname).Select(e => e.Channel).Distinct().OrderBy(e => e);
            return channels;
        }
        public IEnumerable<int> GetChannels()
        {
            IEnumerable<int> channels = _allEvents.Select(e => e.Channel).Distinct().OrderBy(e => e);
            return channels;
        }

        /// <summary>
        /// Helper to get an event collection.
        /// </summary>
        /// <param name="papnamettern">Specific pattern name.</param>
        /// <param name="channel">Specific channel.</param>
        /// <returns>The collection or null if invalid.</returns>
        public IEnumerable<Event> GetEvents(string pname, int channel)
        {
            IEnumerable<Event> ret = _allEvents.Where(e => e.Pattern == pname && e.Channel == channel);
            return ret;
        }
        public IEnumerable<Event> GetEvents(string pname)
        {
            IEnumerable<Event> ret = _allEvents.Where(e => e.Pattern == pname);
            return ret;
        }

        #region Public methods
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fn"></param>
        public void ProcessFile(string fn)
        {
            // Init everything.
            _allEvents.Clear();
            Patterns.Clear();
            Patterns.Add(new()); // always at least one
            Filename = fn;
            DeltaTicksPerQuarterNote = 0;

            using var br = new BinaryReader(File.OpenRead(fn));
            bool done = false;

            while (!done)
            {
                var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));

                //Capture(-1, "Section", -1, sectionName);

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
                        //Capture(-1, "Done", -1, "!!!");
                        done = true;
                        break;
                }
            }
        }
        #endregion

        #region Section readers
        /// <summary>
        /// Read the midi header section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadMThd(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            // Midi file type.
            MidiFileType = (int)Read(br, 2);
            //if (MidiFileType != 0)
            //{
            //    throw new FormatException($"This is type {MidiFileType} - must be 0");
            //}

            // Number of tracks.
            Tracks = (int)Read(br, 2);
            // if (Tracks != 1)
            // {
            //     throw new FormatException($"This has {Tracks} tracks - must be 1");
            // }

            // Resolution.
            DeltaTicksPerQuarterNote = (int)Read(br, 2);
        }

        /// <summary>
        /// Read a midi track chunk.
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        int ReadMTrk(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
            long startPos = br.BaseStream.Position;
            int absoluteTime = 0;

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
                        //Capture(evt.AbsoluteTime, "NoteOn", evt.Channel, evt.ToString());
                        break;

                    case NoteEvent evt:
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "NoteOff", evt.Channel, evt.ToString());
                        break;

                    case ControlChangeEvent evt:
                        if (!IgnoreNoisy)
                        {
                            AddMidiEvent(evt);
                            //Capture(evt.AbsoluteTime, "ControlChange", evt.Channel, evt.ToString());
                        }
                        break;

                    case PitchWheelChangeEvent evt:
                        if (!IgnoreNoisy)
                        {
                            //AddMidiEvent(evt);
                        }
                        break;

                    case PatchChangeEvent evt:
                        Patterns.Last().Patches[evt.Channel] = evt.Patch;
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "PatchChangeEvent", evt.Channel, evt.ToString());
                        break;

                    case SysexEvent evt:
                        if (!IgnoreNoisy)
                        {
                            AddMidiEvent(evt);
                            //string s = evt.ToString().Replace(Environment.NewLine, " ");
                            //Capture(evt.AbsoluteTime, "Sysex", evt.Channel, s);
                        }
                        break;

                    ///// Meta events /////
                    case TrackSequenceNumberEvent evt:
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "TrackSequenceNumber", evt.Channel, evt.ToString());
                        break;

                    case TempoEvent evt:
                        Patterns.Last().Tempo = evt.Tempo;
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "SetTempo", evt.Channel, evt.Tempo.ToString());
                        break;

                    case TimeSignatureEvent evt:
                        Patterns.Last().TimeSig = evt.TimeSignature;
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "TimeSignature", evt.Channel, evt.TimeSignature);
                        break;

                    case KeySignatureEvent evt:
                        Patterns.Last().KeySig = evt.ToString();
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "KeySignature", evt.Channel, evt.ToString());
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "SequenceTrackName", evt.Channel, evt.Text);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // Indicates start of a new midi pattern.
                        //Capture(evt.AbsoluteTime, "Marker", evt.Channel, evt.Text);
                        if(Patterns.Last().Name == "")
                        {
                            // It's the default/single pattern so update its name.
                            Patterns.Last().Name = evt.Text;
                        }
                        else
                        {
                            // Add a new pattern with defaults set to previous one.
                            Patterns.Add(new(Patterns.Last(), evt.Text));
                        }

                        absoluteTime = 0;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.TextEvent:
                        AddMidiEvent(evt);
                        //Capture(evt.AbsoluteTime, "TextEvent", evt.Channel, evt.Text);
                        break;

                    case MetaEvent evt when evt.MetaEventType == MetaEventType.EndTrack:
                        // Indicates end of current midi track.
                        //Capture(evt.AbsoluteTime, "EndTrack", evt.Channel, evt.ToString());
                        AddMidiEvent(evt);
                        //_currentPattern = "";
                        break;

                    default:
                        // Other MidiCommandCodes: AutoSensing, ChannelAfterTouch, ContinueSequence, Eox, KeyAfterTouch, StartSequence, StopSequence, TimingClock
                        // Other MetaEventType: Copyright, CuePoint, DeviceName, Lyric, MidiChannel, MidiPort, ProgramName, SequencerSpecific, SmpteOffset, TrackInstrumentName
                        //Capture(-1, "Other", -1, $"{me.GetType()} {me}");
                        break;
                }
            }

            ///// Local function. /////
            void AddMidiEvent(MidiEvent evt)
            {
                _allEvents.Add(new Event(Patterns.Last().Name, evt.Channel, evt.AbsoluteTime, evt));
            }

            return absoluteTime;
        }

        /// <summary>
        /// Read the CASM section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCASM(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
        }

        /// <summary>
        /// Read the CSEG section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCSEG(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
        }

        /// <summary>
        /// Read the Sdec section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadSdec(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Ctab section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCtab(BinaryReader br)
        {
            // Has some key and chord info.
            uint chunkSize = Read(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Cntt section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCntt(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the OTSc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadOTSc(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the FNRc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadFNRc(BinaryReader br)
        {
            uint chunkSize = Read(br, 4);
            br.ReadBytes((int)chunkSize);
        }
        #endregion

        #region Output formatters
        /// <summary>
        /// Dump the contents in a csv readable form.
        /// This is as the events appear in the original file plus some other stuff for debugging.
        /// </summary>
        /// <returns></returns>
        public List<string> GetReadableContents()
        {
            List<string> contents = new();
            contents.Add($"Timestamp,Type,Pattern,Channel,FilePos,Content");

            _allEvents.OrderBy(v => v.AbsoluteTime).
                ForEach(evt => contents.Add($"{evt.AbsoluteTime},{evt.GetType()},{evt.Pattern},{evt.Channel},{_lastStreamPos},{evt}"));

            return contents;
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel.
        /// </summary>
        /// <returns></returns>
        public List<string> GetReadableGrouped()
        {
            List<string> meta = new()
            {
                $"---Meta---",
                $"Meta,Value",
                $"MidiFileType,{MidiFileType}",
                $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                //$"StartAbsoluteTime,{StartAbsoluteTime}",
                $"Tracks,{Tracks}"
            };

            List<string> notes = new()
            {
                $"",
                $"---Notes---",
                "Time,Event,Channel,Pattern,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> other = new()
            {
                $"",
                $"---Other---",
                "Time,Event,Channel,Pattern,Val1,Val2,Val3",
            };

            foreach (var me in _allEvents)
            {
                // Boilerplate.
                string ntype = me.MidiEvent.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{ntype},{me.MidiEvent.Channel},{me.Pattern}";

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
            ret.AddRange(notes);
            ret.AddRange(other);

            return ret;
        }

        /// <summary>
        /// Output part or all of the file to a new midi file.
        /// </summary>
        /// <param name="fn">Where to put the midi.</param>
        /// <param name="pattern">Specific pattern if a style file.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        public void ExportMidi(string fn, string pattern, string info)
        {
            // Get pattern info. This should work for the simple midi file case too.
            PatternInfo pinfo = Patterns.First(p => p.Name == pattern);

            MidiEventCollection mecoll = new(1, DeltaTicksPerQuarterNote);
            IList<MidiEvent> mevents = mecoll.AddTrack();

            // Tempo.
            mevents.Add(new TempoEvent(0, 0) { Tempo = pinfo.Tempo });

            // General info.
            mevents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pinfo.TimeSig != "")
            {
                //mevents.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, 8));
            }
            if (pinfo.KeySig != "")
            {
                //mevents.Add(new KeySignatureEvent(0, 0, 0));
            }

            // Patches.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if(pinfo.Patches[i] != -1)
                {
                    mevents.Add(new PatchChangeEvent(0, i + 1, pinfo.Patches[i]));
                }
            }
            // Collect the midi events for this pattern ordered by timestamp.
            IEnumerable<Event> evts = _allEvents.Where(e => e.Pattern == pattern).OrderBy(e => e.AbsoluteTime);
            long ltime = evts.Last().AbsoluteTime;

            // Copy to output.
            evts.ForEach(e => mevents.Add(e.MidiEvent));

            // End track.
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            mevents.Add(endt);

            NAudio.Midi.MidiFile.Export(fn, mecoll);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Save an event for internal processing.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="etype"></param>
        /// <param name="channel"></param>
        /// <param name="content"></param>
        void Capture(long timestamp, string etype, int channel, string content)
        {
            //ReadableContents.Add($"{timestamp},{etype},{_currentPattern},{channel},{_lastStreamPos},{content.Replace(',', '_')}");
        }

        /// <summary>
        /// Read a number from stream and adjust endianess.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        uint Read(BinaryReader br, int size)
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
