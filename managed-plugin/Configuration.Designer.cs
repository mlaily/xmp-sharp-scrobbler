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
            this.btnReAuth = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtStatus = new System.Windows.Forms.Label();
            this.btnGetSessionKey = new System.Windows.Forms.Button();
            this.openLogLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // btnReAuth
            // 
            this.btnReAuth.Location = new System.Drawing.Point(12, 12);
            this.btnReAuth.Name = "btnReAuth";
            this.btnReAuth.Size = new System.Drawing.Size(149, 23);
            this.btnReAuth.TabIndex = 0;
            this.btnReAuth.Text = "Authenticate with Last.fm...";
            this.btnReAuth.UseVisualStyleBackColor = true;
            this.btnReAuth.Click += new System.EventHandler(this.BtnReAuth_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(159, 134);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 26);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(240, 134);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 26);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // txtStatus
            // 
            this.txtStatus.AutoSize = true;
            this.txtStatus.Location = new System.Drawing.Point(12, 46);
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(16, 13);
            this.txtStatus.TabIndex = 3;
            this.txtStatus.Text = "...";
            // 
            // btnGetSessionKey
            // 
            this.btnGetSessionKey.Enabled = false;
            this.btnGetSessionKey.Location = new System.Drawing.Point(167, 12);
            this.btnGetSessionKey.Name = "btnGetSessionKey";
            this.btnGetSessionKey.Size = new System.Drawing.Size(149, 23);
            this.btnGetSessionKey.TabIndex = 4;
            this.btnGetSessionKey.Text = "Complete authentication";
            this.btnGetSessionKey.UseVisualStyleBackColor = true;
            // 
            // openLogLink
            // 
            this.openLogLink.AutoSize = true;
            this.openLogLink.LinkColor = System.Drawing.Color.RoyalBlue;
            this.openLogLink.Location = new System.Drawing.Point(12, 141);
            this.openLogLink.Name = "openLogLink";
            this.openLogLink.Size = new System.Drawing.Size(66, 13);
            this.openLogLink.TabIndex = 5;
            this.openLogLink.TabStop = true;
            this.openLogLink.Text = "Open log file";
            this.openLogLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenLogLink_LinkClicked);
            // 
            // Configuration
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(327, 172);
            this.Controls.Add(this.openLogLink);
            this.Controls.Add(this.btnGetSessionKey);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnReAuth);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Configuration";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sharp Scrobbler configuration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnReAuth;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label txtStatus;
        private System.Windows.Forms.Button btnGetSessionKey;
        private System.Windows.Forms.LinkLabel openLogLink;
    }
}