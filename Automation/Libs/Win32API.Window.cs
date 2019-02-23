using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Automation.Win32API
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

		public static bool GetWindowText(IntPtr hWnd, out string text, int nMaxCount = 0)
		{
			text = "";
			if (nMaxCount < 1)
			{
				nMaxCount = 255;
			}

			StringBuilder sb = new StringBuilder(nMaxCount + 1);
			if (!GetWindowText(hWnd, sb, sb.Capacity))
			{
				return false;
			}

			text = sb.ToString();
			return true;
		}

		[DllImport("user32.dll")]
		static extern bool GetClassName(IntPtr hWnd, StringBuilder text, int nMaxCount);

		public static bool GetClassName(IntPtr hWnd, out string text, int nMaxCount = 0)
		{
			text = "";
			if (nMaxCount < 1)
			{
				nMaxCount = 255;
			}

			StringBuilder sb = new StringBuilder(nMaxCount + 1);
			if (!GetClassName(hWnd, sb, sb.Capacity))
			{
				return false;
			}

			text = sb.ToString();
			return true;
		}

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, out Rectangle lpRect);

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hwnd, out Rectangle lpRect);

		[DllImport("user32.dll", EntryPoint = "ClientToScreen")]
		static extern bool _ClientToScreen(IntPtr hwnd, out Point lpPoint);

		public static Point ClientToScreen(IntPtr hwnd, Point point)
		{
			Point offset;
			_ClientToScreen(hwnd, out offset);
			point.Offset(offset);
			return point;
		}

		[DllImport("user32.dll", EntryPoint = "ScreenToClient")]
		static extern bool _ScreenToClient(IntPtr hwnd, out Point lpPoint);

		public static Point ScreenToClient(IntPtr hwnd, Point point)
		{
			Point offset;
			_ScreenToClient(hwnd, out offset);
			point.Offset(offset);
			return point;
		}

		public static Point WindowToScreen(IntPtr hwnd, Point point)
		{
			Rectangle rect = new Rectangle();
			if (GetWindowRect(hwnd, out rect))
			{
				point.X += rect.X;
				point.Y += rect.Y;
			}
			
			return point;
		}

		public static Point ScreenToWindow(IntPtr hwnd, Point point)
		{
			Rectangle rect = new Rectangle();
			if (GetWindowRect(hwnd, out rect))
			{
				point.X -= rect.X;
				point.Y -= rect.Y;
			}
			
			return point;
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
