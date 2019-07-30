using System;
using System.Drawing;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using Win32API;
using MFGLib;

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
		public IntPtr TargetWnd { get; protected set; } = IntPtr.Zero;

		/// <summary> 
		/// Client rectangle of the target window, top-left is always 0,0
		/// </summary>
		public Rectangle ClientRect { get { return Window.GetClientRect(TargetWnd); } }

		/// <summary> 
		/// Win32 ClientToScreen offset
		/// </summary>
		public Point ClientToScreen { get { return Window.ClientToScreen(TargetWnd); } }

		/// <summary> 
		/// Win32 ScreenToClient offset
		/// </summary>
		public Point ScreenToClient { get { return Window.ScreenToClient(TargetWnd); } }

		/// <summary> 
		/// Thread error messages used by message window
		/// </summary> 
		public string LastError { get; protected set; } = null;

		/// <summary>
		/// Turn on/off beep sounds for thread start/stop
		/// </summary>
		public bool EnableBeeps { get; set; } = false;

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
		public bool IsAlive { get { return m_thread.IsAlive; } }

		/// <summary> 
		/// Whether the thread was aborted by user
		/// </summary> 
		public bool Aborted { get { return m_thread.Aborted; } }

		/// <summary> 
		/// Pause or resume the thread
		/// </summary> 
		public bool Paused { get; set; } = false;
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
			RegisterLocales(); // Static method for localization
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
		/// <param name="tickInterval">Interval of the ticker, the ticker won't start if the parameter is 0</param> 
		/// </summary>
		public virtual bool Start(Form messageForm, int tickInterval = 1000)
		{
			if (IsAlive)
			{
				LastError = Localize("Thread is already running.");
				return false;
			}

			TargetWnd = FindTargetWnd();
			if (TargetWnd == IntPtr.Zero)
			{
				LastError = Localize("Target window not found.");
				return false;
			}

			LastError = null;
			Paused = false;

			if (!PreStart())
			{
				return false;
			}

			m_messageWnd = messageForm.Handle;
			m_thread.Start();

			// Start the ticker
			if (tickInterval > 0)
			{
				if (tickInterval < 100)
				{
					tickInterval = 100;
				}
				m_ticker.Start(tickInterval);
			}

			return true;
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
		/// <param name="obj">Object to lock, use this if null</param> 
		/// </summary>
		public void Lock(Object obj = null)
		{
			if (obj == null)
			{
				obj = this;
			}

			m_thread.Lock(obj);
		}

		/// <summary> 
		/// Sync unlock
		/// <param name="obj">Object to unlock, use this if null</param> 
		/// </summary>
		public void Unlock(Object obj = null)
		{
			if (obj == null)
			{
				obj = this;
			}

			m_thread.Lock(obj);
		}

		#endregion

		#region Overrides
		/// <summary>
		/// Find handle of the target window which the thread is dealing with
		/// </summary>
		/// <returns>Handle of the target window, or IntPtr.Zero if not exists</returns>
		public abstract IntPtr FindTargetWnd();

		/// <summary> 
		/// A callback checking before the thread starts
		/// <returns>Return true to allow the thread to start, false otherwise.</returns>
		/// </summary>
		protected virtual bool PreStart() { return true; }

		/// <summary> 
		/// Thread started
		/// </summary>
		protected virtual void OnStart() {}

		/// <summary> 
		/// Thread stopped
		/// </summary>
		protected virtual void OnStop()	{}	

		/// <summary> 
		/// Called periodically during the thread execution
		/// </summary>
		protected virtual void OnTick() {}

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
		/// Static method to find a window using class and title
		/// </summary>
		/// <param name="windowClass">Class name of the window, null to ignore</param>
		/// <param name="windowName">Window title of the window, null to ignore</param>
		/// <returns></returns>
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
			if (Window.IsIconic(TargetWnd))
			{
				Window.ShowWindow(TargetWnd, Window.SW_RESTORE);
			}
			Window.SetForegroundWindow(TargetWnd);
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
			MemDC dc = new MemDC();
			return dc.CaptureAndGetPixel(x + ClientToScreen.X, y + ClientToScreen.Y);			
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
			MemDC dc = new MemDC();
			return dc.WaitForPixel(x + ClientToScreen.X, y + ClientToScreen.Y, color, timeout, sleep);			
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

		#region Localizations
		/// <summary> 
		/// Register a new locale if not exists		
		/// <param name="name">Locale name, such as fr-FR, de-DE, ko-KR, etc</param>
		/// <returns>A Locale object</returns>
		/// </summary>
		public static Locale RegisterLocale(string name)
		{
			return m_locales.RegisterLocale(name);
		}

		/// <summary> 
		/// Translate a text into its localized form using current system locale		
		/// <param name="key">The en-US form of the text</param>
		/// <returns>The localized text</returns>
		/// </summary>
		public static string Localize(string key)
		{
			return m_locales.GetLocalizedString(key);
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
			MouseMove(x, y);
			Input.MouseClick(button);
		}

		/// <summary> 
		/// Move the cursor to the target window's client area
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client y coords</param> 
		/// </summary>
		public void MouseMove(int x, int y)
		{
			Point point = TranslateLocation(x, y);
			Input.MouseMove(point.X, point.Y);
		}

		/// <summary> 
		/// Press down a mouse button
		/// <param name="button">Mouse button</param> 
		/// </summary>
		public static void MouseDown(MouseButtons button = MouseButtons.Left)
		{
			Input.MouseDown(button);
		}

		/// <summary> 
		/// Release a mouse button
		/// <param name="button">Mouse button</param> 
		/// </summary>
		public static void MouseUp(MouseButtons button = MouseButtons.Left)
		{
			Input.MouseUp(button);
		}

		/// <summary> 
		/// Scroll the mouse wheel
		/// <param name="scrollUp">Wheel direction</param> 
		/// </summary>
		public static void MouseWheel(bool scrollUp = false)
		{
			Input.MouseWheel(scrollUp);
		}
		#endregion

		#region Target Window Keyboard Interactions

		/// <summary> 
		/// Send a keystroke	
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public static void KeyStroke(Keys key, ModKeys mods = ModKeys.None)
		{
			KeyDown(key, mods);			
			KeyUp(key, mods);
		}

		/// <summary> 
		/// Send a character		
		/// <param name="value">Character value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public static void SendChar(char value, ModKeys mods = ModKeys.None)
		{
			SendChar("" + value, mods);
		}

		/// <summary> 
		/// Send a character
		/// <param name="name">Character name</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public static void SendChar(string name, ModKeys mods = ModKeys.None)
		{
			switch (name)
			{				
				case "\n":
					name = "ENTER";
					break;

				case "\t":
					name = "TAB";
					break;

				case "\b":
					name = "BACKSPACE";
					break;

				case "\r":
				case "\0":
					name = "";
					break;

				case " ":
					name = "";
					KeyStroke(Keys.Space, mods); // SendKeys does not support {Space}
					break;

				default:
					break;
			}

			if (String.IsNullOrEmpty(name))
			{
				return;
			}			

			name = "{" + name + "}";
			if ((mods & ModKeys.Alt) != 0)
			{
				name = "%" + name;
			}

			if ((mods & ModKeys.Control) != 0)
			{
				name = "^" + name;
			}

			if ((mods & ModKeys.Shift) != 0)
			{
				name = "+" + name;
			}

			try
			{
				SendKeys.SendWait(name);
			}
			catch
			{
			}			
		}

		/// <summary> 
		/// Send a character		
		/// <param name="contents">The string contents to be sent</param>
		/// <param name="delay">Delay between each 2 characters, in milliseconds</param>
		/// </summary>
		public static void SendString(string contents, int delay = 0)
		{
			if (contents == null)
			{
				return;
			}

			foreach (char ch in contents)
			{
				SendChar(ch);
				if (delay > 0)
				{
					Thread.Sleep(delay);
				}
			}
		}

		/// <summary> 
		/// Press a key		
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public static void KeyDown(Keys key, ModKeys mods = ModKeys.None)
		{
			Input.KeyDown(key, mods);
		}

		/// <summary> 
		/// Release a key		
		/// <param name="key">Keys value</param>
		/// <param name="mods">Modifiers</param>
		/// </summary>
		public static void KeyUp(Keys key, ModKeys mods = ModKeys.None)
		{
			Input.KeyUp(key, mods);
		}

		/// <summary>
		/// Check whether a key is currently held down
		/// </summary>
		/// <param name="key">Keys value</param>
		/// <returns>Return true if the specified key is held down, false otherwise.</returns>
		public static bool IsKeyDown(Keys key)
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
					m_ticker.Dispose();
					m_thread.Dispose();
					m_soundPlayerStart.Dispose();
					m_soundPlayerStop.Dispose();
					m_soundPlayerAlert.Dispose();
				}

				m_disposed = true;
			}
		}

		private static void RegisterLocales()
		{
			Locale locale;

			locale = RegisterLocale("zh-CN");
			locale["Thread is already running."] = "线程已经在运行中。";
			locale["Target window not found."] = "目标窗口未找到。";
			locale["Failed to create device context. "] = "创建DC失败。";

			locale = RegisterLocale("zh-TW");
			locale["Thread is already running."] = "線程已經在運行中。";
			locale["Target window not found."] = "目標窗體未找到。";
			locale["Failed to create device context. "] = "創建DC失敗。";
		}
		#endregion

		#region Private Members
		private static LocaleCollection m_locales = new LocaleCollection();
		private EventThread m_thread = new EventThread();
		private TickEventThread m_ticker = new TickEventThread();
		private bool m_alerting = false; // Sound alarm on?
		private IntPtr m_messageWnd = IntPtr.Zero;
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
			if (EnableBeeps)
			{
				m_soundPlayerStart.Play();
			}

			if (TargetWnd != IntPtr.Zero && !IsTargetWndForeground())
			{
				SetTargetWndForeground();
			}

			OnStart();
			PostMessage(THREAD_MSG_START, 0);
		}

		private void _OnStop()
		{
			if (EnableBeeps)
			{
				m_soundPlayerStop.Play();
			}
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

			OnTick();
		}
		
		#endregion
	}
}
