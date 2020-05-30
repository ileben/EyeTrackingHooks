using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

// Microsoft.Windows.SDK.Contracts NuGet package to add UWP interfaces to .net desktop applications
using Windows.Media.Ocr;

namespace EyeTrackingHooks
{
	public class OcrEngineWindows : IOcrEngine
	{
		OcrEngine engine = null;

		public OcrEngineWindows()
		{
			engine = Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
		}

		async Task<OcrResult> RecognizeBitmapAsync(Bitmap b)
		{
			// Need to marshall from Drawing.Bitmap to UWP SoftwareBitmap
			using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
			{
				b.Save(stream.AsStream(), System.Drawing.Imaging.ImageFormat.Bmp);//choose the specific image format by your own bitmap source
				Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
				Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
				return await engine.RecognizeAsync(softwareBitmap);
			}
		}

		public IOcrResult Recognize(Bitmap bitmap)
		{
			OcrResultWindows result = new OcrResultWindows();

			Task<OcrResult> task = Task.Run(async () => await RecognizeBitmapAsync(bitmap));
			task.Wait();

			result.ocrResult = task.Result;
			result.bitmap = bitmap;

			return result;
		}
	}

	public class OcrResultWindows : IOcrResult
	{
		public string debugText = "";
		public OcrResult ocrResult = null;
		public Bitmap bitmap = null;

		public void Dispose()
		{
		}

		public bool FindWord(string searchWord, out Point position)
		{
			bool result = false;
			float minDistance = float.MaxValue;
			Point center = new Point(bitmap.Width / 2, bitmap.Height / 2);
			position = new Point(0, 0);

			debugText += ocrResult.Text;

			foreach (OcrLine ocrLine in ocrResult.Lines)
			{
				foreach (OcrWord ocrWord in ocrLine.Words)
				{
					string word = ocrWord.Text;
					debugText += "\r\nWord: '" + word + "'";
					if (searchWord.Length > 0 && word.ToUpper().Contains(searchWord.ToUpper()))
					{
						debugText += "\r\n*** Word match!";
						Windows.Foundation.Rect rect = ocrWord.BoundingRect;
						Point wordCenter = new Point((int)(rect.Left + rect.Right) / 2, (int)(rect.Top + rect.Bottom) / 2);
						int dx = wordCenter.X - center.X;
						int dy = wordCenter.Y - center.Y;
						float distance = (float)Math.Sqrt(dx * dx + dy * dy);
						if (distance <= minDistance)
						{
							debugText += "\r\n*** Closest!";
							position = wordCenter;
							result = true;
							minDistance = distance;
						}
					}
				}
			}

			return result;
		}
		public string GetDebugText()
		{
			return debugText;
		}
	}
}
