using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;

using Tobii.Interaction;
using Tobii.Interaction.Framework;

namespace EyeTrackingHooks
{
	public class EyeTracking
    {
		enum State
		{
			None,
			Strafing,
			Orbiting
		}

		static Host host = null;

		static int gazeX = 0;
		static int gazeY = 0;
		static int smoothX = 0;
		static int smoothY = 0;

		static bool followGaze = false;
		static List<Point> gazeList = new List<Point>();
		static Form zoomForm = null;

		static Bitmap zoomBackground = null;
		static Bitmap zoomBitmap = null; // Includes the cursor indicator over the background
		static Object bitmapLock = new object();

		static ZoomPictureBox zoomPicture = null;
		static ZoomBounds zoomBounds = new ZoomBounds(0, 0);

		static State state = State.None;

		// Zoom source and destination rectangles
		class ZoomBounds
		{
			public float zoomFactor;

			public int screenW;
			public int screenH;

			public int zoomW;
			public int zoomH;
			public int zoomX;
			public int zoomY;

			public int bigW;
			public int bigH;
			public int bigX;
			public int bigY;

			public ZoomBounds(int x, int y)
			{
				System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
				screenW = screenBounds.Width;
				screenH = screenBounds.Height;

				zoomFactor = 6;

				zoomW = (int)(screenBounds.Width / zoomFactor);
				zoomH = (int)(screenBounds.Height / zoomFactor);

				zoomX = x - zoomW / 2;
				zoomY = y - zoomH / 2;

				bigW = (int)(zoomW * zoomFactor);
				bigH = (int)(zoomH * zoomFactor);

				bigX = (screenBounds.Right + screenBounds.Left) / 2 - bigW / 2;
				bigY = (screenBounds.Bottom + screenBounds.Top) / 2 - bigH / 2;
			}
		}

		class ZoomPictureBox : PictureBox
		{
			protected override void OnPaint(PaintEventArgs e)
			{
				// Just in case Zoom() happens on another thread
				lock (bitmapLock)
				{
					//UpdateZoomBackground();
					UpdateZoomBitmap();
					base.OnPaint(e);
				}
			}
		}

		public static void OnGaze(double x, double y, double ts)
		{
			gazeX = (int)x; gazeY = (int)y;

			// Keep X latest points for smoothing
			gazeList.Add(new Point(gazeX, gazeY));
			while (gazeList.Count > 50)
			{
				gazeList.RemoveAt(0);
			}

			if (followGaze)
			{
				// Filtering to smooth-out gaze position
				float endX = (float)gazeList.Last().X;
				float endY = (float)gazeList.Last().Y;

				float avgX = endX;
				float avgY = endY;
				float count = 1;

				for (int p = gazeList.Count-2; p >= 0; --p)
				{
					float px = (float)gazeList[p].X;
					float py = (float)gazeList[p].Y;
					
					if (px - endX <= 100.0f &&
						py - endY <= 100.0f)
					{
						avgX += (float)gazeList[p].X;
						avgY += (float)gazeList[p].Y;
						count += 1;
					}
				}
				avgX /= count;
				avgY /= count;

				smoothX = (int)avgX;
				smoothY = (int)avgY;

				if (zoomForm != null &&
					zoomForm.Visible)
				{
					// If currently zooming, use translated zoom position
					System.Windows.Forms.Cursor.Position = GetZoomGaze();
					zoomPicture.Invalidate();
				}
				else
				{
					// Otherwise use filtered position directly
					System.Windows.Forms.Cursor.Position = new Point(smoothX, smoothY);
				}
			}

			ProcessGaze();
		}

		public static void UpdateZoomBackground()
		{
			ZoomBounds z = zoomBounds;

			Graphics g = Graphics.FromImage(zoomBackground);
			//g.CopyFromScreen(z.zoomX, z.zoomY, 0, 0,
				//new System.Drawing.Size(z.zoomW, z.zoomH),
				//CopyPixelOperation.SourceCopy);
			g.CopyFromScreen(0, 0, 0, 0,
				new System.Drawing.Size(z.screenW, z.screenH),
				CopyPixelOperation.SourceCopy);
			g.Dispose();
		}

