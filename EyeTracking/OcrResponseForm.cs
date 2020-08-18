using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

namespace EyeTrackingHooks
{
	public partial class OcrResponseForm : Form
	{
		Stopwatch watch = new Stopwatch();

		public OcrResponseForm()
		{
			InitializeComponent();
		}

		private void opacityTimer_Tick(object sender, EventArgs e)
		{
			double opacity = Math.Min(1.0, 1.0 - ((double)watch.ElapsedMilliseconds - 2000.0) / 1000.0);
			if (opacity < 0.0)
			{
				this.Hide();
				opacityTimer.Stop();
			}
			else
			{
				this.Opacity = opacity;
			}
		}

		public void Show(string message)
		{
			lblResponse.Text = message;

			Font originalFont = lblResponse.Font;
			using (Graphics g = lblResponse.CreateGraphics())
			{
				for (int fontSize = 100; fontSize >= 10; fontSize -= 5)
				{
					Font font = new Font(originalFont.Name, fontSize, originalFont.Style);
					if ((int)g.MeasureString(message, font).Width <= lblResponse.Width)
					{
						lblResponse.Font = font;
						break;
					}
				}
			}

			watch.Restart();
			opacityTimer.Start();

			this.Opacity = 1;
			this.Hide();
			this.Show();
		}
		private void OcrResponseForm_Shown(object sender, EventArgs e)
		{
			
		}
	}
}
