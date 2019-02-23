using System;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// 用以控制外挂线程的Form类
	/// </summary>
	public class AutomationForm : Form
	{
		/// <summary>
		/// 检查线程是否运行中
		/// </summary>
		public virtual bool IsAlive { get { return m_thread.IsAlive; } }

		/// <summary>
		/// 检查线程是否由用户终止
		/// </summary>
		public bool Aborted { get { return m_thread.Aborted; } }

		/// <summary>
		/// 设置线程对象
		/// <param name="thread">由继承类创建的线程对象</param> 
		/// </summary>
		protected virtual void SetThread(AutomationThread thread)
		{
			m_thread = thread;
		}

		/// <summary> 
		/// 开始线程
		/// <param name="tickInterval">监控线程的监控间隔（毫秒）</param> 
		/// <returns>如果线程成功启动返回true，否则返回false</returns>
		/// </summary>
		public virtual bool StartThread(int tickInterval = 1000)
		{
			bool success = m_thread.Start(this, tickInterval);
			if (!success)
			{
				Message(m_thread.LastError);
			}
			return success;
		}

		/// <summary>
		/// 终止线程
		/// </summary>
		public virtual void StopThread()
		{
			m_thread.Stop();
		}

		/// <summary>
		/// 显示一个确认对话框
		/// <param name="text">对话框内容文字</param> 
		/// <returns>如果用户点击了OK按钮返回true，否则返回false</returns>
		/// </summary>
		public bool Confirm(string text)
		{
			return Message(text, MessageBoxIcon.Question, MessageBoxButtons.OKCancel) == DialogResult.OK;
		}

		/// <summary>
		/// 显示一个对话框
		/// <param name="text">对话框内容文字</param> 
		/// <param name="icon">对话框图标，默认为感叹号</param>
		/// <param name="buttons">对话框按钮，默认为单个OK</param>
		/// <returns>返回用户通过点击的按钮作出的选择</returns>
		/// </summary>
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
		/// <summary>
		/// 线程已开始
		/// </summary>
		protected virtual void OnThreadStart() { }

		/// <summary>
		/// 线程非正常停止
		/// </summary>
		protected virtual void OnThreadAbort() { }

		/// <summary>
		/// 线程已终止
		/// </summary>
		protected virtual void OnThreadStop() { }

		/// <summary>
		/// 用户按下了某个通过RegisterHotkey注册的热键
		/// <param name="id">热键ID</param>
		/// </summary>
		protected virtual void OnHotkey(int id) { }

		/// <summary>
		/// 接收到来自线程的消息
		/// <param name="wParam">消息内容wParam</param>
		/// <param name="lParam">消息内容lParam</param>
		/// </summary>
		protected virtual void OnThreadMessage(int wParam, int lParam) { }

		/// <summary>
		/// 接收到一般消息
		/// <param name="message">内容ID</param>
		/// <param name="wParam">消息内容wParam</param>
		/// <param name="lParam">消息内容lParam</param>
		/// </summary>
		protected virtual void OnMessage(int message, IntPtr wParam, IntPtr lParam) { }

		/// <summary>
		/// 注册一个热键，用户在任何场合按下此键，本窗口都会收到消息
		/// <param name="id">热键ID</param>
		/// <param name="key">键位</param>
		/// <param name="modifiers">辅助键（Ctrl, Alt, Shift），可通过|混合多个辅助键</param>
		/// <returns>注册成功返回true，否则返回false</returns>
		/// </summary>
		protected bool RegisterHotkey(int id, Keys key, int modifiers = 0)
		{
			return Hotkey.RegisterHotKey(this.Handle, id, modifiers, key);
		}

		/// <summary>
		/// 取消已注册的热键
		/// <param name="id">热键ID</param>		
		/// </summary>
		protected void UnregisterHotKey(int id)
		{
			Hotkey.UnregisterHotKey(this.Handle, id);
		}

		/// <summary>
		/// 继承Form必须在Form_Load中调用 base.OnFormLoad(sender, e);
		/// </summary>
		protected virtual void Form_OnLoad(object sender, EventArgs e)
		{
			if (!RegisterHotkey(WM_HOTKEY_PAUSE, Keys.Pause, Hotkey.MOD_NONE))
			{
				Message("注册快捷键PAUSE失败，请先关闭占用此键的应用程序，然后重试。");
			}
		}

		/// <summary>
		/// 继承Form必须在Form_OnClosing中调用 base.OnFormClosing(sender, e);
		/// </summary>
		protected virtual void Form_OnClosing(object sender, FormClosingEventArgs e)
		{
			if (m_thread.IsAlive)
			{
				// 如果线程仍在运行中，需要用户确认是否真的退出
				e.Cancel = !Confirm("线程尚未结束，是否仍然结束运行？");
			}
		}

		/// <summary>
		/// 继承Form必须在Form_OnClosed中调用 base.OnFormClosed(sender, e);
		/// </summary>
		protected virtual void Form_OnClosed(object sender, FormClosedEventArgs e)
		{
			UnregisterHotKey(WM_HOTKEY_PAUSE);
			m_thread.Stop();
			m_thread.Alerting = false;
			m_thread.Dispose();
		}

		/// <summary>
		/// 重载Form类的WndProc
		/// <param name="m">消息结构</param>		
		/// </summary>
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

		#region 私有成员
		private static readonly int WM_HOTKEY_PAUSE = 9035; // Pause key id
		private AutomationThread m_thread = null;

		private static int IntPtrToInt(IntPtr p)
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
		#endregion
	}
}
