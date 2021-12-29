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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnSettings = new System.Windows.Forms.ToolStripButton();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.btnAutoplay = new System.Windows.Forms.ToolStripButton();
            this.btnLoop = new System.Windows.Forms.ToolStripButton();
            this.txtViewer = new NBagOfUis.TextViewer();
            this.sldVolume = new NBagOfUis.Slider();
            this.btnRewind = new System.Windows.Forms.Button();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.cmbPatchList = new System.Windows.Forms.ComboBox();
            this.txtPatchChannel = new System.Windows.Forms.TextBox();
            this.barBar = new NBagOfUis.BarBar();
            this.cgChannels = new NBagOfUis.ClickGrid();
            this.sldTempo = new NBagOfUis.Slider();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnKill = new System.Windows.Forms.Button();
            this.lbPatterns = new System.Windows.Forms.ListBox();
            this.chkDrumsOn1 = new System.Windows.Forms.CheckBox();
            this.chkLogMidi = new System.Windows.Forms.CheckBox();
            this.btnPatch = new System.Windows.Forms.Button();
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
            this.btnLoop});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1358, 27);
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
            this.btnAutoplay.Click += new System.EventHandler(this.btnAutoplay_Click);
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
            this.btnLoop.Click += new System.EventHandler(this.btnLoop_Click);
            // 
            // txtViewer
            // 
            this.txtViewer.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtViewer.Location = new System.Drawing.Point(20, 362);
            this.txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtViewer.MaxText = 5000;
            this.txtViewer.Name = "txtViewer";
            this.txtViewer.Size = new System.Drawing.Size(647, 283);
            this.txtViewer.TabIndex = 58;
            this.txtViewer.Text = "";
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DecPlaces = 1;
            this.sldVolume.DrawColor = System.Drawing.Color.Fuchsia;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(179, 43);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.ResetValue = 0D;
            this.sldVolume.Size = new System.Drawing.Size(100, 50);
            this.sldVolume.TabIndex = 42;
            this.sldVolume.Value = 0.5D;
            // 
            // btnRewind
            // 
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_173_rewind;
            this.btnRewind.Location = new System.Drawing.Point(71, 43);
            this.btnRewind.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(43, 49);
            this.btnRewind.TabIndex = 39;
            this.btnRewind.UseVisualStyleBackColor = false;
            this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(20, 43);
            this.chkPlay.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(43, 49);
            this.chkPlay.TabIndex = 41;
            this.chkPlay.UseVisualStyleBackColor = false;
            this.chkPlay.CheckedChanged += new System.EventHandler(this.Play_CheckedChanged);
            // 
            // cmbPatchList
            // 
            this.cmbPatchList.BackColor = System.Drawing.SystemColors.Control;
            this.cmbPatchList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatchList.DropDownWidth = 150;
            this.cmbPatchList.FormattingEnabled = true;
            this.cmbPatchList.Location = new System.Drawing.Point(466, 258);
            this.cmbPatchList.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmbPatchList.Name = "cmbPatchList";
            this.cmbPatchList.Size = new System.Drawing.Size(98, 28);
            this.cmbPatchList.TabIndex = 84;
            this.toolTip.SetToolTip(this.cmbPatchList, "Patch name");
            // 
            // txtPatchChannel
            // 
            this.txtPatchChannel.BackColor = System.Drawing.SystemColors.Control;
            this.txtPatchChannel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPatchChannel.Location = new System.Drawing.Point(534, 223);
            this.txtPatchChannel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtPatchChannel.Name = "txtPatchChannel";
            this.txtPatchChannel.Size = new System.Drawing.Size(30, 27);
            this.txtPatchChannel.TabIndex = 83;
            this.toolTip.SetToolTip(this.txtPatchChannel, "Channel number");
            // 
            // barBar
            // 
            this.barBar.BeatsPerBar = 4;
            this.barBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.barBar.FontLarge = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.FontSmall = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.Location = new System.Drawing.Point(466, 43);
            this.barBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.barBar.MarkerColor = System.Drawing.Color.Black;
            this.barBar.Name = "barBar";
            this.barBar.ProgressColor = System.Drawing.Color.NavajoWhite;
            this.barBar.Size = new System.Drawing.Size(699, 62);
            this.barBar.Snap = NBagOfUis.BarBar.SnapType.Bar;
            this.barBar.SubdivsPerBeat = 8;
            this.barBar.TabIndex = 82;
            this.toolTip.SetToolTip(this.barBar, "Time in bar:beat:subdivision");
            // 
            // cgChannels
            // 
            this.cgChannels.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cgChannels.Location = new System.Drawing.Point(721, 113);
            this.cgChannels.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cgChannels.Name = "cgChannels";
            this.cgChannels.Size = new System.Drawing.Size(444, 222);
            this.cgChannels.TabIndex = 81;
            this.toolTip.SetToolTip(this.cgChannels, "Midi channels with mute/solo");
            this.cgChannels.IndicatorEvent += new System.EventHandler<NBagOfUis.IndicatorEventArgs>(this.Channels_IndicatorEvent);
            // 
            // sldTempo
            // 
            this.sldTempo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldTempo.DecPlaces = 0;
            this.sldTempo.DrawColor = System.Drawing.Color.White;
            this.sldTempo.Label = "BPM";
            this.sldTempo.Location = new System.Drawing.Point(466, 113);
            this.sldTempo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.sldTempo.Maximum = 200D;
            this.sldTempo.Minimum = 50D;
            this.sldTempo.Name = "sldTempo";
            this.sldTempo.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldTempo.ResetValue = 50D;
            this.sldTempo.Size = new System.Drawing.Size(98, 53);
            this.sldTempo.TabIndex = 80;
            this.toolTip.SetToolTip(this.sldTempo, "Tempo adjuster");
            this.sldTempo.Value = 100D;
            this.sldTempo.ValueChanged += new System.EventHandler(this.Tempo_ValueChanged);
            // 
            // btnKill
            // 
            this.btnKill.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_242_flash;
            this.btnKill.Location = new System.Drawing.Point(508, 299);
            this.btnKill.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnKill.Name = "btnKill";
            this.btnKill.Size = new System.Drawing.Size(32, 40);
            this.btnKill.TabIndex = 89;
            this.toolTip.SetToolTip(this.btnKill, "Kill all midi channels");
            this.btnKill.UseVisualStyleBackColor = true;
            this.btnKill.Click += new System.EventHandler(this.Kill_Click);
            // 
            // lbPatterns
            // 
            this.lbPatterns.BackColor = System.Drawing.SystemColors.Control;
            this.lbPatterns.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbPatterns.FormattingEnabled = true;
            this.lbPatterns.ItemHeight = 20;
            this.lbPatterns.Location = new System.Drawing.Point(584, 113);
            this.lbPatterns.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lbPatterns.Name = "lbPatterns";
            this.lbPatterns.Size = new System.Drawing.Size(131, 222);
            this.lbPatterns.TabIndex = 88;
            this.toolTip.SetToolTip(this.lbPatterns, "All patterns in style file");
            this.lbPatterns.SelectedIndexChanged += new System.EventHandler(this.Patterns_SelectedIndexChanged);
            // 
            // chkDrumsOn1
            // 
            this.chkDrumsOn1.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkDrumsOn1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkDrumsOn1.Location = new System.Drawing.Point(466, 179);
            this.chkDrumsOn1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkDrumsOn1.Name = "chkDrumsOn1";
            this.chkDrumsOn1.Size = new System.Drawing.Size(98, 34);
            this.chkDrumsOn1.TabIndex = 87;
            this.chkDrumsOn1.Text = "Drums on 1";
            this.toolTip.SetToolTip(this.chkDrumsOn1, "Drums are on channel 1");
            this.chkDrumsOn1.UseVisualStyleBackColor = true;
            this.chkDrumsOn1.CheckedChanged += new System.EventHandler(this.DrumsOn1_CheckedChanged);
            // 
            // chkLogMidi
            // 
            this.chkLogMidi.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkLogMidi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkLogMidi.Image = global::MidiStyleExplorer.Properties.Resources.glyphicons_170_record;
            this.chkLogMidi.Location = new System.Drawing.Point(466, 299);
            this.chkLogMidi.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkLogMidi.Name = "chkLogMidi";
            this.chkLogMidi.Size = new System.Drawing.Size(32, 40);
            this.chkLogMidi.TabIndex = 86;
            this.toolTip.SetToolTip(this.chkLogMidi, "Enable logging midi events");
            this.chkLogMidi.UseVisualStyleBackColor = true;
            // 
            // btnPatch
            // 
            this.btnPatch.Location = new System.Drawing.Point(465, 223);
            this.btnPatch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnPatch.Name = "btnPatch";
            this.btnPatch.Size = new System.Drawing.Size(65, 29);
            this.btnPatch.TabIndex = 85;
            this.btnPatch.Text = "Patch->";
            this.toolTip.SetToolTip(this.btnPatch, "Send the patch to channel");
            this.btnPatch.UseVisualStyleBackColor = true;
            this.btnPatch.Click += new System.EventHandler(this.Patch_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1358, 656);
            this.Controls.Add(this.cmbPatchList);
            this.Controls.Add(this.txtPatchChannel);
            this.Controls.Add(this.barBar);
            this.Controls.Add(this.cgChannels);
            this.Controls.Add(this.sldTempo);
            this.Controls.Add(this.btnKill);
            this.Controls.Add(this.lbPatterns);
            this.Controls.Add(this.chkDrumsOn1);
            this.Controls.Add(this.chkLogMidi);
            this.Controls.Add(this.btnPatch);
            this.Controls.Add(this.txtViewer);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.chkPlay);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.btnRewind);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "Clip Explorer";
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
        private System.Windows.Forms.ComboBox cmbPatchList;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.TextBox txtPatchChannel;
        private NBagOfUis.BarBar barBar;
        private NBagOfUis.ClickGrid cgChannels;
        private NBagOfUis.Slider sldTempo;
        private System.Windows.Forms.Button btnKill;
        private System.Windows.Forms.ListBox lbPatterns;
        private System.Windows.Forms.CheckBox chkDrumsOn1;
        private System.Windows.Forms.CheckBox chkLogMidi;
        private System.Windows.Forms.Button btnPatch;
        private System.Windows.Forms.ToolStripButton btnAutoplay;
        private System.Windows.Forms.ToolStripButton btnLoop;
    }
}

