using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Win32API;
using UIToolkits;

namespace Automation
{
	/// <summary>
	/// A Form derived class controls AutomationThread
	/// </summary>
	public class AutomationForm : Form
	{
		/// <summary>
		/// Message for user trying to exit app while the thread is alive
		/// </summary>
		public string TargetNotFoundMessage { get; set; } = "Target window not found.";

		/// <summary>
		/// Message for user trying to exit app while the thread is alive
		/// </summary>
		public string ExitAliveMessage { get; set; } = "The thread is still running, exit anyway?";

		/// <summary>
		/// Message for hotkey registering failure
		/// </summary>
		public string BossModeHotKeyFailureMessage { get; set; } = "Failed to register the {Ctrl-Alt-B} key.";

		/// <summary>
		/// Whether the thread is running
		/// </summary>
		public virtual bool IsAlive => m_thread != null && m_thread.IsAlive;

		/// <summary>
		/// Whether the thread was stopped by user
		/// </summary>
		public bool Aborted => m_thread != null && m_thread.Aborted;		

		/// <summary>
		/// Automatically register the {Ctrl-Alt-B} key which triggers boss mode (hide/show the target window)
		/// </summary>
		public bool RegisterBossMode { get; set; }

		/// <summary>
		/// Whether boss mode is on
		/// </summary>
		public bool BossMode
		{
			get
			{
				return m_bossModeWnd != IntPtr.Zero;
			}

			set
			{
				if (value)
				{
					if (m_thread == null)
					{
						return;
					}

					m_bossModeWnd = m_thread.FindTargetWnd();
					if (m_bossModeWnd != IntPtr.Zero)
					{
						Window.ShowWindow(m_bossModeWnd, Window.SW_HIDE);
					}
				}
				else
				{
					if (m_bossModeWnd != IntPtr.Zero)
					{
						Window.ShowWindow(m_bossModeWnd, Window.SW_SHOW);
					}
					m_bossModeWnd = IntPtr.Zero;
				}
			
				OnBossMode(m_bossModeWnd != IntPtr.Zero);				
			}
		}

		/// <summary>
		/// Thread tick interval, in milliseconds, 0 to disable ticking
		/// </summary>
		public int ThreadTickInterval { get; set; }

