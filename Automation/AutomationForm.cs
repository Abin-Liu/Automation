using System;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
	public class AutomationForm : Form
	{
		public static readonly int WM_HOTKEY_PAUSE = 1; // Pause key id

		public virtual bool IsAlive { get { return m_thread.IsAlive; } }
		public bool Aborted { get { return m_thread.Aborted; } }

		protected virtual void SetThread(AutomationThread thread)
		{
			m_thread = thread;
		}

		public virtual bool StartThread(int tickInterval = 1000)
		{
			bool success = m_thread.Start(this, tickInterval);
			if (!success)
			{
				Message(m_thread.LastError);
			}
			return success;
		}

		public virtual void StopThread()
		{
			m_thread.Stop();
		}

		public virtual void Message(string text, MessageBoxIcon icon = MessageBoxIcon.Exclamation)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			if (Window.GetForegroundWindow() != this.Handle)
			{
				if (Window.IsIconic(this.Handle))
				{
					Window.ShowWindow(this.Handle, Window.SW_RESTORE); // 如果最小化先恢复
				}
				Window.SetForegroundWindow(this.Handle);
			}			

			MessageBox.Show(this, text, Application.ProductName, MessageBoxButtons.OK, icon);
		}

		protected virtual void OnThreadStart() { }
		protected virtual void OnThreadAbort() { }
		protected virtual void OnThreadStop() { }

		// 继承Form必须在Form_Load中调用base.OnFormLoad(sender, e)
		protected virtual void Form_OnLoad(object sender, EventArgs e)
		{
			if (!Hotkey.RegisterHotKey(this.Handle, WM_HOTKEY_PAUSE, Hotkey.MOD_NONE, Keys.Pause))
			{
				MessageBox.Show(this, "注册快捷键PAUSE失败，请先关闭占用此键的应用程序，然后重试。", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		// 继承Form必须在Form_OnClosing中调用base.OnFormClosing(sender, e)
		protected virtual void Form_OnClosing(object sender, FormClosingEventArgs e)
		{
			if (!m_thread.IsAlive)
			{
				return;
			}

			DialogResult result = MessageBox.Show(this, "线程尚未结束，是否仍然结束运行？", Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			e.Cancel = result != DialogResult.OK;
		}

		// 继承Form必须在Form_OnClosed中调用base.OnFormClosed(sender, e)
		protected virtual void Form_OnClosed(object sender, FormClosedEventArgs e)
		{			
			m_thread.Stop();
			m_thread.Alerting = false;
			Hotkey.UnregisterHotKey(this.Handle, WM_HOTKEY_PAUSE);
		}

		public static int IntPtrToInt(IntPtr p)
		{
			try
			{
				return (int)p;
			}
			catch
			{
				return 0;
			}
		}

		protected override void WndProc(ref Message m)
		{
			int message = m.Msg;
			IntPtr wParam = m.WParam;
			IntPtr lParam = m.LParam;

			if (message == 0x0312) // #define WM_HOTKEY 0x0312
			{
				int id = IntPtrToInt(wParam);
				if (id == WM_HOTKEY_PAUSE)
				{
					if (IsAlive)
					{
						StopThread();
					}
					else
					{
						StartThread();
					}
				}
				else
				{
					OnHotkey(id); // other hotkey
				}
			}
			else if (message == AutomationThread.THREAD_MSG_ID)
			{
				int flag = IntPtrToInt(wParam);
				if (flag == AutomationThread.THREAD_MSG_START)
				{
					OnThreadStart();
				}
				else if (flag == AutomationThread.THREAD_MSG_STOP)
				{
					OnThreadStop();
				}
				else
				{
					OnThreadMessage(flag, IntPtrToInt(lParam));
				}
			}
			else
			{
				OnMessage(m.Msg, wParam, lParam); // generic messages
			}

			base.WndProc(ref m);
		}

		protected virtual void OnHotkey(int id) { }
		protected virtual void OnThreadMessage(int wParam, int lParam) { }
		protected virtual void OnMessage(int message, IntPtr wParam, IntPtr lParam) { }
		private AutomationThread m_thread = null;
	}
}
