using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace EyeTrackingHooks
{
	// Generic interface to wrap any specific OCR library
	public interface IOcrEngine
	{
		IOcrResult Recognize(Bitmap bitmap);
	}

	public interface IOcrResult
	{
		void Dispose();
		bool FindWord(string searchWord, out Point position);
		string GetDebugText();
	}
}
