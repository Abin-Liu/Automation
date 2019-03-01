using System;
using System.Threading;

namespace Automation
{
	public delegate void EventThreadHandler();

	public class EventThread : IDisposable
	{
		public EventThreadHandler OnStart { get; set; } // Called when the thread starts
		public EventThreadHandler OnStop { get; set; } // Called when the thread is stopped
		public EventThreadHandler ThreadProc { get; set; } // Thread process

		public bool IsAlive
		{
			get
			{
				return m_thread != null && m_thread.IsAlive;
			}
		}

		public bool Aborted { get; private set; }

		public ThreadState ThreadState
		{
			get
			{
				return m_thread == null ? ThreadState.Unstarted : m_thread.ThreadState;
			}
		}

		public bool IsBackground
		{
			get
			{
				return m_background;
			}

			set
			{
				m_background = value;
				if (m_thread != null)
				{
					m_thread.IsBackground = value;
				}
			}
		}

		public EventThread(bool background = false)
		{
			m_background = background;
		}

		~EventThread()
		{
			Dispose(false);
		}

		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.m_disposed)
			{
				if (disposing)
				{					
				}

				Stop();
				m_disposed = true;
			}
		}

		public virtual void Start()
		{
			Stop();
			Aborted = false;
			m_thread = new Thread(new ThreadStart(_ThreadProc));
			m_thread.IsBackground = IsBackground;
			m_thread.Start();
		}

		public virtual void Stop()
		{
			if (IsAlive)
			{
				m_thread.Abort();
			}				
		}

		public void Lock(Object obj)
		{
			Monitor.Enter(obj);
		}

		public void Unlock(Object obj)
		{
			Monitor.Exit(obj);
		}		

		public void Sleep(int ms)
		{
			Thread.Sleep(ms);
		}		

		private void _ThreadProc()
		{
			OnStart?.Invoke();

			try
			{				
				ThreadProc?.Invoke();				
			}
			catch (ThreadAbortException)
			{
				Aborted = true;
			}
			finally
			{
				OnStop?.Invoke();
			}
		}

		private bool m_background = false;
		private Thread m_thread = null;
		private bool m_disposed = false;
	}	

	class TickThread : EventThread
	{
		public EventThreadHandler OnTick { get; set; } // Called on every tick 
		public int Interval { get; private set; } = 100;

		public TickThread(): base()
		{
			ThreadProc += new EventThreadHandler(TickThreadProc);
		}

		public virtual void Start(int interval)
		{
			Interval = interval > 0 ? interval : 100;
			base.Start();
		}

		protected void TickThreadProc()
		{
			while (true)
			{
				OnTick?.Invoke();
				Sleep(Interval);
			}
		}
	}
}
