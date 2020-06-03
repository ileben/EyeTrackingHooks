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

	public class OcrResultIterator
	{
		OcrResult result;
		int lineIndex = 0;
		int wordIndex = 0;
		int charIndex = 0;

		public OcrResultIterator(OcrResult r)
		{
			result = r;
		}

		public OcrResultIterator(OcrResultIterator i)
		{
			result = i.result;
			lineIndex = i.lineIndex;
			wordIndex = i.wordIndex;
			charIndex = i.charIndex;
		}

		public bool Done()
		{
			return lineIndex >= result.Lines.Count;
		}

		public OcrWord NextWord()
		{
			OcrWord word = result.Lines[lineIndex].Words[wordIndex];
			wordIndex++;
			charIndex = 0;

			// I guess there could be lines with zero words?
			while (lineIndex < result.Lines.Count &&
				   wordIndex >= result.Lines[lineIndex].Words.Count)
			{
				lineIndex++;
				wordIndex = 0;
			}

			return word;
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

		string Sanitize(string word)
		{
			return word.Replace(" ", "").Replace("\t", "").ToUpper();
		}

		public bool FindWord(string searchWord, out Point position)
		{
			bool result = false;
			float minDistance = float.MaxValue;
			Point center = new Point(bitmap.Width / 2, bitmap.Height / 2);
			position = new Point(0, 0);

			debugText += ocrResult.Text;

			// We remove whitespace in both the search word and the recognition result 
			// to allow searching for longer phrases
			searchWord = Sanitize(searchWord);
			if (searchWord.Length > 0)
			{
				OcrResultIterator i = new OcrResultIterator(ocrResult);
				//foreach (OcrLine ocrLine in ocrResult.Lines)
				{
					//foreach (OcrWord ocrWord in ocrLine.Words)
					while (!i.Done())
					{
						//string word = ocrWord.Text;

						// Sometimes the engine splits words when it shouldn't
						// Append following words until we have enough text to possibly fit the search word
						OcrWord ocrWord = i.NextWord();
						string word = Sanitize(ocrWord.Text);
						OcrResultIterator j = new OcrResultIterator(i);
						while (word.Length < searchWord.Length && !j.Done())
						{
							ocrWord = j.NextWord();
							word += Sanitize(ocrWord.Text);
						}

						debugText += "\r\nWord: '" + word + "'";
						if (word.Contains(searchWord))
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
			}

			return result;
		}
		public string GetDebugText()
		{
			return debugText;
		}
	}
}
