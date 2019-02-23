using System;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
	public class AutomationForm : Form
	{
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

		public bool Confirm(string text)
		{
			return Message(text, MessageBoxIcon.Question, MessageBoxButtons.OKCancel) == DialogResult.OK;
		}

		public DialogResult Message(string text, MessageBoxIcon icon = MessageBoxIcon.Exclamation, MessageBoxButtons buttons = MessageBoxButtons.OK)
		{
			if (string.IsNullOrEmpty(text))
			{
				return DialogResult.Cancel;
			}

			// 显示消息框前必须先使监控线程暂停
			m_thread.Paused = true;
			if (Window.GetForegroundWindow() != this.Handle)
			{
				if (Window.IsIconic(this.Handle))
				{
					Window.ShowWindow(this.Handle, Window.SW_RESTORE); // 如果最小化先恢复
				}
				Window.SetForegroundWindow(this.Handle);
			}

			DialogResult result = MessageBox.Show(this, text, Application.ProductName, buttons, icon);
			m_thread.Paused = false;
			return result;
		}

		// 可重载方法
		protected virtual void OnThreadStart() { }
		protected virtual void OnThreadAbort() { }
		protected virtual void OnThreadStop() { }
		protected virtual void OnHotkey(int id) { }
		protected virtual void OnThreadMessage(int wParam, int lParam) { }
		protected virtual void OnMessage(int message, IntPtr wParam, IntPtr lParam) { }

		protected bool RegisterHotkey(int id, Keys key, int modifiers = 0)
		{
			return Hotkey.RegisterHotKey(this.Handle, id, modifiers, key);
		}

		protected void UnregisterHotKey(int id)
		{
			Hotkey.UnregisterHotKey(this.Handle, id);
		}

		// 继承Form必须在Form_Load中调用base.OnFormLoad(sender, e)
		protected virtual void Form_OnLoad(object sender, EventArgs e)
		{
			if (!RegisterHotkey(WM_HOTKEY_PAUSE, Keys.Pause, Hotkey.MOD_NONE))
			{
				Message("注册快捷键PAUSE失败，请先关闭占用此键的应用程序，然后重试。");
			}
		}

		// 继承Form必须在Form_OnClosing中调用base.OnFormClosing(sender, e)
		protected virtual void Form_OnClosing(object sender, FormClosingEventArgs e)
		{
			if (m_thread.IsAlive)
			{
				// 如果线程仍在运行中，需要用户确认是否真的退出
				e.Cancel = !Confirm("线程尚未结束，是否仍然结束运行？");
			}
		}

		// 继承Form必须在Form_OnClosed中调用base.OnFormClosed(sender, e)
		protected virtual void Form_OnClosed(object sender, FormClosedEventArgs e)
		{
			UnregisterHotKey(WM_HOTKEY_PAUSE);
			m_thread.Stop();
			m_thread.Alerting = false;
			m_thread.Dispose();
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

			if (message == 0x0312) // Win32 WM_HOTKEY = 0x0312
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

		private static readonly int WM_HOTKEY_PAUSE = 9035; // Pause key id
		private AutomationThread m_thread = null;
	}
}
