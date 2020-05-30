using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Drawing;

// Tesseract NuGet package 
// https://github.com/charlesw/tesseract/
using Tesseract;

namespace EyeTrackingHooks
{
	public class OcrEngineTesseract : IOcrEngine
	{
		TesseractEngine tesseract = null;

		public OcrEngineTesseract()
		{
			// tessdata/eng.traineddata downloaded from https://github.com/tesseract-ocr/tessdata
			string modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string dataPath = Path.Combine(modulePath, "tessdata");
			tesseract = new TesseractEngine(dataPath, "eng", EngineMode.LstmOnly);
		}

		public IOcrResult Recognize(Bitmap bitmap)
		{
			var result = new OcrResultTesseract();
			result.page = tesseract.Process(bitmap);
			result.bitmap = bitmap;
			return result;
		}
	}

	public class OcrResultTesseract : IOcrResult
	{
		public Tesseract.Page page = null;
		public string debugText = "";
		public Bitmap bitmap = null;

		public void Dispose()
		{
			page.Dispose();
		}

		public bool FindWord(string searchWord, out Point position)
		{
			bool result = false;
			float minDistance = float.MaxValue;
			Point center = new Point(bitmap.Width / 2, bitmap.Height / 2);
			position = new Point(0, 0);

			debugText += page.GetText();

			var iterator = page.GetIterator();
			iterator.Begin();
			do
			{
				string word = iterator.GetText(PageIteratorLevel.Word);
				if (word != null)
				{
					debugText += "\r\nWord: '" + word + "'";
					if (searchWord.Length > 0 && word.ToUpper().Contains(searchWord.ToUpper()))
					{
						debugText += "\r\n*** Word match!";
						Rect rect;
						if (iterator.TryGetBoundingBox(PageIteratorLevel.Word, out rect))
						{
							Point wordCenter = new Point((rect.X1 + rect.X2) / 2, (rect.Y1 + rect.Y2) / 2);
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
						else
						{
							debugText += "\r\n*** Error: Failed to get bounding box!";
						}
					}
				}
			} while (iterator.Next(PageIteratorLevel.Word));

			return result;
		}
		public string GetDebugText()
		{
			return debugText;
		}
	}
}
