namespace EyeTrackingHooks
{
	partial class OcrDebugForm
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
			this.ocrPicture = new System.Windows.Forms.PictureBox();
			this.ocrText = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.ocrPicture)).BeginInit();
			this.SuspendLayout();
			// 
			// ocrPicture
			// 
			this.ocrPicture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ocrPicture.Location = new System.Drawing.Point(0, -4);
			this.ocrPicture.Name = "ocrPicture";
			this.ocrPicture.Size = new System.Drawing.Size(281, 303);
			this.ocrPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.ocrPicture.TabIndex = 0;
			this.ocrPicture.TabStop = false;
			// 
			// ocrText
			// 
			this.ocrText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ocrText.Location = new System.Drawing.Point(0, 305);
			this.ocrText.Multiline = true;
			this.ocrText.Name = "ocrText";
			this.ocrText.Size = new System.Drawing.Size(281, 198);
			this.ocrText.TabIndex = 1;
			// 
			// OcrDebugForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 505);
			this.Controls.Add(this.ocrText);
			this.Controls.Add(this.ocrPicture);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "OcrDebugForm";
			this.Text = "OcrDebugForm";
			this.TopMost = true;
			((System.ComponentModel.ISupportInitialize)(this.ocrPicture)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.PictureBox ocrPicture;
		public System.Windows.Forms.TextBox ocrText;
	}
}