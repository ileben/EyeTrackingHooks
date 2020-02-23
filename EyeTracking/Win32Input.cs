using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace EyeTrackingHooks
{
	public class Win32Input
	{
		[DllImport("user32.dll")]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		internal static uint INPUT_MOUSE = 0;
		internal static uint INPUT_KEYBOARD = 1;
		internal static uint INPUT_HARDWARE = 2;

		[StructLayout(LayoutKind.Sequential)]
		internal struct INPUT
		{
			public uint Type;
			public MOUSEKEYBDHARDWAREINPUT Data;
		}

		[StructLayout(LayoutKind.Explicit)]
		internal struct MOUSEKEYBDHARDWAREINPUT
		{
			[FieldOffset(0)]
			public HARDWAREINPUT Hardware;
			[FieldOffset(0)]
			public KEYBDINPUT Keyboard;
			[FieldOffset(0)]
			public MOUSEINPUT Mouse;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct HARDWAREINPUT
		{
			public uint Msg;
			public ushort ParamL;
			public ushort ParamH;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct KEYBDINPUT
		{
			public ushort Vk;
			public ushort Scan;
			public uint Flags;
			public uint Time;
			public IntPtr ExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct MOUSEINPUT
		{
			public int X;
			public int Y;
			public uint MouseData;
			public uint Flags;
			public uint Time;
			public IntPtr ExtraInfo;
		}

		public const int VK_UP = 0x26; //up key
		public const int VK_DOWN = 0x28;  //down key
		public const int VK_LEFT = 0x25;
		public const int VK_RIGHT = 0x27;
		public const int VK_CONTROL = 0x11;

		public const uint KEYEVENTF_KEYUP = 0x0002;
		public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

		public const uint MOUSEEVENTF_MOVE = 0x0001;
		public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
		public const uint MOUSEEVENTF_LEFTUP = 0x0004;
		public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
		public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

		public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;


		public static void PressKey(int key)
		{
			//keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
			INPUT input = new INPUT
			{
				Type = INPUT_KEYBOARD
			};
			input.Data.Keyboard = new KEYBDINPUT();
			input.Data.Keyboard.Vk = (ushort)key;
			input.Data.Keyboard.Scan = 0;
			input.Data.Keyboard.Flags = 0;
			input.Data.Keyboard.Time = 0;
			input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			INPUT[] inputs = new INPUT[] { input };
			if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
			{
				throw new Exception();
			}
		}

		public static void ReleaseKey(int key)
		{
			//keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
			INPUT input = new INPUT
			{
				Type = INPUT_KEYBOARD
			};
			input.Data.Keyboard = new KEYBDINPUT();
			input.Data.Keyboard.Vk = (ushort)key;
			input.Data.Keyboard.Scan = 0;
			input.Data.Keyboard.Flags = 2;
			input.Data.Keyboard.Time = 0;
			input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			INPUT[] inputs = new INPUT[] { input };
			if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
				throw new Exception();
		}

		public static void MouseEvent(uint flags, Point position)
		{
			System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

			INPUT input = new INPUT
			{
				Type = INPUT_MOUSE
			};
			input.Data.Mouse = new MOUSEINPUT();
			input.Data.Mouse.X = (int)(((float)position.X/screenBounds.Width)*65535.0f);
			input.Data.Mouse.Y = (int)(((float)position.Y/screenBounds.Height)*65535.0f);
			input.Data.Mouse.MouseData = 0;
			input.Data.Mouse.Flags = flags | MOUSEEVENTF_ABSOLUTE;
			input.Data.Mouse.Time = 0;
			input.Data.Mouse.ExtraInfo = IntPtr.Zero;
			INPUT[] inputs = new INPUT[] { input };
			if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
				throw new Exception();
		}
	}

	public enum MouseButton
	{
		Left,
		Right
	}

	class Mouse
	{
		public static void Press(MouseButton button, Point position)
		{
			switch (button)
			{
				case MouseButton.Left:
					Win32Input.MouseEvent(Win32Input.MOUSEEVENTF_LEFTDOWN, position);
					break;
				case MouseButton.Right:
					Win32Input.MouseEvent(Win32Input.MOUSEEVENTF_RIGHTDOWN, position);
					break;
			}
		}

		public static void Release(MouseButton button, Point position)
		{
			switch (button)
			{
				case MouseButton.Left:
					Win32Input.MouseEvent(Win32Input.MOUSEEVENTF_LEFTUP, position);
					break;
				case MouseButton.Right:
					Win32Input.MouseEvent(Win32Input.MOUSEEVENTF_RIGHTUP, position);
					break;
			}
		}

		public static void Move(Point position)
		{
			Win32Input.MouseEvent(Win32Input.MOUSEEVENTF_MOVE, position);
		}

		public static void Drag(MouseButton button, Point from, Point to)
		{
			Mouse.Move(from);
			Mouse.Press(button, from);
			Mouse.Move(to);
			Mouse.Release(button, to);
		}

		private static Point MovePoint(Point p, int x, int y)
		{
			return new Point(p.X + x, p.Y + y);
		}

		public static void Drag(MouseButton button, Point from, int offsetX, int offsetY)
		{
			Drag(button, from, MovePoint(from, offsetX, offsetY));
		}
	}
}

