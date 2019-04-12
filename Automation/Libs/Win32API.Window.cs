using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Win32API
{
	public delegate bool EnumWindowsCallBack(IntPtr hwnd, int lParam);	

	public class Window
	{
		[DllImport("user32.dll")]
		public extern static IntPtr FindWindow(string className, string windowName);

		[DllImport("user32.dll")]
		public extern static bool IsWindow(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern bool EnumWindows(EnumWindowsCallBack callback, int lParam);

		[DllImport("user32.dll")]
		static extern bool GetWindowText(IntPtr hWnd, StringBuilder text, int nMaxCount);

		public static string GetWindowText(IntPtr hWnd)
		{
			StringBuilder sb = new StringBuilder(1000);
			if (!GetWindowText(hWnd, sb, sb.Capacity))
			{
				return null;
			}

			return sb.ToString();
		}

		[DllImport("user32.dll")]
		static extern bool GetClassName(IntPtr hWnd, StringBuilder text, int nMaxCount);

		public static string GetClassName(IntPtr hWnd)
		{
			StringBuilder sb = new StringBuilder(1000);
			if (!GetClassName(hWnd, sb, sb.Capacity))
			{
				return null;
			}

			return sb.ToString();
		}

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hwnd, out Rectangle lpRect);

		public static Rectangle GetWindowRect(IntPtr hwnd)
		{
			Rectangle rect = new Rectangle();
			GetWindowRect(hwnd, out rect);
			return rect;
		}

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hwnd, out Rectangle lpRect);

		public static Rectangle GetClientRect(IntPtr hwnd)
		{
			Rectangle rect = new Rectangle();
			GetClientRect(hwnd, out rect);
			return rect;
		}

		[DllImport("user32.dll", EntryPoint = "ClientToScreen")]
		static extern bool _ClientToScreen(IntPtr hwnd, out Point lpPoint);		

		public static Point ClientToScreen(IntPtr hwnd)
		{
			Point offset;
			_ClientToScreen(hwnd, out offset);			
			return offset;
		}

		[DllImport("user32.dll", EntryPoint = "ScreenToClient")]
		static extern bool _ScreenToClient(IntPtr hwnd, out Point lpPoint);

		public static Point ScreenToClient(IntPtr hwnd)
		{
			Point offset;
			_ScreenToClient(hwnd, out offset);
			return offset;
		}

		public static Point WindowToScreen(IntPtr hwnd)
		{
			Point offset = new Point(0, 0);
			Rectangle rect = new Rectangle();
			if (GetWindowRect(hwnd, out rect))
			{
				offset.X = rect.X;
				offset.Y = rect.Y;
			}

			return offset;
		}

		public static Point ScreenToWindow(IntPtr hwnd)
		{
			Point offset = WindowToScreen(hwnd);
			offset.X = -offset.X;
			offset.Y = -offset.Y;
			return offset;
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsIconic(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(IntPtr hwnd);		

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

		public static readonly int SW_HIDE = 0;
		public static readonly int SW_SHOWNORMAL = 1;
		public static readonly int SW_NORMAL = 1;
		public static readonly int SW_SHOWMINIMIZED = 2;
		public static readonly int SW_SHOWMAXIMIZED = 3;
		public static readonly int SW_MAXIMIZE = 3;
		public static readonly int SW_SHOWNOACTIVATE = 4;
		public static readonly int SW_SHOW = 5;
		public static readonly int SW_MINIMIZE = 6;
		public static readonly int SW_SHOWMINNOACTIVE = 7;
		public static readonly int SW_SHOWNA = 8;
		public static readonly int SW_RESTORE = 9;
		public static readonly int SW_SHOWDEFAULT = 10;
		public static readonly int SW_FORCEMINIMIZE = 11;
		public static readonly int SW_MAX = 11;

		[DllImport("user32.dll")]
		public extern static IntPtr GetDesktopWindow();

		public static readonly int WM_USER = 0x0400; // define your own message id after this
		public static readonly int WM_APP = 0x8000;

		[DllImport("User32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		[DllImport("User32.dll")]
		public static extern int PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);		
	}
}
