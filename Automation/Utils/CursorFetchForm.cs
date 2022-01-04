using System;
using System.Drawing;
using System.Windows.Forms;
using Win32API;

namespace Automation.Utils
{
	/// <summary>
	/// 鼠标抓取提示窗口
	/// </summary>
	public partial class CursorFetchForm : Form
	{
		/// <summary>
		/// 抓取快捷键
		/// </summary>
		public Keys HotKey { get; set; }

		/// <summary>
		/// 抓取组合键
		/// </summary>
		public Keys Modifiers { get; set; }

		/// <summary>
		/// 窗口提示信息
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// 参照窗口
		/// </summary>
		public IntPtr RefWnd { get; set; }

		/// <summary>
		/// 抓取结果
		/// </summary>
		public CursorFetchResult Result { get; private set; }

		private const int HotkeyID = 1;
		private MemDC m_dc = null;

		/// <summary>
		/// 默认构造函数
		/// </summary>
		public CursorFetchForm()
		{
			InitializeComponent();
		}

		private void CursorFetchForm_Load(object sender, EventArgs e)
		{
			Result = null;

			if (!string.IsNullOrEmpty(Message))
			{
				lblMessage.Text = Message;
			}			

			if (Hotkey.RegisterHotKey(Handle, HotkeyID, HotKey, Modifiers))
			{
				m_dc = new MemDC();
			}
			else
			{
				DialogResult = DialogResult.Cancel;
				throw new HotkeyRegisterFailureException();				
			}
		}

		private void CursorFetchForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (m_dc != null)
			{
				m_dc.Cleanup();
				m_dc = null;
				Hotkey.UnregisterHotKey(Handle, HotkeyID);
			}
		}

		private void DoFetch()
		{
			if (RefWnd != IntPtr.Zero && !Window.IsWindow(RefWnd))
			{
				RefWnd = IntPtr.Zero;
			}

			Point pt = Input.GetCursorPos();
			int color = m_dc.CaptureAndGetPixel(pt.X, pt.Y);

			if (RefWnd != IntPtr.Zero)
			{
				Point offset = Window.ScreenToClient(RefWnd);
				pt.X += offset.X;
				pt.Y += offset.Y;
			}

			Result = new CursorFetchResult()
			{
				X = pt.X,
				Y = pt.Y,
				Color = color, 
				RefWnd = RefWnd,
			};
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m"></param>
		protected override void WndProc(ref Message m)
		{
			int eventID = Hotkey.IsHotkeyEvent(ref m);
			if (eventID == HotkeyID)
			{
				DoFetch();
				DialogResult = DialogResult.OK;
			}			

			base.WndProc(ref m);
		}
	}

	/// <summary>
	/// 快捷键注册异常
	/// </summary>
	public class HotkeyRegisterFailureException : Exception
	{
		/// <summary>
		/// 默认构造函数
		/// </summary>
		public HotkeyRegisterFailureException() : base("Failed to register hotkey.")
		{
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="message">异常说明</param>
		public HotkeyRegisterFailureException(string message): base(message)
		{
		}
	}
}
