using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Drawing;
using Size = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

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
		static Stopwatch zoomSteadyStopwatch = new Stopwatch();
		static Stopwatch zoomStopwatch = new Stopwatch();
		static List<Point> zoomGazeHistory = new List<Point>();
		static bool zoomHighlight = false;
		static Timer zoomUpdateTimer = new Timer(500);
		static int ZOOM_CURSOR_DELAY = 800;

		static State state = State.None;

		// Zoom source and destination rectangles
		class ZoomBounds
		{
			public float zoomFactor;

			public int screenW;
			public int screenH;

			public Rectangle source = new Rectangle();
			public Rectangle big = new Rectangle();

			public ZoomBounds(int x, int y)
			{
				Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
				screenW = screenBounds.Width;
				screenH = screenBounds.Height;

				zoomFactor = 6;

				// Try to find a rectangle on the side of the zoom point with the largest area, 
				// to maximize the size of the blown up picture without obscuring the zoom area.
				// We want a clearing around the zoom point of at least a quarter of the screen.
				int clearing = Math.Min(screenW, screenH) / 4;

				int leftEdge = x - clearing;
				int rightEdge = x + clearing;
				int topEdge = y - clearing;
				int bottomEdge = y + clearing;

				int leftSize = leftEdge - screenBounds.Left;
				int rightSize = screenBounds.Right - rightEdge;
				int topSize = topEdge - screenBounds.Top;
				int bottomSize = screenBounds.Bottom - bottomEdge;

				int leftArea = leftSize * screenBounds.Height;
				int rightArea = rightSize * screenBounds.Height;
				int topArea = topSize * screenBounds.Width;
				int bottomArea = bottomSize * screenBounds.Width;

				int largest = leftArea;
				big = new Rectangle(
					screenBounds.Left, screenBounds.Top,
					leftSize, screenBounds.Height);

				if (rightArea > largest)
				{
					largest = rightArea;
					big = new Rectangle(
						rightEdge, screenBounds.Top,
						rightSize, screenBounds.Height);
				}
				if (rightArea > largest)
				{
					largest = topArea;
					big = new Rectangle(
						screenBounds.Left, screenBounds.Top,
						screenBounds.Width, topSize);
				}
				if (rightArea > largest)
				{
					largest = bottomArea;
					big = new Rectangle(
						screenBounds.Left, bottomEdge,
						screenBounds.Width, bottomSize);
				}
				
				// Now construct the source rectangle of the same aspect ratio centered on the zoom point
				source.Width = (int)(big.Width / zoomFactor);
				source.Height = (int)(big.Height / zoomFactor);

				source.X = x - source.Width / 2;
				source.Y = y - source.Height / 2;
			}
		}

		class ZoomPictureBox : PictureBox
		{
			protected override void OnPaint(PaintEventArgs e)
			{
				// Just in case Zoom() happens on another thread
				lock (bitmapLock)
				{
					UpdateZoomBackground();
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
					
					if (Math.Abs(px - endX) <= 100.0f &&
						Math.Abs(py - endY) <= 100.0f)
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
					// When they start zooming, wait for a bit before moving the cursor
					// so the eyes can settle on the blown up picture
					if (zoomStopwatch.ElapsedMilliseconds > ZOOM_CURSOR_DELAY)
					{
						zoomStopwatch.Stop();

						// If currently zooming, use translated zoom position
						System.Windows.Forms.Cursor.Position = GetZoomGaze();
						zoomPicture.Invalidate();
					}
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
			g.DrawImage(zoomBackground, 0, 0, z.source, GraphicsUnit.Pixel);

			float bX = (smoothX - z.big.X) / z.zoomFactor;
			float bY = (smoothY - z.big.Y) / z.zoomFactor;
			Pen crossPen = zoomHighlight ? Pens.Red: Pens.Black;
			g.DrawLine(crossPen, bX - 10, bY, bX + 10, bY);
			g.DrawLine(crossPen, bX, bY - 10, bX, bY + 10);
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
				zoomForm.FormBorderStyle = FormBorderStyle.None;
				zoomForm.TopMost = true;
				//zoomForm.Opacity = 0.7f;
				//zoomForm.TransparencyKey = Color.Red;
				zoomForm.FormClosed += OnZoomFormClosed;

				zoomPicture = new ZoomPictureBox();
				zoomPicture.Parent = zoomForm;
				zoomPicture.SizeMode = PictureBoxSizeMode.StretchImage;
			}

			zoomForm.Width = z.big.Width;
			zoomForm.Height = z.big.Height;
			zoomPicture.Width = z.big.Width;
			zoomPicture.Height = z.big.Height;

			zoomForm.Hide();

			lock (bitmapLock)
			{
				if (zoomBitmap != null)
				{
					zoomBitmap.Dispose();
					zoomBackground.Dispose();
				}

				zoomBitmap = new Bitmap(z.source.Width, z.source.Height);
				zoomBackground = new Bitmap(z.screenW, z.screenH);

				UpdateZoomBackground();
				UpdateZoomBitmap();
			}

			zoomPicture.Image = zoomBitmap;
			zoomForm.Show();
			zoomForm.Left = z.big.X;
			zoomForm.Top = z.big.Y;
			zoomStopwatch.Restart();

			//zoomUpdateTimer.Elapsed += OnZoomUpdate;
			//zoomUpdateTimer.AutoReset = true;
			//zoomUpdateTimer.Enabled = true;

			followGaze = true;
		}

		static public void OnZoomUpdate(Object source, ElapsedEventArgs e)
		{
			if (zoomForm != null &&
				zoomForm.Visible)
			{
				zoomForm.Opacity = 0;
				UpdateZoomBackground();
				zoomForm.Opacity = 1;
			}
		}

		static public void Unzoom()
		{
			if (zoomForm != null)
			{
				zoomForm.Hide();
				zoomStopwatch.Reset();
				zoomSteadyStopwatch.Reset();
				zoomHighlight = false;
				zoomUpdateTimer.Enabled = false;
			}
		}

		static public Point GetZoomGaze()
		{
			ZoomBounds z = zoomBounds;
			float screenX = z.source.X + (smoothX - z.big.X) / z.zoomFactor;
			float screenY = z.source.Y + (smoothY - z.big.Y) / z.zoomFactor;
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

		private static GazeZone GetGazeZone(int zoneCountX, int zoneCountY)
		{
			return new GazeZone(zoneCountX, zoneCountY, new Point(gazeX, gazeY), zoomBounds.big);
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
				if (zoomStopwatch.ElapsedMilliseconds > ZOOM_CURSOR_DELAY)
				{
					int z = 4;
					int k = 1;
					bool gazeIsSteady = false;
					GazeZone gazeZone = GetGazeZone(z, z);

					if (gazeZone.IsOnEdge())
					{
						zoomBounds.source.X += gazeZone.GetHorizontalEdgeSign() * k;
						zoomBounds.source.Y += gazeZone.GetVerticalEdgeSign() * k;
					}
					else
					{
						// Check whether the gaze moved much recently by calculating the extent of the
						// bounding box of the recent few gaze positions 
						zoomGazeHistory.Add(GetZoomGaze());
						while (zoomGazeHistory.Count > 10)
						{
							zoomGazeHistory.RemoveAt(0);
						}
						int minX = Int32.MaxValue;
						int minY = Int32.MaxValue;
						int maxX = Int32.MinValue;
						int maxY = Int32.MinValue;
						foreach (Point p in zoomGazeHistory)
						{
							minX = Math.Min(minX, p.X);
							minY = Math.Min(minY, p.Y);
							maxX = Math.Max(maxX, p.X);
							maxY = Math.Max(maxY, p.Y);
						}
						int sizeX = maxX - minX;
						int sizeY = maxY - minY;
						if (sizeX <= 10 && sizeY <= 10)
						{
							gazeIsSteady = true;
						}
					}
					if (gazeIsSteady)
					{
						int STEADY_THRESHOLD = 300;
						int CLICK_THRESHOLD = 1000;

						// First wait for the gaze to be steady for a while
						zoomSteadyStopwatch.Start();
						long t = zoomSteadyStopwatch.ElapsedMilliseconds;
						if (t > STEADY_THRESHOLD)
						{
							// Then blink for a while before automatically clicking
							int BLINK_INTERVAL = 100;
							zoomHighlight = (((t - STEADY_THRESHOLD) / BLINK_INTERVAL) % 2 == 1);
							if (t > CLICK_THRESHOLD)
							{
								Unzoom();
								followGaze = false;

								// Click
								Mouse.Press(MouseButton.Left, GetZoomGaze());
								Thread.Sleep(200);
								Mouse.Release(MouseButton.Left, GetZoomGaze());
							}
						}
					}
					else
					{
						zoomSteadyStopwatch.Reset();
						zoomHighlight = false;
					}
				}
			}
			if (state == State.Strafing)
			{
				GazeZone gazeZone = GetGazeZone(3, 3);

				if (gazeZone.IsOnRightEdge())
					PressKey('D');
				else
					ReleaseKey('D');

				if (gazeZone.IsOnLeftEdge())
					PressKey('A');
				else
					ReleaseKey('A');

				if (gazeZone.IsOnBottomEdge())
					PressKey('S');
				else
					ReleaseKey('S');

				if (gazeZone.IsOnTopEdge())
					PressKey('W');
				else
					ReleaseKey('W');
			}
			else if (state == State.Orbiting)
			{
				GazeZone gazeZone = GetGazeZone(3, 3);
				Point center = ScreenCenter();

				if (gazeZone.IsOnEdge())
				{
					int k = 5;
					Point d = gazeZone.GetEdgeSign();
					Mouse.Drag(MouseButton.Right, center, d.X*k, d.Y*k);
				}
			}
		}

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

		public static SHDocVw.InternetExplorer GetCurrentExplorerWindow()
		{
			IntPtr activeWindow = GetForegroundWindow();
			try
			{
				// Required ref: SHDocVw (Microsoft Internet Controls COM Object)
				SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();

				foreach (SHDocVw.InternetExplorer window in shellWindows)
				{
					if (window.HWND == (int)activeWindow)
					{
						return window;
					}
				}
			}
			catch (Exception)
			{
			}
			return null;
		}
		public static string GetCurrentFile()
		{
			SHDocVw.InternetExplorer window = GetCurrentExplorerWindow();
			if (window != null)
			{
				return window.Document.FocusedItem.Path;
			}
			return "";
		}

		public static string GetCurrentFolder()
		{
			SHDocVw.InternetExplorer window = GetCurrentExplorerWindow();
			if (window != null)
			{
				return window.LocationURL.Replace("file:///", "");
			}
			return "";
		}
	}
}
