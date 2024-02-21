using System;
using System.Drawing;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using Win32API;
using AbinLibs;

namespace Automation
{
	/// <summary>
	/// The abstract thread class interact with the target window
	/// </summary>
	public abstract class AutomationThread : IDisposable
	{
		#region Public Properties
		/// <summary>
		/// Handle of the target window
		/// </summary>
		public IntPtr TargetWnd { get; private set; } = IntPtr.Zero;

		/// <summary>
		/// Checks whether there's a target window
		/// </summary>
		public bool HasTargetWnd => TargetWnd != IntPtr.Zero;

		/// <summary> 
		/// Client rectangle of the target window, top-left is always 0,0
		/// </summary>
		public Rectangle ClientRect => Window.GetClientRect(TargetWnd);

		/// <summary> 
		/// Win32 ClientToScreen offset
		/// </summary>
		public Point ClientToScreen => Window.ClientToScreen(TargetWnd);

		/// <summary> 
		/// Win32 ScreenToClient offset
		/// </summary>
		public Point ScreenToClient => Window.ScreenToClient(TargetWnd);

		/// <summary> 
		/// Thread error messages used by message window
		/// </summary> 
		public string LastError { get; protected set; }		

		/// <summary> 
		/// Start or stop sound alarm
		/// </summary>
		public bool Alerting
		{
			get
			{
				return m_alerting;
			}

			set
			{
				if (value)
				{
					if (!m_alerting)
					{
						m_alerting = true;
						m_soundPlayerAlert.PlayLooping();
					}
					
				}
				else
				{
					if (m_alerting)
					{
						m_alerting = false;
						m_soundPlayerAlert.Stop();
					}					
				}				
			}
		}

		/// <summary> 
		/// Whether the thread is running
		/// </summary> 
		public bool IsAlive => m_thread.IsAlive;

		/// <summary> 
		/// Whether the thread was aborted by user
		/// </summary> 
		public bool Aborted => m_thread.Aborted;

		/// <summary> 
		/// Pause or resume the thread
		/// </summary> 
		public bool Paused { get; set; }	
		
		/// <summary>
		/// Time when thread started
		/// </summary>
		public DateTime StartTime { get; private set; }

		/// <summary>
		/// Time when thread ended
		/// </summary>
		public DateTime EndTime { get; private set; }

		/// <summary>
		/// Total time the thread had run
		/// </summary>
		public TimeSpan RunTime => EndTime < StartTime ? DateTime.Now - StartTime : EndTime - StartTime;
		#endregion

		#region C'tors
		/// <summary> 
		/// Default constructor
		/// </summary>
		public AutomationThread()
		{
			m_thread.OnStart = _OnStart;
			m_thread.OnStop = _OnStop;
			m_thread.ThreadProc = _ThreadProc;
			m_ticker.OnTick = _OnTick;
		}

		/// <summary> 
		/// Destructor
		/// </summary>
		~AutomationThread()
		{
			Dispose(false);
		}
		#endregion

		#region Thread Operations
		/// <summary> 
		/// Start the thread
		/// <param name="messageForm">The window which receives thread messages, usually the main form</param>
		/// <param name="autoForeground">If true, the thread will periadically set target window foreground</param> 
		/// </summary>
		public virtual void Start(Form messageForm, bool autoForeground)
		{
			if (IsAlive)
			{
				throw new ThreadAlreadyRunningException();
			}

			TargetWnd = FindTargetWnd();
			if (TargetWnd == IntPtr.Zero)
			{
				throw new TargetWindowNotFoundException();
			}

			LastError = null;
			Paused = false;

			PreStart();

			m_messageWnd = messageForm.Handle;
			m_thread.Start();

			// Start the ticker
			if (autoForeground)
			{
				m_ticker.Start(1500);
			}
		}

		/// <summary> 
		/// Stop the thread
		/// </summary>
		public virtual void Stop()
		{
			m_ticker.Stop();
			m_thread.Stop();
		}		
		
		/// <summary> 
		/// Sleep the current thread
		/// <param name="milliseconds">Milliseconds</param> 
		/// </summary>
		public static void Sleep(int milliseconds)
		{
			Thread.Sleep(milliseconds);
		}

		/// <summary> 
		/// Sync lock
		/// <param name="locker">Object to lock, use internal locker if null</param> 
		/// </summary>
		public void Lock(object locker = null)
		{
			if (locker == null)
			{
				locker = m_locker;
			}

			m_thread.Lock(locker);
		}

