namespace MidiStyleExplorer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnSettings = new System.Windows.Forms.ToolStripButton();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.btnAutoplay = new System.Windows.Forms.ToolStripButton();
            this.btnLoop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLogMidi = new System.Windows.Forms.ToolStripButton();
            this.btnKillMidi = new System.Windows.Forms.ToolStripButton();
            this.txtViewer = new NBagOfUis.TextViewer();
            this.sldVolume = new NBagOfUis.Slider();
            this.btnRewind = new System.Windows.Forms.Button();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.barBar = new NBagOfUis.BarBar();
            this.sldTempo = new NBagOfUis.Slider();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lbPatterns = new System.Windows.Forms.ListBox();
            this.ftree = new NBagOfUis.FilTree();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileDropDownButton,
            this.btnSettings,
            this.btnAbout,
            this.btnAutoplay,
            this.btnLoop,
            this.toolStripSeparator1,
            this.btnLogMidi,
            this.btnKillMidi});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1110, 27);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // fileDropDownButton
            // 
            this.fileDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.fileDropDownButton.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_37_file;
            this.fileDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fileDropDownButton.Name = "fileDropDownButton";
            this.fileDropDownButton.Size = new System.Drawing.Size(34, 24);
            this.fileDropDownButton.Text = "File";
            this.fileDropDownButton.ToolTipText = "File operations";
            this.fileDropDownButton.DropDownOpening += new System.EventHandler(this.File_DropDownOpening);
            // 
            // btnSettings
            // 
            this.btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSettings.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_137_cogwheel;
            this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(29, 24);
            this.btnSettings.Text = "toolStripButton1";
            this.btnSettings.ToolTipText = "Make it your own";
            this.btnSettings.Click += new System.EventHandler(this.Settings_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAbout.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_195_question_sign;
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(29, 24);
            this.btnAbout.Text = "toolStripButton1";
            this.btnAbout.ToolTipText = "Get some info";
            this.btnAbout.Click += new System.EventHandler(this.About_Click);
            // 
            // btnAutoplay
            // 
            this.btnAutoplay.CheckOnClick = true;
            this.btnAutoplay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAutoplay.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_221_play_button;
            this.btnAutoplay.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAutoplay.Name = "btnAutoplay";
            this.btnAutoplay.Size = new System.Drawing.Size(29, 24);
            this.btnAutoplay.Text = "toolStripButton1";
            this.btnAutoplay.ToolTipText = "Autoplay the selection";
            // 
            // btnLoop
            // 
            this.btnLoop.CheckOnClick = true;
            this.btnLoop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnLoop.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_82_refresh;
            this.btnLoop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLoop.Name = "btnLoop";
            this.btnLoop.Size = new System.Drawing.Size(29, 24);
            this.btnLoop.Text = "toolStripButton1";
            this.btnLoop.ToolTipText = "Loop forever";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // btnLogMidi
            // 
            this.btnLogMidi.CheckOnClick = true;
            this.btnLogMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnLogMidi.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_170_record;
            this.btnLogMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLogMidi.Name = "btnLogMidi";
            this.btnLogMidi.Size = new System.Drawing.Size(29, 24);
            this.btnLogMidi.Text = "toolStripButton1";
            this.btnLogMidi.ToolTipText = "Enable logging midi events";
            // 
            // btnKillMidi
            // 
            this.btnKillMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnKillMidi.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_242_flash;
            this.btnKillMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnKillMidi.Name = "btnKillMidi";
            this.btnKillMidi.Size = new System.Drawing.Size(29, 24);
            this.btnKillMidi.Text = "toolStripButton1";
            this.btnKillMidi.ToolTipText = "Kill all midi channels";
            this.btnKillMidi.Click += new System.EventHandler(this.Kill_Click);
            // 
            // txtViewer
            // 
            this.txtViewer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtViewer.Location = new System.Drawing.Point(8, 577);
            this.txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtViewer.MaxText = 5000;
            this.txtViewer.Name = "txtViewer";
            this.txtViewer.Size = new System.Drawing.Size(648, 128);
            this.txtViewer.TabIndex = 58;
            this.txtViewer.WordWrap = true;
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DecPlaces = 1;
            this.sldVolume.DrawColor = System.Drawing.Color.Fuchsia;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(558, 103);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sldVolume.Maximum = 2D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.ResetValue = 0D;
            this.sldVolume.Size = new System.Drawing.Size(98, 40);
            this.sldVolume.TabIndex = 42;
            this.sldVolume.Value = 1D;
            // 
            // btnRewind
            // 
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_173_rewind;
            this.btnRewind.Location = new System.Drawing.Point(613, 41);
            this.btnRewind.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(43, 49);
            this.btnRewind.TabIndex = 39;
            this.btnRewind.UseVisualStyleBackColor = false;
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(558, 41);
            this.chkPlay.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(43, 49);
            this.chkPlay.TabIndex = 41;
            this.chkPlay.UseVisualStyleBackColor = false;
            // 
            // barBar
            // 
            this.barBar.BeatsPerBar = 4;
            this.barBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.barBar.FontLarge = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.FontSmall = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.Location = new System.Drawing.Point(669, 40);
            this.barBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.barBar.MarkerColor = System.Drawing.Color.Black;
            this.barBar.Name = "barBar";
            this.barBar.ProgressColor = System.Drawing.Color.NavajoWhite;
            this.barBar.Size = new System.Drawing.Size(399, 50);
            this.barBar.Snap = NBagOfUis.BarBar.SnapType.Bar;
            this.barBar.SubdivsPerBeat = 8;
            this.barBar.TabIndex = 82;
            this.toolTip.SetToolTip(this.barBar, "Time in bar:beat:subdivision");
            this.barBar.ZeroBased = false;
            // 
            // sldTempo
            // 
            this.sldTempo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldTempo.DecPlaces = 0;
            this.sldTempo.DrawColor = System.Drawing.Color.White;
            this.sldTempo.Label = "BPM";
            this.sldTempo.Location = new System.Drawing.Point(558, 154);
            this.sldTempo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.sldTempo.Maximum = 200D;
            this.sldTempo.Minimum = 50D;
            this.sldTempo.Name = "sldTempo";
            this.sldTempo.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldTempo.ResetValue = 50D;
            this.sldTempo.Size = new System.Drawing.Size(98, 40);
            this.sldTempo.TabIndex = 80;
            this.toolTip.SetToolTip(this.sldTempo, "Tempo adjuster");
            this.sldTempo.Value = 100D;
            this.sldTempo.ValueChanged += new System.EventHandler(this.Tempo_ValueChanged);
            // 
            // lbPatterns
            // 
            this.lbPatterns.BackColor = System.Drawing.SystemColors.Control;
            this.lbPatterns.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbPatterns.FormattingEnabled = true;
            this.lbPatterns.ItemHeight = 20;
            this.lbPatterns.Location = new System.Drawing.Point(558, 202);
            this.lbPatterns.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lbPatterns.Name = "lbPatterns";
            this.lbPatterns.Size = new System.Drawing.Size(98, 342);
            this.lbPatterns.TabIndex = 88;
            this.toolTip.SetToolTip(this.lbPatterns, "All patterns in style file");
            this.lbPatterns.SelectedIndexChanged += new System.EventHandler(this.Patterns_SelectedIndexChanged);
            // 
            // ftree
            // 
            this.ftree.Location = new System.Drawing.Point(8, 32);
            this.ftree.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.ftree.Name = "ftree";
            this.ftree.SingleClickSelect = true;
            this.ftree.Size = new System.Drawing.Size(527, 536);
            this.ftree.TabIndex = 89;
            this.ftree.FileSelectedEvent += new System.EventHandler<string>(this.Navigator_FileSelectedEvent);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1110, 709);
            this.Controls.Add(this.ftree);
            this.Controls.Add(this.barBar);
            this.Controls.Add(this.sldTempo);
            this.Controls.Add(this.lbPatterns);
            this.Controls.Add(this.txtViewer);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.chkPlay);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.btnRewind);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "Midi Style Explorer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton fileDropDownButton;
        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.CheckBox chkPlay;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.ToolTip toolTip;
        private NBagOfUis.BarBar barBar;
        private NBagOfUis.Slider sldTempo;
        private System.Windows.Forms.ListBox lbPatterns;
        private System.Windows.Forms.ToolStripButton btnAutoplay;
        private System.Windows.Forms.ToolStripButton btnLoop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private NBagOfUis.FilTree ftree;
    }
}

