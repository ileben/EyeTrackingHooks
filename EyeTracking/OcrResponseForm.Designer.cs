namespace EyeTrackingHooks
{
	partial class OcrResponseForm
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
			this.components = new System.ComponentModel.Container();
			this.lblResponse = new System.Windows.Forms.Label();
			this.opacityTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// lblResponse
			// 
			this.lblResponse.Font = new System.Drawing.Font("Microsoft Sans Serif", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblResponse.ForeColor = System.Drawing.Color.Red;
			this.lblResponse.Location = new System.Drawing.Point(12, 9);
			this.lblResponse.Name = "lblResponse";
			this.lblResponse.Size = new System.Drawing.Size(386, 155);
			this.lblResponse.TabIndex = 0;
			this.lblResponse.Text = "Something?";
			this.lblResponse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// opacityTimer
			// 
			this.opacityTimer.Enabled = true;
			this.opacityTimer.Interval = 10;
			this.opacityTimer.Tick += new System.EventHandler(this.opacityTimer_Tick);
			// 
			// OcrResponseForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(410, 173);
			this.Controls.Add(this.lblResponse);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "OcrResponseForm";
			this.Text = "OcrResponseForm";
			this.TopMost = true;
			this.Shown += new System.EventHandler(this.OcrResponseForm_Shown);
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.Label lblResponse;
		private System.Windows.Forms.Timer opacityTimer;
	}
}