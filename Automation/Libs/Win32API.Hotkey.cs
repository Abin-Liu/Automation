using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Automation.Win32API
{
	public class Hotkey
	{
		public static readonly int MOD_NONE = 0x00;
		public static readonly int MOD_ALT = 0x01;
		public static readonly int MOD_CONTROL = 0x02;
		public static readonly int MOD_SHIFT = 0x04;

		[DllImport("user32.dll")]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, int modifiers, Keys vk);

		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public static int IsHotkeyEvent(ref Message m)
		{
			if (m.Msg == 0x0312) // #define WM_HOTKEY 0x0312
			{
				return (int)m.WParam;
			}

			return -1;						
		}
	}
}
