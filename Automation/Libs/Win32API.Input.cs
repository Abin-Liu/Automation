using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Automation.Win32API
{
	public class Input
	{
		public enum ModKeys { None = 0, Shift = 0x01, Control = 0x02, Alt = 0x04 }

		public static void MouseMove(int x, int y)
		{
			mouse_event(MOUSEEVENTF_MOVE, 5000, 5000, 0, 0);			
			SetCursorPos(x, y);
		}

		public static void MouseDown(MouseButtons buttons)
		{
			if ((buttons & MouseButtons.Left) != 0)
			{
				mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
			}

			if ((buttons & MouseButtons.Right) != 0)
			{
				mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
			}

			if ((buttons & MouseButtons.Middle) != 0)
			{
				mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
			}
		}

		public static void MouseUp(MouseButtons buttons)
		{
			if ((buttons & MouseButtons.Left) != 0)
			{
				mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
			}

			if ((buttons & MouseButtons.Right) != 0)
			{
				mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
			}

			if ((buttons & MouseButtons.Middle) != 0)
			{
				mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
			}
		}

		public static void MouseClick(MouseButtons buttons)
		{
			MouseDown(buttons);
			MouseUp(buttons);
		}

		public static void MouseDblClick(MouseButtons buttons)
		{
			MouseClick(buttons);
			MouseClick(buttons);
		}

		public static void MouseWheel(bool scrollUp)
		{
			mouse_event(MOUSEEVENTF_WHEEL, 0, 0, scrollUp ? 120 : -120, 0);
		}

		public static void ReleaseAllKeys()
		{
			byte[] states = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				states[i] = 0;
			}

			GetKeyboardState(states);
			for (int i = 0; i < 256; i++)
			{
				if ((states[i] & 0x80) != 0)
				{
					keybd_event((byte)i, 0, 2, 0);
				}
			}
		}

		public static ModKeys RemoveRedundantMods(Keys key, ModKeys mods)
		{
			if (mods == ModKeys.None)
			{
				return mods;
			}

			if (key == Keys.Shift)
				mods &= ~ModKeys.Shift;

			if (key == Keys.Control)
				mods &= ~ModKeys.Control;

			if (key == Keys.Alt)
				mods &= ~ModKeys.Alt;

			return mods;
		}

		public static void KeyDown(Keys key, ModKeys mods = ModKeys.None)
		{
			mods = RemoveRedundantMods(key, mods);
			if ((mods & ModKeys.Alt) != 0)
			{
				keybd_event((byte)Keys.Menu, 0, 0, 0);
			}

			if ((mods & ModKeys.Control) != 0)
			{
				keybd_event((byte)Keys.ControlKey, 0, 0, 0);
			}

			if ((mods & ModKeys.Shift) != 0)
			{
				keybd_event((byte)Keys.ShiftKey, 0, 0, 0);
			}

			keybd_event((byte)key, 0, 0, 0);
		}

		public static void KeyUp(Keys key, ModKeys mods = ModKeys.None)
		{
			mods = RemoveRedundantMods(key, mods);

			keybd_event((byte)key, 0, 2, 0);

			if ((mods & ModKeys.Shift) != 0)
			{
				keybd_event((byte)Keys.ShiftKey, 0, 2, 0);
			}

			if ((mods & ModKeys.Control) != 0)
			{
				keybd_event((byte)Keys.ControlKey, 0, 2, 0);
			}

			if ((mods & ModKeys.Alt) != 0)
			{
				keybd_event((byte)Keys.Menu, 0, 2, 0);
			}
		}

		public static void KeyStroke(Keys key, ModKeys mods = ModKeys.None)
		{
			KeyDown(key, mods);
			KeyUp(key, mods);
		}

		[DllImport("user32.dll")]
		static extern int mouse_event(int flags, int x, int y, int buttons, int extraInfo);

		static readonly int MOUSEEVENTF_MOVE = 0x0001; //移动鼠标
		static readonly int MOUSEEVENTF_LEFTDOWN = 0x0002; //模拟鼠标左键按下
		static readonly int MOUSEEVENTF_LEFTUP = 0x0004; //模拟鼠标左键抬起
		static readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008; //模拟鼠标右键按下
		static readonly int MOUSEEVENTF_RIGHTUP = 0x0010; //模拟鼠标右键抬起
		static readonly int MOUSEEVENTF_MIDDLEDOWN = 0x0020; //模拟鼠标中键按下 
		static readonly int MOUSEEVENTF_MIDDLEUP = 0x0040; //模拟鼠标中键抬起
		static readonly int MOUSEEVENTF_WHEEL = 0x800; //模拟鼠标滚轮

		[DllImport("user32.dll")]
		public static extern int SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern int GetCursorPos(out Point point);

		public static Point GetCursorPos()
		{
			Point point = new Point(0, 0);
			GetCursorPos(out point);
			return point;
		}

		[DllImport("user32.dll")]
		static extern int GetKeyboardState(byte[] states);

		[DllImport("user32.dll")]
		static extern void keybd_event(byte vkCode, byte scan, int flags, int extraInfo);		
	}
}
