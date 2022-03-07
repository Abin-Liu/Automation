using System;

namespace Automation
{
	/// <summary>
	/// 异常定义：线程已经在运行中
	/// </summary>
	public class ThreadAlreadyRunningException : Exception
	{
		/// <summary>
		/// 默认构造函数
		/// </summary>
		public ThreadAlreadyRunningException() : base("Thread is already running.")
		{
		}

		/// <summary>
		/// 带描述构造函数
		/// </summary>
		/// <param name="message">异常描述</param>
		public ThreadAlreadyRunningException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// 异常定义：目标窗口未找到
	/// </summary>
	public class TargetWindowNotFoundException : Exception
	{
		/// <summary>
		/// 默认构造函数
		/// </summary>
		public TargetWindowNotFoundException() : base("Target window not found.")
		{
		}

		/// <summary>
		/// 带描述构造函数
		/// </summary>
		/// <param name="message">异常描述</param>
		public TargetWindowNotFoundException(string message) : base(message)
		{
		}
	}
}