		public static void UpdateZoomBitmap()
		{
			ZoomBounds z = zoomBounds;

			Graphics g = Graphics.FromImage(zoomBitmap);
			g.Clear(Color.Gray);
			//g.DrawImage(zoomBackground, 0, 0);
			g.DrawImage(zoomBackground, 0, 0, new System.Drawing.Rectangle(z.zoomX, z.zoomY, z.zoomW, z.zoomH), GraphicsUnit.Pixel);

			float bX = (smoothX - z.bigX) / z.zoomFactor;
			float bY = (smoothY - z.bigY) / z.zoomFactor;
			g.DrawLine(Pens.Black, bX - 10, bY, bX + 10, bY);
			g.DrawLine(Pens.Black, bX, bY - 10, bX, bY + 10);
			g.Dispose();
		}

		public static void OnZoomFormClosed(Object sender, FormClosedEventArgs e)
		{
			zoomForm = null;
		}

		public static void Zoom()
		{
			ZoomBounds z = new ZoomBounds(gazeX, gazeY);
			zoomBounds = z;
			
			if (zoomForm == null)
			{
				zoomForm = new Form();
				zoomForm.Text = "Zoom";
				zoomForm.Width = z.bigW;
				zoomForm.Height = z.bigH;
				zoomForm.FormBorderStyle = FormBorderStyle.None;
				zoomForm.TopMost = true;
				//zoomForm.Opacity = 0.7f;
				zoomForm.FormClosed += OnZoomFormClosed;

				zoomPicture = new ZoomPictureBox();
				zoomPicture.Parent = zoomForm;
				zoomPicture.SizeMode = PictureBoxSizeMode.StretchImage;
				zoomPicture.Width = z.bigW;
				zoomPicture.Height = z.bigH;
			}

			zoomForm.Hide();

			lock (bitmapLock)
			{
				zoomBitmap = new Bitmap(z.zoomW, z.zoomH);
				//zoomBackground = new Bitmap(z.zoomW, z.zoomH);
				zoomBackground = new Bitmap(z.screenW, z.screenH);

				UpdateZoomBackground();
				UpdateZoomBitmap();
			}

			zoomPicture.Image = zoomBitmap;
			zoomForm.Show();
			zoomForm.Left = z.bigX;
			zoomForm.Top = z.bigY;

			followGaze = true;
		}

		static public void Unzoom()
		{
			if (zoomForm != null)
			{
				zoomForm.Hide();
			}
		}

		static public Point GetZoomGaze()
		{
			ZoomBounds z = zoomBounds;
			float screenX = z.zoomX + (smoothX - z.bigX) / z.zoomFactor;
			float screenY = z.zoomY + (smoothY - z.bigY) / z.zoomFactor;
			return new Point((int)screenX, (int)screenY);
		}

		static public void ZoomPush()
		{
			if (zoomForm == null)
				return;

			followGaze = false;

			System.Windows.Forms.Cursor.Position = GetZoomGaze();

			zoomForm.Hide();
		}

		public static void Connect()
		{
			if (host != null)
				return;

			// Everything starts with initializing Host, which manages connection to the 
			// Tobii Engine and provides all the Tobii Core SDK functionality.
			// NOTE: Make sure that Tobii.EyeX.exe is running
			host = new Host();

			// 2. Create stream. 
			var gazePointDataStream = host.Streams.CreateGazePointDataStream();

			// 3. Get the gaze data!
			//gazePointDataStream.GazePoint((x, y, ts) => Console.WriteLine("Timestamp: {0}\t X: {1} Y:{2}", ts, x, y));
			//gazePointDataStream.GazePoint((x, y, ts) => { gazeX = (int)x; gazeY = (int)y; });
			gazePointDataStream.GazePoint(OnGaze);

			// okay, it is 4 lines, but you won't be able to see much without this one :)
			//Console.ReadKey();

			// TODO
			//GetStatus();
		}

		public static async Task GetStatus()
		{
			// TODO: Trying to disable tracking programmatically
			var x = await host.States.GetEyeTrackingDeviceStatusAsync();
			string s = x.ToString();
			await host.States.SetStateValueAsync("DeviceStatus", EyeTrackingDeviceStatus.TrackingPaused);
		}

