
namespace MidiStyleExplorer
{
    partial class ChannelControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.chkSolo = new System.Windows.Forms.CheckBox();
            this.chkMute = new System.Windows.Forms.CheckBox();
            this.lblNumber = new System.Windows.Forms.Label();
            this.sldVolume = new NBagOfUis.Slider();
            this.cmbPatch = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // chkSolo
            // 
            this.chkSolo.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkSolo.AutoSize = true;
            this.chkSolo.Location = new System.Drawing.Point(245, 2);
            this.chkSolo.Name = "chkSolo";
            this.chkSolo.Size = new System.Drawing.Size(27, 30);
            this.chkSolo.TabIndex = 1;
            this.chkSolo.Text = "S";
            this.chkSolo.UseVisualStyleBackColor = true;
            // 
            // chkMute
            // 
            this.chkMute.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkMute.AutoSize = true;
            this.chkMute.Location = new System.Drawing.Point(278, 2);
            this.chkMute.Name = "chkMute";
            this.chkMute.Size = new System.Drawing.Size(32, 30);
            this.chkMute.TabIndex = 2;
            this.chkMute.Text = "M";
            this.chkMute.UseVisualStyleBackColor = true;
            // 
            // lblNumber
            // 
            this.lblNumber.AutoSize = true;
            this.lblNumber.Location = new System.Drawing.Point(2, 8);
            this.lblNumber.Name = "lblNumber";
            this.lblNumber.Size = new System.Drawing.Size(18, 20);
            this.lblNumber.TabIndex = 3;
            this.lblNumber.Text = "#";
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DecPlaces = 1;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(158, 2);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.ResetValue = 0D;
            this.sldVolume.Size = new System.Drawing.Size(80, 30);
            this.sldVolume.TabIndex = 43;
            this.sldVolume.Value = 0.8D;
            // 
            // cmbPatch
            // 
            this.cmbPatch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatch.FormattingEnabled = true;
            this.cmbPatch.Location = new System.Drawing.Point(36, 3);
            this.cmbPatch.Name = "cmbPatch";
            this.cmbPatch.Size = new System.Drawing.Size(104, 28);
            this.cmbPatch.TabIndex = 44;
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbPatch);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.lblNumber);
            this.Controls.Add(this.chkMute);
            this.Controls.Add(this.chkSolo);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(312, 38);
            this.Load += new System.EventHandler(this.ChannelControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox chkSolo;
        private System.Windows.Forms.CheckBox chkMute;
        private System.Windows.Forms.Label lblNumber;
        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.ComboBox cmbPatch;
    }
}
