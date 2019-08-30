using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Win32API;

namespace Automation
{
	/// <summary>
	/// A Form derived class controls AutomationThread
	/// </summary>
	public class AutomationForm : Form
	{
		/// <summary>
		/// Whether the thread is running
		/// </summary>
		public virtual bool IsAlive { get { return m_thread.IsAlive; } }

		/// <summary>
		/// Whether the thread was stopped by user
		/// </summary>
		public bool Aborted { get { return m_thread.Aborted; } }		

		/// <summary>
		/// Automatically register the {Ctrl-Alt-B} key which triggers boss mode (hide/show the target window)
		/// </summary>
		public bool RegisterBossMode { get; set; } = false;

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
		public int ThreadTickInterval { get; set; } = 1000;

		/// <summary>
		/// Whether hide main form (using a notification icon?)
		/// </summary>
		public bool HideForm { get; set; } = false;

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
			bool success = m_thread.Start(this, ThreadTickInterval);
			if (!success)
			{
				Message(m_thread.LastError);
			}
			return success;
		}

		/// <summary>
		/// Stop the thread
		/// </summary>
		public virtual void StopThread()
		{
			m_thread.Stop();
		}

		/// <summary>
		/// Toggle the thread
		/// </summary>
		public virtual void ToggleThread()
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

		/// <summary>
		/// Display a confirmation dialog with OK and Cancel buttons
		/// <param name="text">Message text</param> 
		/// <returns>Return true if the user clicks the OK button.</returns>
		/// </summary>
		public bool Confirm(string text)
		{
			return Message(text, MessageBoxIcon.Question, MessageBoxButtons.OKCancel) == DialogResult.OK;
		}

		/// <summary>
		/// Display a message dialog
		/// <param name="text">Message text.</param> 
		/// <param name="icon">Dialog icon, default is exclamation.</param>
		/// <param name="buttons">Dialog buttons, default is single OK.</param>
		/// <returns>Return the user choice.</returns>
		/// </summary>
		public DialogResult Message(string text, MessageBoxIcon icon = MessageBoxIcon.Exclamation, MessageBoxButtons buttons = MessageBoxButtons.OK)
		{
			if (string.IsNullOrEmpty(text))
			{
				return DialogResult.Cancel;
			}

			// Pause the thread while message dialog is on
			m_thread.Paused = true;
			if (Window.GetForegroundWindow() != this.Handle)
			{
				if (Window.IsMinimized(this.Handle))
				{
					Window.ShowWindow(this.Handle, Window.SW_RESTORE);
				}
				Window.SetForegroundWindow(this.Handle);
			}

			DialogResult result = MessageBox.Show(this, text, Application.ProductName, buttons, icon);
			m_thread.Paused = false;
			return result;
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
		public bool RegisterMainKey(Keys key, ModKeys modifiers = ModKeys.None)
		{
			UnregisterMainKey();
			return RegisterHotKey(HOTKEY_ID_PAUSE, key, modifiers);
		}

		/// <summary>
		/// Unregister the main hotkey which starts/stgops the thread
		/// </summary>
		public void UnregisterMainKey()
		{
			Hotkey.UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE);
		}

		/// <summary>
		/// Register a hotkey, whenever the user presses it, the form will be notified
		/// <param name="id">Hotkey id</param>
		/// <param name="key">Key value</param>
		/// <param name="mods">Modifiers（Ctrl, Alt, Shift）, can be combined with | operator</param>
		/// <returns>Return true if success, false otherwise.</returns>
		/// </summary>
		protected bool RegisterHotKey(int id, Keys key, ModKeys mods = ModKeys.None)
		{
			if (m_hotkeys.IndexOf(id) != -1)
			{
				return false;
			}

			if (!Hotkey.RegisterHotKey(this.Handle, id, key, mods))
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
				Hotkey.UnregisterHotKey(this.Handle, id);
			}			
		}

		/// <summary>
		/// Inherited forms must call base.OnFormLoad(sender, e) in their Form_Load
		/// </summary>
		protected virtual void Form_OnLoad(object sender, EventArgs e)
		{
			if (HideForm)
			{
				this.Hide();
				this.ShowInTaskbar = false;
			}			

			if (RegisterBossMode && !RegisterHotKey(HOTKEY_ID_BOSSMODE, Keys.B, ModKeys.Control | ModKeys.Alt))
			{
				Message("Failed to register the {Ctrl-Alt-B} key.");
			}
		}

		/// <summary>
		/// Inherited forms must call base.OnFormClosing(sender, e) in their Form_OnClosing
		/// </summary>
		protected virtual void Form_OnClosing(object sender, FormClosingEventArgs e)
		{
			bool cancel = false;
			if (m_thread.IsAlive)
			{
				// Display a confirmation is the thread is alive
				cancel = !Confirm("The thread is still running, exit anyway?");
			}

			// Cleanup before close
			if (!cancel)
			{
				CleanupBeforeClose();
			}

			e.Cancel = cancel;
		}		

		/// <summary>
		/// Inherited forms must call base.OnFormClosed(sender, e) in their Form_OnClosed
		/// </summary>
		protected virtual void Form_OnClosed(object sender, FormClosedEventArgs e)
		{
			CleanupBeforeClose();
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

			m_thread.Stop();
			m_thread.Alerting = false;
			m_thread.Dispose();
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