		/// <summary> 
		/// Sync unlock
		/// <param name="locker">Object to unlock, use internal locker if null</param> 
		/// </summary>
		public void Unlock(object locker = null)
		{
			if (locker == null)
			{
				locker = m_locker;
			}

			m_thread.Lock(locker);
		}

		#endregion

		#region Sound Beeps
		/// <summary>
		/// Play a short prompting beep sound
		/// </summary>
		/// <param name="start">Play start.wav is true, play stop.wav otherwise</param>
		public void Beep(bool start = true)
		{
			if (start)
			{
				m_soundPlayerStart.Play();
			}
			else
			{
				m_soundPlayerStop.Play();				
			}
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Find handle of the target window which the thread is dealing with
		/// </summary>
		/// <returns>Handle of the target window, or IntPtr.Zero if not exists</returns>
		protected abstract IntPtr FindTargetWnd();

		/// <summary> 
		/// A callback checking before the thread starts
		/// </summary>
		protected virtual void PreStart() {}

		/// <summary> 
		/// Thread started
		/// </summary>
		protected virtual void OnStart() {}

		/// <summary> 
		/// Thread stopped
		/// </summary>
		protected virtual void OnStop()	{}		

		/// <summary> 
		/// Thread work
		/// </summary>
		protected abstract void ThreadProc();		
		#endregion

		#region Message Window Interactions
		public const int THREAD_MSG_ID = Window.WM_APP + 3317;
		public const int THREAD_MSG_START = 16677923;
		public const int THREAD_MSG_STOP = 16677924;

		/// <summary> 
		/// Send a message to the message window
		/// <param name="wParam">wParam</param> 
		/// <param name="lParam">lParam</param> 		/// 
		/// </summary>
		protected void PostMessage(int wParam, int lParam)
		{
			if (m_messageWnd != IntPtr.Zero)
			{
				Window.PostMessage(m_messageWnd, THREAD_MSG_ID, wParam, lParam);
			}			
		}
		#endregion

		#region Target Window  Interactions

		/// <summary>
		/// Retrieve/update target window handle
		/// </summary>
		public virtual void UpdateTargetWnd()
		{
			TargetWnd = FindTargetWnd();
		}

		/// <summary>
		/// Static method to find a window using class and title
		/// </summary>
		/// <param name="windowClass">Class name of the window, null to ignore</param>
		/// <param name="windowName">Window title of the window, null to ignore</param>
		/// <returns>Return target window handle</returns>
		public static IntPtr FindWindow(string windowClass, string windowName)
		{
			return Window.FindWindow(windowClass, windowName);
		}

		/// <summary> 
		/// Whether the target window is foreground
		/// <returns>Return true if the target window is foreground, false otherwise</returns>
		/// </summary>
		public bool IsTargetWndForeground()
		{
			if (TargetWnd == IntPtr.Zero)
			{
				return false;
			}

			return Window.GetForegroundWindow() == TargetWnd;
		}

		/// <summary> 
		/// Set the target window foreground		
		/// </summary>
		public void SetTargetWndForeground()
		{
			if (TargetWnd == IntPtr.Zero)
			{
				return;
			}

			Window.ShowWindow(TargetWnd, Window.SW_SHOW);
			if (Window.IsMinimized(TargetWnd))
			{
				Window.ShowWindow(TargetWnd, Window.SW_RESTORE);
			}
			Window.SetForegroundWindow(TargetWnd);
		}

		/// <summary>
		/// Wait until the target window is foreground
		/// </summary>
		public void WaitTargetWndForeground()
		{
			while (!IsTargetWndForeground())
			{
				Sleep(1000);
			}
		}

		/// <summary> 
		/// Apply a delay before sending an action for stablity, also check for thread pause status.
		/// <param name="milliseconds">Delay in milliseconds</param> 
		/// </summary>
		public void DelayBeforeAction(int milliseconds = 500)
		{
			Sleep(milliseconds);
			while (_NeedPauseThreads())
			{
				Sleep(2000);
			}
		}
		#endregion

		#region Target Window Pixel Access
		/// <summary> 
		/// Read pixel RGB value from client area of the target window.
		/// <param name="x">X coords (relative to client).</param>
		/// <param name="y">Y coords (relative to client).</param>
		/// <returns>Return RGB value if success, 0 otherwise.</returns>
		/// </summary>
		public int GetPixel(int x, int y)
		{			
			return m_dc.CaptureAndGetPixel(x + ClientToScreen.X, y + ClientToScreen.Y);			
		}

		/// <summary> 
		/// Keeps checking whether a pixel of the target window matches specified RGB values
		/// <param name="x">X coords (relative to client)</param> 
		/// <param name="y">Y coords (relative to client)</param> 
		/// <param name="color">The RGB value</param> 
		/// <param name="timeout">Maximum milliseconds before timeout, 0 to check infinitely</param>
		/// <param name="sleep">Sleep the running thread between two checks, in millisecond (minimum is 100ms) </param>
		/// <returns>Return true if the pixel matches before timeout, false otherwise</returns>
		/// </summary>
		public virtual bool WaitForPixel(int x, int y, int color, int timeout, int sleep = 200)
		{
			return m_dc.WaitForPixel(x + ClientToScreen.X, y + ClientToScreen.Y, color, timeout, sleep);			
		}		

		/// <summary>
		/// Compose rgb values into an integer
		/// </summary>
		/// <param name="r">Value of r component</param>
		/// <param name="g">Value of g component</param>
		/// <param name="b">Value of b component</param>
		/// <returns>Integer form of rgb value</returns>
		public static int RGB(byte r, byte g, byte b)
		{
			return MemDC.RGB(r, g, b);
		}

		/// <summary>
		/// Compose rgb values into an integer, unlike System.Drawing.Color, it eliminates alpha value
		/// </summary>
		/// <param name="color">Value of color</param>		
		/// <returns>Integer form of rgb value</returns>
		public static int RGB(Color color)
		{
			return MemDC.RGB(color);
		}

		/// <summary>
		/// Extract the r component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the r component</returns>
		public static byte GetRValue(int color)
		{
			return MemDC.GetRValue(color);
		}

		/// <summary>
		/// Extract the g component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the g component</returns>
		public static byte GetGValue(int color)
		{
			return MemDC.GetGValue(color);
		}

		/// <summary>
		/// Extract the b component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the b component</returns>
		public static byte GetBValue(int color)
		{
			return MemDC.GetBValue(color);
		}

		/// <summary>
		/// Check pixel color with tolerances
		/// </summary>
		/// <param name="color">Color to be checked</param>
		/// <param name="rangeR">Tolerance range [min, max] for R value, null to ignore R value check</param>
		/// <param name="rangeG">Tolerance range [min, max] for G value, null to ignore G value check</param>
		/// <param name="rangeB">Tolerance range [min, max] for B value, null to ignore B value check</param>
		/// <returns>Return true if the pixel passed, false otherwise</returns>
		public static bool ExamPixel(int color, int[] rangeR, int[] rangeG, int[] rangeB)
		{
			return MemDC.ExamPixel(color, rangeR, rangeG, rangeB);
		}
		#endregion

		#region Target Window Coordinates Translation
		/// <summary> 
		/// Client coords to screen coords
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client y coords</param> 
		/// <returns>Screen coords</returns>
		/// </summary>
		public Point TranslateLocation(int x, int y)
		{
			return TranslateLocation(new Point(x, y));			
		}

		/// <summary> 
		/// Client coords to screen coords
		/// <param name="point">Client coords</param> 
		/// <returns>Screen coords</returns>
		/// </summary>
		public Point TranslateLocation(Point point)
		{
			Point offset = Window.ClientToScreen(TargetWnd);
			point.Offset(offset);
			return point;
		}
		#endregion		

		#region Target Window Mouse Interactions
		/// <summary> 
		/// Click a mouse button inside the target window's client area
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client y coords</param> 
		/// <param name="button">Mouse button</param> 
		/// </summary>
		public void MouseClick(int x, int y, MouseButtons button = MouseButtons.Left)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Point offset = ClientToScreen;
			Input.SetCursorPos(x + offset.X, y + offset.Y);
			Input.MouseClick(button);
		}

