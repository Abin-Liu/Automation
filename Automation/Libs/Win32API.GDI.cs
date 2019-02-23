using System;
using System.Runtime.InteropServices;

namespace Automation.Win32API
{
	public class GDI
	{
		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern bool ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("Gdi32.dll", EntryPoint = "GetPixel")]
		private static extern int Win32GetPixel(IntPtr hdc, int x, int y);

		public static int GetPixel(IntPtr hdc, int x, int y)
		{
			int color = Win32GetPixel(hdc, x, y);			

			// RGB components from win32 GetPixel are reversed from C# .net System.Drawing.Color
			byte r = GetRValue(color);
			byte g = GetGValue(color);
			byte b = GetBValue(color);
			return RGB(b, g, r);
		}

		public static int RGB(byte r, byte g, byte b)
		{
			return ((int)r << 16) | ((short)g << 8) | b; // It's C# .net System.Drawing.Color way
		}

		public static byte GetRValue(int color)
		{
			return (byte)(color >> 16);
		}

		public static byte GetGValue(int color)
		{
			return (byte)(((short)color) >> 8);
		}

		public static byte GetBValue(int color)
		{
			return (byte)color;
		}
	}
}
