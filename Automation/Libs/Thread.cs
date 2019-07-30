using System;
using System.Threading;

namespace MFGLib
{
	public abstract class GenericThread : IDisposable
	{
		/// <summary>
		/// Check whether the thread is alive
		/// </summary>
		public bool IsAlive { get { return m_thread == null ? false : m_thread.IsAlive; } }

		/// <summary>
		/// Get/set whether the thread is background
		/// </summary>
		public bool IsBackground
		{
			get
			{
				return m_background;
			}

			set
			{
				m_background = value;
				if (IsAlive)
				{
					m_thread.IsBackground = value;
				}
			}
		}

		/// <summary>
		/// Check whether the thread was aborted (stopped by throwing a ThreadAbortException)
		/// </summary>
		public bool Aborted { get; protected set; }

		/// <summary>
		/// Retrive thread state
		/// </summary>
		public ThreadState ThreadState
		{
			get
			{
				return m_thread == null ? ThreadState.Unstarted : m_thread.ThreadState;
			}
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~GenericThread()
		{
			Dispose();
		}

		/// <summary>
		/// Dispose the object
		/// </summary>
		public virtual void Dispose()
		{
			Stop();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Start the thread
		/// </summary>
		public virtual void Start()
		{
			Stop();
			Aborted = false;
			m_thread = new Thread(GetWorkerProc());
			m_thread.IsBackground = m_background;
			m_thread.Start();
		}			

		/// <summary>
		/// Stop the thread
		/// </summary>
		public virtual void Stop()
		{
			if (IsAlive)
			{
				m_thread.Abort();
			}
		}

		/// <summary>
		/// Lock an object
		/// </summary>
		/// <param name="target">Object to be marked exclusive</param>
		public void Lock(object target = null)
		{
			Monitor.Enter(target ?? m_lock);
		}

		/// <summary>
		/// Unlock an object
		/// </summary>
		/// <param name="target">Object no longer exclusive</param>
		public void Unlock(object target = null)
		{
			Monitor.Exit(target ?? m_lock);
		}

		/// <summary>
		/// Sleep the thread
		/// </summary>
		/// <param name="milliseconds">Duration in milliseconds</param>
		public void Sleep(int milliseconds)
		{
			Thread.Sleep(milliseconds);
		}

		/// <summary>
		/// Wait for a thread to stop, the function only return after the thread stops or timeout occurred.
		/// </summary>
		/// <param name="thread">The target thread to be waited.</param>
		/// <param name="timeout">Timeout in milliseconds, 0 means infinite.</param>
		/// <returns></returns>
		public static bool WaitForSingleObject(GenericThread thread, int timeout = 0)
		{
			if (thread == null)
			{
				return true;
			}

			DateTime start = DateTime.Now;
			while (thread.IsAlive)
			{
				if (timeout > 0 && (DateTime.Now - start).TotalMilliseconds > timeout)
				{
					return false;
				}

				Thread.Sleep(100);
			}

			return true;
		}

		/// <summary>
		/// Abstract member to be overridden, derived classes must provider a ThreadStart to start the thread, such like "new ThreadStart(_ThreadProc)"
		/// </summary>
		/// <returns></returns>
		protected abstract ThreadStart GetWorkerProc();

		#region Private Members
		private Thread m_thread = null;
		private bool m_background = false;
		private object m_lock = new object();
		#endregion
	}	

	public abstract class WorkerThread : GenericThread
	{
		/// <summary>
		/// Called when the thread starts
		/// </summary>
		protected virtual void OnStart()
		{
		}

		/// <summary>
		/// Called when the thread is stopped
		/// </summary>
		protected virtual void OnStop()
		{
		}

		/// <summary>
		/// Thread working function
		/// </summary>
		protected abstract void ThreadProc();

		/// <summary>
		/// Provide ThreadStart to base class
		/// </summary>
		/// <returns></returns>
		protected override ThreadStart GetWorkerProc()
		{
			return new ThreadStart(_ThreadProc);
		}		

		/// <summary>
		/// Internal thread process
		/// </summary>
		private void _ThreadProc()
		{
			OnStart();

			try
			{
				ThreadProc();
			}
			catch (ThreadAbortException)
			{
				Aborted = true;
			}
			finally
			{
				OnStop();
			}
		}		
	}

	public abstract class TickThread : WorkerThread
	{
		/// <summary>
		/// Interval between every 2 ticks, in milliseconds, default is 200 ms. Only set upon starting to ensure thread safe
		/// </summary>
		public int Interval { get; private set; } = 200;

		/// <summary>
		/// Start ticking
		/// </summary>
		/// <param name="interval">Interval between every 2 ticks</param>
		public virtual void Start(int interval = 200)
		{
			Interval = interval > 0 ? interval : 200;
			base.Start();
		}

		/// <summary>
		/// Tick function, called every Interval
		/// </summary>
		protected abstract void TickProc();

		/// <summary>
		/// Thread working function
		/// </summary>
		protected override void ThreadProc()
		{
			while (true)
			{
				TickProc();
				Sleep(Interval);
			}
		}
	}	
	
	public class EventThread : GenericThread
	{
		/// <summary>
		/// Called when the thread starts
		/// </summary>
		public EventThreadHandler OnStart { get; set; }

		/// <summary>
		/// Called when the thread is stopped
		/// </summary>
		public EventThreadHandler OnStop { get; set; }

		/// <summary>
		/// Thread working function
		/// </summary>
		public EventThreadHandler ThreadProc { get; set; }

		protected override ThreadStart GetWorkerProc()
		{
			return new ThreadStart(_ThreadProc);
		}

		/// <summary>
		/// Internal thread process
		/// </summary>
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
	}
	
	public class TickEventThread : EventThread
	{
		/// <summary>
		/// Called on every tick 
		/// </summary>
		public EventThreadHandler OnTick { get; set; }

		/// <summary>
		/// Interval between every 2 ticks, in milliseconds, default is 200 ms. Only set upon starting to ensure thread safe
		/// </summary>
		public int Interval { get; private set; } = 200;

		/// <summary>
		/// Constructor
		/// </summary>
		public TickEventThread() : base()
		{
			ThreadProc += new EventThreadHandler(_TickThreadProc);
		}

		/// <summary>
		/// Start the tick thread
		/// </summary>
		/// <param name="interval">Interval between every 2 ticks, in milliseconds</param>
		public virtual void Start(int interval = 200)
		{
			Interval = interval > 0 ? interval : 200;
			base.Start();
		}

		/// <summary>
		/// Internal tick process
		/// </summary>
		private void _TickThreadProc()
		{
			while (true)
			{
				OnTick?.Invoke();
				Sleep(Interval);
			}
		}
	}

	/// <summary>
	/// Type definition of event callback functions
	/// </summary>
	public delegate void EventThreadHandler();
}
