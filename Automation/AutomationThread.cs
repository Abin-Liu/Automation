using System;
using System.Drawing;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// The abstract thread class interact with the target window
	/// </summary>
	public abstract class AutomationThread : IDisposable
	{
		#region Public Properties
		/// <summary>
		/// Text of the target window
		/// </summary>
		public string TargetWndName { get; protected set; } = null;

		/// <summary>
		/// Class name of the target window
		/// </summary>
		public string TargetWndClass { get; protected set; } = null;

		/// <summary>
		/// Handle of the target window
		/// </summary>
		public IntPtr TargetWnd { get; protected set; } = IntPtr.Zero;

		/// <summary> 
		/// Win32 device context
		/// </summary> 
		public IntPtr DC { get; private set; } = IntPtr.Zero;

		/// <summary> 
		/// Whether the instance can read screen pixels
		/// </summary> 
		public bool Valid { get { return DC != IntPtr.Zero; } }

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

			if (TargetWnd != IntPtr.Zero && !Window.IsWindow(TargetWnd))
			{
				TargetWnd = IntPtr.Zero;
			}

			if (TargetWnd == IntPtr.Zero)
			{
				// The target window needs to be found before starts
				if (TargetWndClass == null && TargetWndName == null)
				{
					LastError = Localize("Neither window text nor class name is specified for target window.");
					return false;
				}

				TargetWnd = Window.FindWindow(TargetWndClass, TargetWndName);
				if (TargetWnd == IntPtr.Zero)
				{
					LastError = Localize("Target window not found - ") + (TargetWndName == null ? TargetWndClass : TargetWndName);
					return false;
				}
			}

			// Inherited threads might need to use dc in their PreStart()
			if (!CreateDC())
			{
				LastError = "Failed to create device context.";
				return false;
			}

			LastError = null;
			Paused = false;

			if (!PreStart())
			{
				ReleaseDC();
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
		public void Sleep(int milliseconds)
		{
			m_thread.Sleep(milliseconds);
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
		public static readonly int THREAD_MSG_ID = Window.WM_APP + 3317;
		public static readonly int THREAD_MSG_START = 16677923;
		public static readonly int THREAD_MSG_STOP = 16677924;

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

		#region Target Window Pixel Access
		/// <summary> 
		/// Retrieve a pixel of the target window
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client y coords</param> 
		/// <returns>RGB value</returns>
		/// </summary>
		public int GetPixel(int x, int y)
		{
			Point point = TranslateLocation(x, y);
			return GDI.GetPixel(DC, point.X, point.Y);
		}

		/// <summary> 
		/// Keeps checking whether a pixel of the target window matches specified RGB values
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client Y coords</param> 
		/// <param name="color">The RGB value</param> 
		/// <param name="timeout">Maximum milliseconds before timeout, 0 to check infinitely</param>
		/// <returns>Return true if the pixel matches before timeout, false otherwise</returns>
		/// </summary>
		public bool WaitForPixel(int x, int y, int color, int timeout)
		{
			DateTime start = DateTime.Now;
			while (GetPixel(x, y) != color)
			{
				if (timeout > 0 && (DateTime.Now - start).TotalMilliseconds > timeout)
				{
					return false;
				}

				Sleep(200);
			}
			return true;
		}

		/// <summary> 
		/// Keeps checking whether a pixel of the target window matches specified RGB values
		/// <param name="x">Client x coords</param> 
		/// <param name="y">Client Y coords</param> 
		/// <param name="r">R component</param> 
		/// <param name="g">G component</param> 
		/// <param name="b">B component</param> 
		/// <param name="timeout">Maximum milliseconds before timeout, 0 to check infinitely</param>
		/// <returns>Return true if the pixel matches before timeout, false otherwise</returns>
		/// </summary>
		public bool WaitForPixel(int x, int y, byte r, byte g, byte b, int timeout)
		{			
			return WaitForPixel(x, y, RGB(r, g, b), timeout);
		}

		/// <summary> 
		/// Compose a RGB value
		/// <param name="r">R component</param> 
		/// <param name="g">G component</param> 
		/// <param name="b">B component</param>
		/// <returns>RGB value.</returns>
		/// </summary>
		public static int RGB(byte r, byte g, byte b)
		{
			return GDI.RGB(r, g, b);
		}

		/// <summary> 
		/// Extract R component from an RGB value		
		/// <param name="color">RGB value</param>
		/// <returns>R component</returns>
		/// </summary>
		public static byte GetRValue(int color)
		{
			return GDI.GetRValue(color);
		}

		/// <summary> 
		/// Extract G component from an RGB value		
		/// <param name="color">RGB value</param>
		/// <returns>G component</returns>
		/// </summary>
		public static byte GetGValue(int color)
		{
			return GDI.GetGValue(color);
		}

		/// <summary> 
		/// Extract B component from an RGB value		
		/// <param name="color">RGB value</param>
		/// <returns>B component</returns>
		/// </summary>
		public static byte GetBValue(int color)
		{
			return GDI.GetBValue(color);
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
		public static void MouseWheel(bool scrollUp)
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
					m_ticker.Dispose();
					m_thread.Dispose();
					m_soundPlayerStart.Dispose();
					m_soundPlayerStop.Dispose();
					m_soundPlayerAlert.Dispose();
				}

				ReleaseDC();
				m_disposed = true;
			}
		}

		private static void RegisterLocales()
		{
			Locale locale;

			locale = RegisterLocale("zh-CN");
			locale["Thread is already running."] = "线程已经在运行中。";
			locale["Neither window text nor class name is specified for target window."] = "目标窗口的名称与类名均未定义。";
			locale["Target window not found - "] = "目标窗口未找到 - ";
			locale["Failed to create device context. "] = "创建DC失败。";

			locale = RegisterLocale("zh-TW");
			locale["Thread is already running."] = "線程已經在運行中。";
			locale["Neither window text nor class name is specified for target window."] = "目標窗體的名稱與類名均未定義。";
			locale["Target window not found - "] = "目標窗體未找到 - ";
			locale["Failed to create device context. "] = "創建DC失敗。";
		}
		#endregion

		#region Private Members
		private static LocaleCollection m_locales = new LocaleCollection();
		private EventThread m_thread = new EventThread(true);
		private TickThread m_ticker = new TickThread();
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
			m_soundPlayerStart.Play();

			if (TargetWnd != IntPtr.Zero && !IsTargetWndForeground())
			{
				SetTargetWndForeground();
			}

			OnStart();
			PostMessage(THREAD_MSG_START, 0);
		}

		private void _OnStop()
		{
			m_ticker.Stop();
			m_soundPlayerStop.Play();			
			OnStop();
			ReleaseDC();
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

			// Make sure the target window is foreground, but allow the message window
			IntPtr foregroundWnd = Window.GetForegroundWindow();
			if (foregroundWnd != TargetWnd && foregroundWnd != m_messageWnd)
			{
				SetTargetWndForeground();
			}

			OnTick();
		}

		/// <summary> 
		/// Create device context
		/// <returns>Return true is success, false otherwise</returns>
		/// </summary>
		private bool CreateDC()
		{
			ReleaseDC();
			DC = GDI.GetDC(IntPtr.Zero);
			return DC != IntPtr.Zero;
		}

		/// <summary> 
		/// Release device context, very important!		
		/// </summary>
		private void ReleaseDC()
		{
			if (DC != IntPtr.Zero)
			{
				GDI.ReleaseDC(IntPtr.Zero, DC);
				DC = IntPtr.Zero;
			}
		}
		#endregion
	}
}
