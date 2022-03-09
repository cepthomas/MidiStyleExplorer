using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Text.Json;
using System.Text.Json.Serialization;
using NAudio.Wave;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiStyleExplorer
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Root Directories")]
        [Description("Where to look in order as they appear.")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))] // TODO Should be a multi folder picker.
        public List<string> RootDirs { get; set; } = new();

        [DisplayName("Midi Output Device")]
        [Description("How to play the midi files.")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiOutDevice { get; set; } = "Microsoft GS Wavetable Synth";

        [DisplayName("Default Tempo")]
        [Description("Use this tempo if it's not in the file.")]
        [Browsable(true)]
        public int DefaultTempo { get; set; } = 100;

        [DisplayName("Dump To Clipboard")]
        [Description("Otherwise to file.")]
        [Browsable(true)]
        public bool DumpToClip { get; set; } = false;

        [DisplayName("Export Path")]
        [Description("Where To Export.")]
        [Browsable(true)]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string ExportPath { get; set; } = "";

        [DisplayName("Snap To Grid")]
        [Description("Snap to bar | beat | subdiv.")]
        [Browsable(true)]
        public BarBar.SnapType Snap { get; set; } = BarBar.SnapType.Bar;

        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        [JsonConverter(typeof(JsonRectangleConverter))]
        public Rectangle FormGeometry { get; set; } = new Rectangle(50, 50, 600, 400);

        [Browsable(false)]
        public bool Autoplay { get; set; } = true;

        [Browsable(false)]
        public bool Loop { get; set; } = false;

        [Browsable(false)]
        public double Volume { get; set; } = 0.5;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Non-persisted Properties
        [Browsable(false)]
        public bool Valid { get; set; } = false;
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "???";
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings set = new();
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                var jobj = JsonSerializer.Deserialize<UserSettings>(json);
                if (jobj is not null)
                {
                    set = jobj;
                    set._fn = fn;
                    set.Valid = true;
                }
            }
            else
            {
                // Doesn't exist, create a new one.
                set = new UserSettings()
                {
                    _fn = fn,
                    Valid = true
                };
            }

            return set;
        }

        /// <summary>Save object to file.</summary>
        public void Save()
        {
            if (Valid)
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(_fn, json);
            }
        }
        #endregion
    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "Latency":
                    rec = new List<string>() { "25", "50", "100", "150", "200", "300", "400", "500" };
                    break;

                case "WavOutDevice":
                    rec = new List<string>();
                    for (int id = -1; id < WaveOut.DeviceCount; id++) // –1 indicates the default output device, while 0 is the first output device
                    {
                        var cap = WaveOut.GetCapabilities(id);
                        rec.Add(cap.ProductName);
                    }
                    break;

                case "MidiOutDevice":
                    rec = new List<string>();
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;
            }

            return new StandardValuesCollection(rec);
        }
    }
}
