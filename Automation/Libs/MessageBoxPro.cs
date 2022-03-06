using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

// Depreciated API: AppDomain.GetCurrentThreadId()
#pragma warning disable 0618

namespace UIToolkits
{
	/// <summary>
	/// 解决MessageBox无法居中显示于父窗体的缺陷
	/// </summary>
	public static class MessageBoxPro
	{
		#region 常规方法: 与MessageBox相同
		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text)
		{
			Initialize();
			return MessageBox.Show(text);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text, string caption)
		{
			Initialize();
			return MessageBox.Show(text, caption);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
		{
			Initialize();
			return MessageBox.Show(text, caption, buttons);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			Initialize();
			return MessageBox.Show(text, caption, buttons, icon);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <param name="defButton">默认按钮</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButton)
		{
			Initialize();
			return MessageBox.Show(text, caption, buttons, icon, defButton);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <param name="defButton">默认按钮</param>
		/// <param name="options">显示选项</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButton, MessageBoxOptions options)
		{
			Initialize();
			return MessageBox.Show(text, caption, buttons, icon, defButton, options);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text, string caption)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text, caption);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text, caption, buttons);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text, caption, buttons, icon);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <param name="defButton">默认按钮</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButton)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text, caption, buttons, icon, defButton);
		}

		/// <summary>
		/// 显示消息框
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <param name="buttons">按钮组合</param>
		/// <param name="icon">主题图标</param>
		/// <param name="defButton">默认按钮</param>
		/// <param name="options">显示选项</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButton, MessageBoxOptions options)
		{
			_owner = owner;
			ActivateWindow(owner);
			Initialize();
			return MessageBox.Show(owner, text, caption, buttons, icon,
								   defButton, options);
		}
		#endregion

		#region 单OK按钮快捷方法
		/// <summary>
		/// 提示框: 无图标
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Prompt(string text, string caption = null)
		{
			return ShowShortcut(null, text, caption ?? GetLocale("Information"), MessageBoxIcon.None);
		}
		/// <summary>
		/// 提示框: 无图标
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题，如果为null则使用"Prompt"</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Prompt(IWin32Window owner, string text, string caption = null)
		{
			return ShowShortcut(owner, text, caption ?? GetLocale("Information"), MessageBoxIcon.None);
		}

		/// <summary>
		/// 提示框: 蓝色i图标
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Info(string text, string caption = null)
		{
			return ShowShortcut(null, text, caption ?? GetLocale("Information"), MessageBoxIcon.Information);
		}
		/// <summary>
		/// 提示框: 蓝色i图标
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Info(IWin32Window owner, string text, string caption = null)
		{
			return ShowShortcut(owner, text, caption ?? GetLocale("Information"), MessageBoxIcon.Information);
		}

		/// <summary>
		/// 提示框: 黄色感叹号图标
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Warning(string text, string caption = null)
		{
			return ShowShortcut(null, text, caption ?? GetLocale("Warning"), MessageBoxIcon.Exclamation);
		}

		/// <summary>
		/// 提示框: 黄色感叹号图标
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Warning(IWin32Window owner, string text, string caption = null)
		{
			return ShowShortcut(owner, text, caption ?? GetLocale("Warning"), MessageBoxIcon.Exclamation);
		}

		/// <summary>
		/// 提示框: 红底白叉图标
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Error(string text, string caption = null)
		{
			return ShowShortcut(null, text, caption ?? GetLocale("Error"), MessageBoxIcon.Error);
		}

		/// <summary>
		/// 提示框: 红底白叉图标
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择结果</returns>
		public static DialogResult Error(IWin32Window owner, string text, string caption = null)
		{
			return ShowShortcut(owner, text, caption ?? GetLocale("Error"), MessageBoxIcon.Error);
		}
		#endregion

		#region 双按钮方法
		/// <summary>
		/// OK/Cancel确认选择框：问号图标
		/// </summary>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择OK返回true，否则返回false</returns>
		public static bool Confirm(string text, string caption = null)
		{
			return Show(null, text, caption ?? GetLocale("Confirm"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
		}

		/// <summary>
		/// OK/Cancel确认选择框：问号图标
		/// </summary>
		/// <param name="owner">要居中显示于的父窗体</param>
		/// <param name="text">消息文字</param>
		/// <param name="caption">窗体标题</param>
		/// <returns>用户选择OK返回true，否则返回false</returns>
		public static bool Confirm(IWin32Window owner, string text, string caption = null)
		{
			return Show(owner, text, caption ?? GetLocale("Confirm"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
		}
		#endregion

		#region 私有成员与方法
		private static readonly Dictionary<string, string> WordMap = new Dictionary<string, string>();
		private static IWin32Window _owner = null;
		private static HookProc _hookProc = new HookProc(MessageBoxHookProc);
		private static IntPtr _hHook = IntPtr.Zero;
		private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
		private delegate void TimerProc(IntPtr hWnd, uint uMsg, UIntPtr nIDEvent, uint dwTime);
		private const int WH_CALLWNDPROCRET = 12;

		private enum CbtHookAction : int
		{
			HCBT_MOVESIZE = 0,
			HCBT_MINMAX = 1,
			HCBT_QS = 2,
			HCBT_CREATEWND = 3,
			HCBT_DESTROYWND = 4,
			HCBT_ACTIVATE = 5,
			HCBT_CLICKSKIPPED = 6,
			HCBT_KEYSKIPPED = 7,
			HCBT_SYSCOMMAND = 8,
			HCBT_SETFOCUS = 9
		}

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

		[DllImport("user32.dll")]
		private static extern int MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("User32.dll")]
		private static extern UIntPtr SetTimer(IntPtr hWnd, UIntPtr nIDEvent, uint uElapse, TimerProc lpTimerFunc);

		[DllImport("User32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

		[DllImport("user32.dll")]
		private static extern int UnhookWindowsHookEx(IntPtr idHook);

		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);

		[DllImport("user32.dll")]
		private static extern int EndDialog(IntPtr hDlg, IntPtr nResult);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hwnd);
		
		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

		[StructLayout(LayoutKind.Sequential)]
		private struct CWPRETSTRUCT
		{
			public IntPtr lResult;
			public IntPtr lParam;
			public IntPtr wParam;
			public uint message;
			public IntPtr hwnd;
		};

		static MessageBoxPro()
		{
			string Language = System.Globalization.CultureInfo.InstalledUICulture.Name; // 系统当前语言
			switch (Language)
			{
				case "zh-CN":
				case "zh-Hans":
				case "zh-SG":
					WordMap.Add("Information", "提示");
					WordMap.Add("Warning", "警告");
					WordMap.Add("Error", "错误");
					WordMap.Add("Confirm", "确认");
					break;

				case "zh-HK":
				case "zh-MO":
				case "zh-TW":
				case "zh-Hant":
					WordMap.Add("Information", "提示");
					WordMap.Add("Warning", "警告");
					WordMap.Add("Error", "錯誤");
					WordMap.Add("Confirm", "確認");
					break;

				default:
					break;
			}
		}

		private static string GetLocale(string key)
		{
			string value;
			if (WordMap.TryGetValue(key, out value))
			{
				return value;
			}

			return key;
		}

		private static void ActivateWindow(IWin32Window owner)
		{
			if (owner == null || owner.Handle == IntPtr.Zero || GetForegroundWindow() == owner.Handle)
			{
				return;
			}

			if (IsIconic(owner.Handle))
			{
				ShowWindow(owner.Handle, 9); // SW_RESTORE
			}

			SetForegroundWindow(owner.Handle);			
		}

		private static void Initialize()
		{
			if (_hHook != IntPtr.Zero)
			{
				throw new NotSupportedException("multiple calls are not supported");
			}

			if (_owner != null)
			{
				_hHook = SetWindowsHookEx(WH_CALLWNDPROCRET, _hookProc, IntPtr.Zero, AppDomain.GetCurrentThreadId());
			}
		}

		private static IntPtr MessageBoxHookProc(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode < 0)
			{
				return CallNextHookEx(_hHook, nCode, wParam, lParam);
			}

			CWPRETSTRUCT msg = (CWPRETSTRUCT)Marshal.PtrToStructure(lParam, typeof(CWPRETSTRUCT));
			IntPtr hook = _hHook;

			if (msg.message == (int)CbtHookAction.HCBT_ACTIVATE)
			{
				try
				{
					CenterWindow(msg.hwnd);
				}
				finally
				{
					UnhookWindowsHookEx(_hHook);
					_hHook = IntPtr.Zero;
				}
			}

			return CallNextHookEx(hook, nCode, wParam, lParam);
		}

		private static void CenterWindow(IntPtr hChildWnd)
		{
			Rectangle recChild = new Rectangle(0, 0, 0, 0);
			GetWindowRect(hChildWnd, ref recChild);

			int width = recChild.Width - recChild.X;
			int height = recChild.Height - recChild.Y;

			Rectangle recParent = new Rectangle(0, 0, 0, 0);
			GetWindowRect(_owner.Handle, ref recParent);

			Point ptCenter = new Point(0, 0);
			ptCenter.X = recParent.X + ((recParent.Width - recParent.X) / 2);
			ptCenter.Y = recParent.Y + ((recParent.Height - recParent.Y) / 2);

			Point ptStart = new Point(0, 0);
			ptStart.X = (ptCenter.X - (width / 2));
			ptStart.Y = (ptCenter.Y - (height / 2));

			ptStart.X = (ptStart.X < 0) ? 0 : ptStart.X;
			ptStart.Y = (ptStart.Y < 0) ? 0 : ptStart.Y;

			MoveWindow(hChildWnd, ptStart.X, ptStart.Y, width, height, false);
		}

		private static DialogResult ShowShortcut(IWin32Window owner, string text, string caption, MessageBoxIcon icon)
		{
			caption = caption ?? GetAppTitle();
			if (owner == null)
			{
				return Show(text, caption, MessageBoxButtons.OK, icon);
			}

			return Show(owner, text, caption, MessageBoxButtons.OK, icon);
		}

		private static string GetAppTitle()
		{
			return Path.GetFileNameWithoutExtension(Application.ExecutablePath);
		}
		#endregion
	}
}