		public static void Disconnect()
		{
			if (host == null)
				return;

			// we will close the coonection to the Tobii Engine before exit.
			host.DisableConnection();
		}

		public static void EnableFollowGaze()
		{
			followGaze = true;
		}

		public static void DisableFollowGaze()
		{
			followGaze = false;

			Unzoom();
		}

		public static void TeleportCursor()
		{
			System.Windows.Forms.Cursor.Position = new Point(gazeX, gazeY);
		}

		public static int GetX()
		{
			return gazeX;
		}

		public static int GetY()
		{
			return gazeY;
		}

		public static int Test()
		{
			return 42;
		}

		public static void Strafe()
		{
			ReleaseAllKeys();
			state = State.Strafing;
		}

		public static void Orbit()
		{
			ReleaseAllKeys();
			state = State.Orbiting;
		}

		public static void StopMoving()
		{
			state = State.None;
			ReleaseAllKeys();
		}

		private static List<int> pressedKeys = new List<int>();

		private static void PressKey(int key)
		{
			Win32Input.PressKey(key);
			if (!pressedKeys.Contains(key))
			{
				pressedKeys.Add(key);
			}
		}

		private static void ReleaseKey(int key)
		{
			if (pressedKeys.Contains(key))
			{
				Win32Input.ReleaseKey(key);
				pressedKeys.Remove(key);
			}
		}

		private static void ReleaseAllKeys()
		{
			foreach (int key in pressedKeys)
			{
				ReleaseKey(key);
			}
		}

		private static int GetGazeZoneX(int zoneCount)
		{
			System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
			int zoneSize = screenBounds.Width / zoneCount;
			return (gazeX - screenBounds.Left) / zoneSize;
		}

		private static int GetGazeZoneY(int zoneCount)
		{
			System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
			int zoneSize = screenBounds.Height / zoneCount;
			return (gazeY - screenBounds.Top) / zoneSize;
		}

		private static Point GetGazeZone(int zoneCountX, int zoneCountY)
		{
			return new Point(GetGazeZoneX(zoneCountX), GetGazeZoneY(zoneCountY));
		}

		private static Point ScreenCenter()
		{
			System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
			return new Point(
				(screenBounds.Left + screenBounds.Right) / 2,
				(screenBounds.Top + screenBounds.Bottom) / 2);
		}

		private static Point MovePoint(Point p, int x, int y)
		{
			return new Point(p.X + x, p.Y + y);
		}

		public static void ProcessGaze()
		{
			if (zoomForm != null &&
				zoomForm.Visible)
			{
				Point gazeZone = GetGazeZone(4, 4);
				int k = 1;

				if (gazeZone.X == 3)
				{
					zoomBounds.zoomX += k;
				}
				if (gazeZone.X == 0)
				{
					zoomBounds.zoomX -= k;
				}
				if (gazeZone.Y == 3)
				{
					zoomBounds.zoomY += k;
				}
				if (gazeZone.Y == 0)
				{
					zoomBounds.zoomY -= k;
				}
			}
			if (state == State.Strafing)
			{
				Point gazeZone = GetGazeZone(3, 3);

				if (gazeZone.X == 2)
					PressKey('D');
				else
					ReleaseKey('D');

				if (gazeZone.X == 0)
					PressKey('A');
				else
					ReleaseKey('A');

				if (gazeZone.Y == 2)
					PressKey('S');
				else
					ReleaseKey('S');

				if (gazeZone.Y == 0)
					PressKey('W');
				else
					ReleaseKey('W');
			}
			else if (state == State.Orbiting)
			{
				Point gazeZone = GetGazeZone(3, 3);
				Point center = ScreenCenter();

				if (gazeZone.X == 2)
				{
					Mouse.Drag(MouseButton.Right, center, 5, 0);
				}
				if (gazeZone.X == 0)
				{
					Mouse.Drag(MouseButton.Right, center, -5, 0);
				}
				if (gazeZone.Y == 2)
				{
					Mouse.Drag(MouseButton.Right, center, 0, 5);
				}
				if (gazeZone.Y == 0)
				{
					Mouse.Drag(MouseButton.Right, center, 0, -5);
				}
			}
		}
	}
}
