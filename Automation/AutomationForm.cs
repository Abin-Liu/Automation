using System;
using System.Windows.Forms;
using Win32API;
using UIToolkits;
using Automation.Utils;

namespace Automation
{
	/// <summary>
	/// A Form derived class controls AutomationThread
	/// </summary>
	public class AutomationForm : Form
	{
		/// <summary>
		/// Hotkey to toggle thread
		/// </summary>
		protected virtual Keys Hotkey { get; }

		/// <summary>
		/// Whether hide main form (using a notification icon?)
		/// </summary>
		protected virtual bool HideMainForm { get; }

		/// <summary>
		/// Whether the thread is running
		/// </summary>
		protected virtual bool IsAlive => m_thread != null && m_thread.IsAlive;

		/// <summary>
		/// Whether the thread was stopped by user
		/// </summary>
		protected bool Aborted => m_thread != null && m_thread.Aborted;

		/// <summary>
		/// Handle of the target window
		/// </summary>
		protected IntPtr TargetWnd => m_thread != null ? m_thread.TargetWnd : IntPtr.Zero;

		/// <summary>
		/// Wether the form is configuring, it automatically registers/unregisters hotkey
		/// </summary>
		protected bool Configuring
		{
			get
			{
				return m_configuring;
			}

			set
			{
				if (value == m_configuring)
				{
					return;
				}

				m_configuring = value;
				if (m_configuring)
				{
					UnregisterHotKey();
				}
				else
				{
					RegisterHotKey();
				}
			}
		}

		private const int HotkeyID = 51819;

		/// <summary>
		/// Default constructor
		/// </summary>
		public AutomationForm()
		{
			Load += new EventHandler(AutomationForm_Load);
			FormClosing += new FormClosingEventHandler(AutomationForm_FormClosing);
		}

		/// <summary>
		/// Set the thread member
		/// <param name="thread">An object derived from AutomationThread</param> 
		/// </summary>
		protected virtual void SetThread(AutomationThread thread)
		{
			m_thread = thread;
		}

		/// <summary> 
		/// Start the thread
		/// <returns>Return true if the thread starts successfully, false otherwise.</returns>
		/// </summary>
		protected virtual void StartThread()
		{
			if (m_thread == null)
			{
				throw new NullReferenceException("Thread is null.");
			}

			try
			{
				m_thread.Start(this);
			}			
			catch (Exception ex)
			{
				Messagex.Error(this, LangManager.Get(ex.Message));
			}
		}

		/// <summary>
		/// Stop the thread
		/// </summary>
		protected virtual void StopThread()
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
				Messagex.Error(this, LangManager.Get(ex.Message));
			}
		}

		/// <summary>
		/// Toggle the thread
		/// </summary>
		protected virtual void ToggleThread()
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
		/// Show this window and set it foreground
		/// </summary>
		protected void ShowForm()
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
					if (id == HotkeyID)
					{
						ToggleThread();
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
		private AutomationThread m_thread = null;
		private bool m_configuring = false;

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

			RegisterHotKey();
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
				cancel = !Messagex.Confirm(this, LangManager.Get("The thread is still running, exit anyway?"));
			}

			// Cleanup before close
			if (!cancel)
			{
				Cleanup();
			}

			e.Cancel = cancel;
		}		

		private void RegisterHotKey()
		{
			if (Hotkey == Keys.None)
			{
				return;
			}

			if (Win32API.Hotkey.RegisterHotKey(Handle, HotkeyID, Hotkey))
			{
				return;
			}

			Messagex.Error(this, $"Failed to register hotkey \"{Hotkey}\".");
		}

		private void UnregisterHotKey()
		{
			if (Hotkey == Keys.None)
			{
				return;
			}

			Win32API.Hotkey.UnregisterHotKey(Handle, HotkeyID);
		}

		private void Cleanup()
		{
			UnregisterHotKey();

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
