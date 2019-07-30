using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Win32API
{
	public class Hotkey
	{
		const int WM_HOTKEY = 0x0312;
		const int MOD_ALT = 0x01;
		const int MOD_CONTROL = 0x02;
		const int MOD_SHIFT = 0x04;

		[DllImport("user32.dll")]
		static extern bool RegisterHotKey(IntPtr hWnd, int id, int modifiers, Keys vk);

		public static bool RegisterHotKey(IntPtr hWnd, int id, Keys vk, ModKeys mods = ModKeys.None)
		{
			int flags = 0;
			if ((mods & ModKeys.Alt) != 0)
			{
				flags |= MOD_ALT;
			}

			if ((mods & ModKeys.Shift) != 0)
			{
				flags |= MOD_SHIFT;
			}

			if ((mods & ModKeys.Control) != 0)
			{
				flags |= MOD_CONTROL;
			}

			return RegisterHotKey(hWnd, id, flags, vk);
		}

		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public static int IsHotkeyEvent(ref Message m)
		{
			if (m.Msg == WM_HOTKEY)
			{
				return (int)m.WParam;
			}

			return -1;						
		}
	}
}