		/// <summary> 
		/// Move the cursor to the target window's client area
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client y coords</param> 
		/// </summary>
		public void MouseMove(int x, int y)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Point offset = ClientToScreen;
			Input.SetCursorPos(x + offset.X, y + offset.Y);
		}

		/// <summary>
		/// Drag the mouse from one position to another
		/// </summary>
		/// <param name="x1">X coords of the start position</param>
		/// <param name="y1">Y coords of the start position</param>
		/// <param name="x2">X coords of the end position</param>
		/// <param name="y2">Y coords of the end position</param>
		/// <param name="button">The button to be held down</param>
		public void MouseDrag(int x1, int y1, int x2, int y2, MouseButtons button = MouseButtons.Left)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Point offset = ClientToScreen;
			Input.MouseDrag(x1 + offset.X, y1 + offset.Y, x2 + offset.X, y2 + offset.Y, button);
		}

		/// <summary> 
		/// Press down a mouse button
		/// <param name="button">Mouse button</param> 
		/// </summary>
		public void MouseDown(MouseButtons button = MouseButtons.Left)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.MouseDown(button);
		}

		/// <summary> 
		/// Release a mouse button
		/// <param name="button">Mouse button</param> 
		/// </summary>
		public void MouseUp(MouseButtons button = MouseButtons.Left)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.MouseUp(button);
		}

		/// <summary> 
		/// Scroll the mouse wheel
		/// <param name="scrollUp">Wheel direction</param> 
		/// </summary>
		public void MouseWheel(bool scrollUp = false)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.MouseWheel(scrollUp);
		}
		#endregion

		#region Target Window Keyboard Interactions

		/// <summary> 
		/// Send a keystroke	
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public void KeyStroke(Keys key, Keys mods = Keys.None)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.KeyDown(key, mods);
			Input.KeyUp(key, mods);
		}		

		/// <summary> 
		/// Send a sequence of keystokes		
		/// <param name="keys">The string defines keys to be sent, same format as System.Windows.SendKeys</param>
		/// <param name="delay">Delay between each 2 key, in milliseconds</param>
		/// </summary>
		public void KeyStroke(string keys, int delay = 0)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.KeyStroke(keys, delay);
		}

		/// <summary> 
		/// Press a key		
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public void KeyDown(Keys key, Keys mods = Keys.None)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.KeyDown(key, mods);
		}

		/// <summary> 
		/// Release a key		
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public void KeyUp(Keys key, Keys mods = Keys.None)
		{
			if (!IsTargetWndForeground())
			{
				return;
			}

			Input.KeyUp(key, mods);
		}

		/// <summary>
		/// Check whether a key is currently held down
		/// </summary>
		/// <param name="key">Keys value</param>
		/// <returns>Return true if the specified key is held down, false otherwise.</returns>
		public bool IsKeyDown(Keys key)
		{
			return Input.IsKeyDown(key);
		}
		#endregion

		#region IDisposable Interface Implememtation
		/// <summary> 
		/// Dispose the object
		/// </summary>
		public virtual void Dispose()
		{			
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary> 
		/// Release unmanaged resources such as device context
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.m_disposed)
			{
				if (disposing)
				{
					Alerting = false;
					m_dc.Dispose();
					m_ticker.Dispose();
					m_thread.Dispose();
					m_soundPlayerStart.Dispose();
					m_soundPlayerStop.Dispose();
					m_soundPlayerAlert.Dispose();
				}

				m_disposed = true;
			}
		}		
		#endregion

		#region Private Members
		private EventThread m_thread = new EventThread();
		private TickEventThread m_ticker = new TickEventThread();
		private MemDC m_dc = new MemDC();
		private bool m_alerting = false; // Sound alarm on?
		private IntPtr m_messageWnd = IntPtr.Zero;
		private object m_locker = new object(); // internal object for locking
		private SoundPlayer m_soundPlayerStart = new SoundPlayer(Resources.ResourceManager.GetStream("Start"));
		private SoundPlayer m_soundPlayerStop = new SoundPlayer(Resources.ResourceManager.GetStream("Stop"));
		private SoundPlayer m_soundPlayerAlert = new SoundPlayer(Resources.ResourceManager.GetStream("Alert"));		
		private bool m_disposed = false;

		private bool _NeedPauseThreads()
		{
			return Paused || Window.GetForegroundWindow() == m_messageWnd;
		}

		private void _OnStart()
		{
			StartTime = DateTime.Now;
			EndTime = DateTime.MinValue;

			if (TargetWnd != IntPtr.Zero && !IsTargetWndForeground())
			{
				SetTargetWndForeground();
			}

			OnStart();
			PostMessage(THREAD_MSG_START, 0);
		}

		private void _OnStop()
		{
			EndTime = DateTime.Now;
			m_ticker.Stop();
			OnStop();
			PostMessage(THREAD_MSG_STOP, 0);
		}

		private void _ThreadProc()
		{
			ThreadProc();
		}

		// Check status of the target window periodically
		private void _OnTick()
		{
			if (TargetWnd == IntPtr.Zero || _NeedPauseThreads())
			{
				return;
			}

			// The target window is closed or hidden, stop the thread
			if (!Window.IsWindow(TargetWnd) || !Window.IsWindowVisible(TargetWnd))
			{
				Stop();
				return;
			}

			// Make sure the target window is foreground unless the message window is, in which case the user may be configuring
			IntPtr foregroundWnd = Window.GetForegroundWindow();
			if (foregroundWnd != TargetWnd && foregroundWnd != m_messageWnd)
			{
				SetTargetWndForeground();
			}
		}
		
		#endregion
	}
}
