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
		/// If true, the thread will periadically set target window foreground
		/// </summary>
		public virtual bool AutoForeground { get; }

		/// <summary>
		/// Whether hide main form (using a notification icon?)
		/// </summary>
		public virtual bool HideMainForm { get; }		

		/// <summary>
		/// Whether the thread is running
		/// </summary>
		public virtual bool IsAlive => m_thread != null && m_thread.IsAlive;

		/// <summary>
		/// Whether the thread was stopped by user
		/// </summary>
		public bool Aborted => m_thread != null && m_thread.Aborted;

		/// <summary>
		/// Handle of the target window
		/// </summary>
		public IntPtr TargetWnd => m_thread != null ? m_thread.TargetWnd : IntPtr.Zero;		

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
		public virtual void StartThread()
		{
			if (m_thread == null)
			{
				throw new NullReferenceException("Thread is null.");
			}

			try
			{
				m_thread.Start(this, AutoForeground);
			}			
			catch (Exception ex)
			{
				Messagex.Error(this, Localization.Get(ex.Message));
			}
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

			try
			{
				m_thread.Stop();
			}
			catch (Exception ex)
			{
				Messagex.Error(this, Localization.Get(ex.Message));
			}
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
		/// The user pressed a registered hotkey
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
				return true; // already registered
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
					OnHotKey(id); // other hotkey
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
		private AutomationThread m_thread = null;
		private List<int> m_hotkeys = new List<int>();

		/// <summary>
		/// Called upon Load event
		/// </summary>
		private void AutomationForm_Load(object sender, EventArgs e)
		{
			if (HideMainForm)
			{
				Hide();
				ShowInTaskbar = false;
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
				cancel = !Messagex.Confirm(this, Localization.Get("The thread is still running, exit anyway?"));
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
			// cleanup
			foreach (int id in m_hotkeys)
			{
				Hotkey.UnregisterHotKey(Handle, id);
			}

			m_hotkeys.Clear();

			IntPtr targetWnd = TargetWnd;
			if (targetWnd != IntPtr.Zero && !Window.IsWindowVisible(targetWnd))
			{
				Window.ShowWindow(targetWnd, Window.SW_SHOW);
			}

			if (m_thread != null)
			{
				m_thread.Stop();
				m_thread.Alerting = false;
			}			
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
