namespace XmpSharpScrobbler
{
    partial class Configuration
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.BtnReAuth = new System.Windows.Forms.Button();
            this.BtnSave = new System.Windows.Forms.Button();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.TxtStatus = new System.Windows.Forms.Label();
            this.BtnGetSessionKey = new System.Windows.Forms.Button();
            this.OpenLogLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // BtnReAuth
            // 
            this.BtnReAuth.Location = new System.Drawing.Point(12, 12);
            this.BtnReAuth.Name = "BtnReAuth";
            this.BtnReAuth.Size = new System.Drawing.Size(149, 23);
            this.BtnReAuth.TabIndex = 0;
            this.BtnReAuth.Text = "Authenticate with Last.fm...";
            this.BtnReAuth.UseVisualStyleBackColor = true;
            this.BtnReAuth.Click += new System.EventHandler(this.BtnReAuth_Click);
            // 
            // BtnSave
            // 
            this.BtnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnSave.Location = new System.Drawing.Point(159, 134);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(75, 26);
            this.BtnSave.TabIndex = 1;
            this.BtnSave.Text = "Save";
            this.BtnSave.UseVisualStyleBackColor = true;
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // BtnCancel
            // 
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(240, 134);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(75, 26);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // TxtStatus
            // 
            this.TxtStatus.AutoSize = true;
            this.TxtStatus.Location = new System.Drawing.Point(12, 46);
            this.TxtStatus.Name = "TxtStatus";
            this.TxtStatus.Size = new System.Drawing.Size(16, 13);
            this.TxtStatus.TabIndex = 3;
            this.TxtStatus.Text = "...";
            // 
            // BtnGetSessionKey
            // 
            this.BtnGetSessionKey.Enabled = false;
            this.BtnGetSessionKey.Location = new System.Drawing.Point(167, 12);
            this.BtnGetSessionKey.Name = "BtnGetSessionKey";
            this.BtnGetSessionKey.Size = new System.Drawing.Size(149, 23);
            this.BtnGetSessionKey.TabIndex = 4;
            this.BtnGetSessionKey.Text = "Complete authentication";
            this.BtnGetSessionKey.UseVisualStyleBackColor = true;
            // 
            // OpenLogLink
            // 
            this.OpenLogLink.AutoSize = true;
            this.OpenLogLink.LinkColor = System.Drawing.Color.RoyalBlue;
            this.OpenLogLink.Location = new System.Drawing.Point(12, 141);
            this.OpenLogLink.Name = "OpenLogLink";
            this.OpenLogLink.Size = new System.Drawing.Size(66, 13);
            this.OpenLogLink.TabIndex = 5;
            this.OpenLogLink.TabStop = true;
            this.OpenLogLink.Text = "Open log file";
            this.OpenLogLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenLogLink_LinkClicked);
            // 
            // ConfigurationForm
            // 
            this.AcceptButton = this.BtnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(327, 172);
            this.Controls.Add(this.OpenLogLink);
            this.Controls.Add(this.BtnGetSessionKey);
            this.Controls.Add(this.TxtStatus);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.BtnReAuth);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sharp Scrobbler configuration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnReAuth;
        private System.Windows.Forms.Button BtnSave;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Label TxtStatus;
        private System.Windows.Forms.Button BtnGetSessionKey;
        private System.Windows.Forms.LinkLabel OpenLogLink;
    }
}