using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
    public abstract class AutomationThread : IDisposable
	{
		#region 公开属性
		/// <summary>
		/// 目标窗口名称，用以在线程启动时寻找目标窗口
		/// </summary>
		public string TargetWndName { get; protected set; } = null;

		/// <summary>
		/// 目标窗口类名称，用以在线程启动时寻找目标窗口
		/// </summary>
		public string TargetWndClass { get; protected set; } = null;

		/// <summary>
		/// 目标窗口句柄，计算Client坐标偏移量的依据
		/// </summary>
		public IntPtr TargetWnd { get; private set; } = IntPtr.Zero;

		/// <summary> 
		/// Win32设备上下文指针，一旦使用结束必须尽快被释放
		/// </summary> 
		public IntPtr DC { get; private set; } = IntPtr.Zero;

		/// <summary> 
		/// 检查此实例是否可以正常读取屏幕像素（拥有合法的设备上下文）
		/// </summary> 
		public bool Valid { get { return DC != IntPtr.Zero; } }

		/// <summary> 
		/// 线程设置error消息供主窗口调用
		/// </summary> 
		public string LastError { get; protected set; }

		/// <summary> 
		/// 使线程开始或停止响铃提醒
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
		/// 检查线程是否运行中
		/// </summary> 
		public bool IsAlive { get { return m_thread.IsAlive; } }

		/// <summary> 
		/// 检查线程是否由用户提前终止
		/// </summary> 
		public bool Aborted { get { return m_thread.Aborted; } }

		/// <summary> 
		/// 暂停/继续监控线程
		/// </summary> 
		public bool Paused { get; set; } = false;
		#endregion

		#region 构造/析构函数
		/// <summary> 
		/// 默认构造函数
		/// </summary>
		public AutomationThread()
		{
			m_thread.OnStart = _OnStart;
			m_thread.OnStop = _OnStop;
			m_thread.ThreadProc = _ThreadProc;
			m_ticker.OnTick = _OnTick;
			
		}

		/// <summary> 
		///析构函数
		/// </summary>
		~AutomationThread()
		{
			Dispose(false);
		}
		#endregion

		#region 线程操作
		/// <summary> 
		/// 启动线程
		/// <param name="form">接受线程消息的窗口，通常是程序主窗口</param> 
		/// <param name="tickInterval">监控线程tick间隔（毫秒），如果为0则不启动监控线程</param> 
		/// </summary>
		public virtual bool Start(Form messageForm, int tickInterval = 1000)
		{
			if (IsAlive)
			{
				LastError = "线程已经在运行中。";
				return false;
			}

			TargetWnd = IntPtr.Zero;

			// 线程启动前必须先定位目标窗口
			if (TargetWndClass == null && TargetWndName == null)
			{
				LastError = "目标窗口名称或类名称均未定义。";
				return false;
			}

			TargetWnd = Window.FindWindow(TargetWndClass, TargetWndName);
			if (TargetWnd == IntPtr.Zero)
			{
				LastError = string.Format("目标窗口未找到：[{0}]。", TargetWndName == null ? "Class: " + TargetWndClass : TargetWndName);
				return false;
			}

			// 有可能继承类在PreStart()中需要用到DC，所以及早创建
			if (!CreateDC())
			{
				LastError = "创建DC失败。";
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

			// 同时启动监控线程
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
		/// 终止线程
		/// </summary>
		public virtual void Stop()
		{
			m_ticker.Stop();
			m_thread.Stop();
		}		
		
		/// <summary> 
		/// 当前线程休眠
		/// <param name="milliseconds">休眠毫秒数</param> 
		/// </summary>
		public void Sleep(int milliseconds)
		{
			m_thread.Sleep(milliseconds);
		}

		/// <summary> 
		/// 开始同步锁
		/// <param name="obj">同步锁对象，null则以this为锁对象</param> 
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
		/// 解除同步锁
		/// <param name="obj">同步锁对象, null则以this为锁对象</param> 
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

		#region 可重载函数	
		protected virtual bool PreStart() { return true; } // 开始前状态检查，返回true启动线程
		protected virtual void OnStart() {}	// 线程开始
		protected virtual void OnStop()	{} // 线程结束		
		protected virtual void OnTick() {} // 线程活动期间每隔0.2秒调用一次，除非必要否则不建议重载
		protected abstract void ThreadProc(); // 线程工作函数
		#endregion

		#region 消息接受窗口交互
		public static readonly int THREAD_MSG_ID = Window.WM_APP + 3317;
		public static readonly int THREAD_MSG_START = 16677923;
		public static readonly int THREAD_MSG_STOP = 16677924;

		/// <summary> 
		/// 向线程消息接受窗口发送一个消息
		/// <param name="wParam">Win32消息约定参数WParam</param> 
		/// <param name="lParam">Win32消息约定参数LParam</param> 		/// 
		/// </summary>
		protected void PostMessage(int wParam, int lParam)
		{
			if (m_messageWnd != IntPtr.Zero)
			{
				Window.PostMessage(m_messageWnd, THREAD_MSG_ID, wParam, lParam);
			}			
		}
		#endregion

		#region 目标窗口常用操作
		public virtual bool SetTargetWnd(string windowName, string windowClass = null)
		{
			IntPtr targetWnd = IntPtr.Zero;
			if (windowName != null || windowClass != null)
			{
				targetWnd = Window.FindWindow(windowClass, windowName);
			}
			TargetWnd = targetWnd;
			return targetWnd != IntPtr.Zero;
		}		

		/// <summary> 
		/// 检测目标窗口是否为当前活动窗口
		/// <returns>如果目标窗口为当前活动窗口返回true，否则返回false</returns>
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
		/// 将目标窗口设置为当前活动窗口		
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
				Window.ShowWindow(TargetWnd, Window.SW_RESTORE); // 如果最小化先恢复
			}
			Window.SetForegroundWindow(TargetWnd);
		}

		/// <summary> 
		/// 将目标窗口的客户端坐标转换为屏幕坐标
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// <returns>屏幕坐标</returns>
		/// </summary>
		public Point TranslateLocation(int x, int y)
		{
			return Window.ClientToScreen(TargetWnd, new Point(x, y));
		}

		/// <summary> 
		/// 将目标窗口的客户端坐标转换为屏幕坐标
		/// <param name="point">客户端坐标</param> 
		/// <returns>屏幕坐标</returns>
		/// </summary>
		public Point TranslateLocation(Point point)
		{
			return Window.ClientToScreen(TargetWnd, point);
		}

		/// <summary> 
		/// 获取目标窗口客户端指定坐标位置的像素RGB值
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// <returns>RGB值</returns>
		/// </summary>
		public int GetPixel(int x, int y)
		{
			Point point = TranslateLocation(x, y);
			return GDI.GetPixel(DC, point.X, point.Y);
		}

		/// <summary> 
		/// 检查目标窗口客户端指定坐标位置的像素RGB值是否符合输入值
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// <param name="r">像素R值</param> 
		/// <param name="g">像素G值</param> 
		/// <param name="b">像素B值</param> 
		/// <returns>如果客户端指定坐标像素RGB值符合输入值则返回true，否则返回false</returns>
		/// </summary>
		public bool CheckPixel(int x, int y, byte r, byte g, byte b)
		{
			return GetPixel(x, y) == GDI.RGB(r, g, b);
		}

		/// <summary> 
		/// 持续检查目标窗口客户端指定坐标位置的像素RGB值是否符合输入值
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// <param name="r">像素R值</param> 
		/// <param name="g">像素G值</param> 
		/// <param name="b">像素B值</param> 
		/// <param name="timeout">最大检查时间（毫秒），0为永不超时</param>
		/// <returns>如果在timeout规定时间之内检查到客户端指定坐标像素RGB值符合输入值则返回true，否则返回false</returns>
		/// </summary>
		public bool WaitForPixel(int x, int y, byte r, byte g, byte b, int timeout = 0)
		{
			DateTime start = DateTime.Now;
			while (!CheckPixel(x, y, r, g, b))
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
		/// 将鼠标移动到目标窗口客户端指定坐标位置
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// </summary>
		public void MouseMove(int x, int y)
		{
			Point point = TranslateLocation(x, y);
			Input.MouseMove(point.X, point.Y);
		}

		/// <summary> 
		/// 在目标窗口客户端指定坐标位置点击鼠标左键
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// </summary>
		public void LeftClick(int x, int y)
		{
			MouseMove(x, y);
			Input.MouseClick(MouseButtons.Left);
		}

		/// <summary> 
		/// 在目标窗口客户端指定坐标位置点击鼠标右键
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// </summary>
		public void RightClick(int x, int y)
		{
			MouseMove(x, y);
			Input.MouseClick(MouseButtons.Right);
		}

		/// <summary> 
		/// 在目标窗口客户端指定坐标位置点击鼠标中键
		/// <param name="x">客户端坐标x</param> 
		/// <param name="y">客户端坐标y</param> 
		/// </summary>
		public void MiddleClick(int x, int y)
		{
			MouseMove(x, y);
			Input.MouseClick(MouseButtons.Middle);
		}

		/// <summary> 
		/// 发送执行某个操作前先延迟一段时间以确保稳定性，同时检查线程Pause状态
		/// <param name="milliseconds">延迟毫秒数</param> 
		/// </summary>
		public void DelayBeforeAction(int milliseconds = 200)
		{
			Sleep(milliseconds);
			while (_NeedPauseThreads())
			{
				Sleep(2000);
			}
		}
		#endregion

		#region IDisposable接口实现
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
		#endregion

		#region 私有成员
		private EventThread m_thread = new EventThread(true);
		private TickThread m_ticker = new TickThread();
		private bool m_alerting = false; // 是否正在响铃报警
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
			PostMessage(THREAD_MSG_START, 0); // 通知线程消息接受窗口
		}

		private void _OnStop()
		{
			m_soundPlayerStop.Play();			
			OnStop();
			ReleaseDC();
			PostMessage(THREAD_MSG_STOP, 0); // 通知线程消息接受窗口
		}

		private void _ThreadProc()
		{
			ThreadProc();
		}

		// 周期性检查目标窗口状态
		private void _OnTick()
		{
			if (TargetWnd == IntPtr.Zero || _NeedPauseThreads())
			{
				return;
			}

			// 目标窗口已关闭或被隐藏，直接终止线程
			if (!Window.IsWindow(TargetWnd) || !Window.IsWindowVisible(TargetWnd))
			{
				Stop();
				return;
			}

			// 确保目标窗口在最前台，但允许消息接收窗口在前台（用户正在操作GUI）
			IntPtr foregroundWnd = Window.GetForegroundWindow();
			if (foregroundWnd != TargetWnd && foregroundWnd != m_messageWnd)
			{
				SetTargetWndForeground();
			}

			OnTick();
		}

		/// <summary> 
		/// 创建设备上下文
		/// <returns>创建成功返回true，否则返回false</returns>
		/// </summary>
		private bool CreateDC()
		{
			ReleaseDC();
			DC = GDI.GetDC(IntPtr.Zero);
			return DC != IntPtr.Zero;
		}

		/// <summary> 
		/// 释放设备上下文（非常重要），Windows不会主动释放GDI非托管资源		
		/// </summary>
		private void ReleaseDC()
		{
			if (DC != IntPtr.Zero)
			{
				GDI.ReleaseDC(IntPtr.Zero, DC); // 释放GDI资源
				DC = IntPtr.Zero;
			}
		}
		#endregion
	}
}