		/// <summary>
		/// Whether hide main form (using a notification icon?)
		/// </summary>
		public bool HideForm { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public AutomationForm()
		{
			Load += new EventHandler(AutomationForm_Load);
			FormClosing += new FormClosingEventHandler(AutomationForm_FormClosing);
			FormClosed += new FormClosedEventHandler(AutomationForm_FormClosed);
		}

		/// <summary>
		/// Set the thread member
		/// <param name="thread">An object derived from AutomationThread</param> 
		/// </summary>
		public virtual void SetThread(AutomationThread thread)
		{
			m_thread = thread;
		}

		/// <summary> 
		/// Start the thread
		/// <returns>Return true if the thread starts successfully, false otherwise.</returns>
		/// </summary>
		public virtual bool StartThread()
		{
			if (m_thread == null)
			{
				throw new NullReferenceException("Thread is null.");
			}

			IntPtr target = m_thread.FindTargetWnd();
			if (target == IntPtr.Zero)
			{
				MessageBoxPro.Error(this, TargetNotFoundMessage);
				return false;
			}

			bool success = m_thread.Start(this, ThreadTickInterval);
			if (!success)
			{
				MessageBoxPro.Error(this, m_thread.LastError);
			}

			return success;
		}

		/// <summary>
		/// Stop the thread
		/// </summary>
		public virtual void StopThread()
		{
			if (m_thread == null)
			{
				throw new NullReferenceException("Thread is null.");
			}

			m_thread.Stop();
		}

		/// <summary>
		/// Toggle the thread
		/// </summary>
		public virtual void ToggleThread()
		{
			if (m_thread == null)
			{
				throw new NullReferenceException("Thread is null.");
			}

			if (IsAlive)
			{
				StopThread();
			}
			else
			{
				StartThread();
			}
		}		

		/// <summary>
		/// 显示本窗口并置于前台
		/// </summary>
		public void ShowForm()
		{
			if (Window.GetForegroundWindow() == Handle)
			{
				return;
			}

			if (Window.IsMinimized(Handle))
			{
				Window.ShowWindow(Handle, Window.SW_RESTORE);
			}

			Window.SetForegroundWindow(Handle);
		}		

		// Overrides
		/// <summary>
		/// Thread started
		/// </summary>
		protected virtual void OnThreadStart() { }

		/// <summary>
		/// Thread aborted
		/// </summary>
		protected virtual void OnThreadAbort() { }

		/// <summary>
		/// Thread stopped
		/// </summary>
		protected virtual void OnThreadStop() { }

		/// <summary>
		/// The user pressed a registered hotkey (Pause and Cyrl-Alt-B are exclused)
		/// <param name="id">Hotkey id</param>
		/// </summary>
		protected virtual void OnHotKey(int id) { }

		/// <summary>
		/// Event received from the thread
		/// <param name="wParam">wParam</param>
		/// <param name="lParam">lParam</param>
		/// </summary>
		protected virtual void OnThreadMessage(int wParam, int lParam) { }

		/// <summary>
		/// Generic event received
		/// <param name="message">Message id</param>
		/// <param name="wParam">wParam</param>
		/// <param name="lParam">lParam</param>
		/// </summary>
		protected virtual void OnMessage(int message, IntPtr wParam, IntPtr lParam) { }

		/// <summary>
		/// Boss mode changed
		/// <param name="bossMode">Boss mode is currently on</param>
		/// </summary>
		protected virtual void OnBossMode(bool bossMode) { }

		/// <summary>
		/// Register the main hotkey which starts/stgops the thread
		/// </summary>
		/// <param name="key">Key value</param>
		/// <param name="modifiers">Modifiers（Ctrl, Alt, Shift）, can be combined with | operator</param>
		/// <returns>Return true if success, false otherwise.</returns>
		public bool RegisterMainKey(Keys key, Keys modifiers = Keys.None)
		{
			UnregisterMainKey();
			return RegisterHotKey(HOTKEY_ID_PAUSE, key, modifiers);
		}

		/// <summary>
		/// Unregister the main hotkey which starts/stgops the thread
		/// </summary>
		public void UnregisterMainKey()
		{
			Hotkey.UnregisterHotKey(Handle, HOTKEY_ID_PAUSE);
		}

		/// <summary>
		/// Register a hotkey, whenever the user presses it, the form will be notified
		/// <param name="id">Hotkey id</param>
		/// <param name="key">Key value</param>
		/// <param name="mods">Modifiers（Ctrl, Alt, Shift）, can be combined with | operator</param>
		/// <returns>Return true if success, false otherwise.</returns>
		/// </summary>
		protected bool RegisterHotKey(int id, Keys key, Keys mods = Keys.None)
		{
			if (m_hotkeys.IndexOf(id) != -1)
			{
				return false;
			}

			if (!Hotkey.RegisterHotKey(Handle, id, key, mods))
			{
				return false;
			}

			m_hotkeys.Add(id);
			return true;
		}

		/// <summary>
		/// Unregister a hotkey
		/// <param name="id">Hotkey id</param>		
		/// </summary>
		protected void UnregisterHotKey(int id)
		{
			if (m_hotkeys.Remove(id))
			{
				Hotkey.UnregisterHotKey(Handle, id);
			}			
		}		

		/// <summary>
		/// Override WndProc
		/// <param name="m">Message struct</param>		
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			int message = m.Msg;
			IntPtr wParam = m.WParam;
			IntPtr lParam = m.LParam;

			switch (message)
			{
				case 0x0312: // Win32 WM_HOTKEY = 0x0312
					int id = IntPtrToInt(wParam);
					if (id == HOTKEY_ID_PAUSE)
					{
						ToggleThread();
					}
					else if (id == HOTKEY_ID_BOSSMODE)
					{
						BossMode = !BossMode;
					}
					else
					{
						OnHotKey(id); // other hotkey
					}
					break;

				case AutomationThread.THREAD_MSG_ID:
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
					break;

				default:
					OnMessage(m.Msg, wParam, lParam); // generic messages
					break;
			}		

			base.WndProc(ref m);
		}

		#region Private Members
		private const int HOTKEY_ID_PAUSE = 9035; // Main hotkey id
		private const int HOTKEY_ID_BOSSMODE = 9036; // Boss mode hotkey id
		private AutomationThread m_thread = null;
		private List<int> m_hotkeys = new List<int>();
		private IntPtr m_bossModeWnd = IntPtr.Zero;
		private bool m_disposed = false;

		/// <summary>
		/// Called upon Load event
		/// </summary>
		private void AutomationForm_Load(object sender, EventArgs e)
		{
			if (HideForm)
			{
				Hide();
				ShowInTaskbar = false;
			}

			if (RegisterBossMode && !RegisterHotKey(HOTKEY_ID_BOSSMODE, Keys.B, Keys.Control | Keys.Alt))
			{
				MessageBoxPro.Error(this, BossModeHotKeyFailureMessage);
			}
		}

		/// <summary>
		/// Called upon FormClosing event
		/// </summary>
		private void AutomationForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			bool cancel = false;
			if (m_thread != null && m_thread.IsAlive)
			{
				// Display a confirmation is the thread is alive
				cancel = !MessageBoxPro.Confirm(this, ExitAliveMessage);
			}

			// Cleanup before close
			if (!cancel)
			{
				CleanupBeforeClose();
			}

			e.Cancel = cancel;
		}

		/// <summary>
		/// Called upon FormClosed event
		/// </summary>
		private void AutomationForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			CleanupBeforeClose();
		}

		protected virtual void CleanupBeforeClose()
		{
			if (m_disposed)
			{
				return;
			}

			// cleanup
			foreach (int id in m_hotkeys)
			{
				Hotkey.UnregisterHotKey(this.Handle, id);
			}

			m_hotkeys.Clear();

			if (m_bossModeWnd != IntPtr.Zero && !Window.IsWindowVisible(m_bossModeWnd))
			{
				Window.ShowWindow(m_bossModeWnd, Window.SW_SHOW);
			}
			m_bossModeWnd = IntPtr.Zero;

			if (m_thread != null)
			{
				m_thread.Stop();
				m_thread.Alerting = false;
				m_thread.Dispose();
			}
			
			m_disposed = true;
		}

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
