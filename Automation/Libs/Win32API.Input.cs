using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Win32API
{
	/// <summary>
	/// A helper class for keyboard and mouse input simulation
	/// </summary>
	public class Input
	{
		/// <summary>
		/// Move the mouse to a specified screen location
		/// </summary>
		/// <param name="x">X coords (relative to screen)</param>
		/// <param name="y">Y coords (relative to screen)</param>
		public static void MouseMove(int x, int y)
		{
			SetCursorPos(x, y);
		}

		/// <summary>
		/// Drag the mouse from one position to another
		/// </summary>
		/// <param name="x1">X coords of the start position</param>
		/// <param name="y1">Y coords of the start position</param>
		/// <param name="x2">X coords of the end position</param>
		/// <param name="y2">Y coords of the end position</param>
		/// <param name="button">The button to be held down</param>
		public static void MouseDrag(int x1, int y1, int x2, int y2, MouseButtons button = MouseButtons.Left)
		{
			MouseMove(x1, y1);
			MouseDown(button);
			MouseMove(x2, y2);
			MouseUp(button);
		}

		/// <summary>
		/// Press down a mouse button
		/// </summary>
		/// <param name="button">The button to be pressed</param>
		public static void MouseDown(MouseButtons button = MouseButtons.Left)
		{
			if ((button & MouseButtons.Left) != 0)
			{
				mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
			}

			if ((button & MouseButtons.Right) != 0)
			{
				mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
			}

			if ((button & MouseButtons.Middle) != 0)
			{
				mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
			}
		}


		/// <summary>
		/// Release a mouse button
		/// </summary>
		/// <param name="button">The button to be released</param>
		public static void MouseUp(MouseButtons button = MouseButtons.Left)
		{
			if ((button & MouseButtons.Left) != 0)
			{
				mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
			}

			if ((button & MouseButtons.Right) != 0)
			{
				mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
			}

			if ((button & MouseButtons.Middle) != 0)
			{
				mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
			}
		}

		/// <summary>
		/// Click a mouse button
		/// </summary>
		/// <param name="button">The button to be clicked</param>
		public static void MouseClick(MouseButtons button = MouseButtons.Left)
		{
			MouseDown(button);
			MouseUp(button);
		}

		/// <summary>
		/// Double-click a mouse button
		/// </summary>
		/// <param name="button">The button to be clicked</param>
		public static void MouseDblClick(MouseButtons button = MouseButtons.Left)
		{
			MouseClick(button);
			MouseClick(button);
		}

		/// <summary>
		/// Scroll the mouse wheel
		/// </summary>
		/// <param name="scrollUp">Scroll direction</param>
		public static void MouseWheel(bool scrollUp)
		{
			mouse_event(MOUSEEVENTF_WHEEL, 0, 0, scrollUp ? 120 : -120, 0);
		}

		// whether a key is actually a mouse button
		static MouseButtons IsMouseAction(Keys key)
		{
			MouseButtons mb = MouseButtons.None;
			switch (key)
			{
				case Keys.LButton:
					mb = MouseButtons.Left;
					break;

				case Keys.RButton:
					mb = MouseButtons.Right;
					break;

				case Keys.MButton:
					mb = MouseButtons.Middle;
					break;

				case Keys.XButton1:
					mb = MouseButtons.XButton1;
					break;

				case Keys.XButton2:
					mb = MouseButtons.XButton2;
					break;

				default:
					break;
			}
			return mb;
		}

		/// <summary>
		/// Release all keys if held down
		/// </summary>
		public static void ReleaseAllKeys()
		{
			byte[] states = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				states[i] = 0;
			}

			GetKeyboardState(states);
			for (int i = 0; i < 256; i++)
			{
				if ((states[i] & 0x80) != 0)
				{
					sim_keybd_event((Keys)i, true);
				}
			}
		}		

		// calc scan code before keybd_event
		static void sim_keybd_event(Keys key, bool up = false)
		{
			int scan = MapVirtualKey((uint)key, 0);
			keybd_event((byte)key, (byte)scan, up ? 2 : 0, 0);
		}		

		// press modifier keys
		static void ModifiersDown(Keys mods)
		{
			if ((mods & Keys.Alt) == Keys.Alt)
			{
				sim_keybd_event(Keys.Menu);
			}

			if ((mods & Keys.Control) == Keys.Control)
			{
				sim_keybd_event(Keys.ControlKey);
			}

			if ((mods & Keys.Shift) == Keys.Shift)
			{
				sim_keybd_event(Keys.ShiftKey);
			}
		}

		// release modifier keys
		static void ModifiersUp(Keys mods)
		{
			if ((mods & Keys.Shift) == Keys.Shift)
			{
				sim_keybd_event(Keys.ShiftKey, true);
			}

			if ((mods & Keys.Control) == Keys.Control)
			{
				sim_keybd_event(Keys.ControlKey, true);
			}

			if ((mods & Keys.Alt) == Keys.Alt)
			{
				sim_keybd_event(Keys.Menu, true);
			}
		}

		/// <summary>
		/// Check whether a specified key is currently held down
		/// </summary>
		/// <param name="key">The key</param>
		/// <returns>Return true if the key is held down, false otherwise</returns>
		public static bool IsKeyDown(Keys key)
		{
			return (GetAsyncKeyState(key) & 0x8000) != 0;
		}

		/// <summary>
		/// Press down a key
		/// </summary>
		/// <param name="key">The key to be pressed down</param>
		/// <param name="mods">Modifiers</param>
		public static void KeyDown(Keys key, Keys mods = Keys.None)
		{
			ModifiersDown(mods);

			MouseButtons mb = IsMouseAction(key);
			if (mb == MouseButtons.None)
			{
				sim_keybd_event(key);
			}
			else
			{
				MouseDown(mb);
			}
		}		

		/// <summary>
		/// Release a key
		/// </summary>
		/// <param name="key">The key to be released</param>
		/// <param name="mods">Modifiers</param>
		public static void KeyUp(Keys key, Keys mods = Keys.None)
		{
			MouseButtons mb = IsMouseAction(key);
			if (mb == MouseButtons.None)
			{
				sim_keybd_event(key, true);
			}
			else
			{
				MouseUp(mb);
			}

			ModifiersUp(mods);
		}

		/// <summary>
		/// Stroke a key
		/// </summary>
		/// <param name="key">The key to be stroked</param>
		/// <param name="mods">Modifiers</param>
		public static void KeyStroke(Keys key, Keys mods = Keys.None)
		{
			KeyDown(key, mods);
			KeyUp(key, mods);
		}

		/// <summary>
		/// Stoke a sequence of keys defined in "keys"
		/// </summary>
		/// <param name="keys">definition of keys, format of which are same with SendKeys</param>
		/// <param name="delay">delay applied between each keystroke, in milliseconds</param>
		public static void KeyStroke(string keys, int delay = 0)
		{
			List<KeyAction> actions = KeyAction.Parse(keys);
			if (actions.Count == 0)
			{
				return;
			}

			foreach (KeyAction action in actions)
			{
				if (action.KeyList.Count == 0)
				{
					continue;
				}

				ModifiersDown(action.Modifiers);

				foreach (KeyPress press in action.KeyList)
				{
					if (press.Key == Keys.None)
					{
						continue;
					}

					for (int i = 0; i < press.Count; i++)
					{
						KeyStroke(press.Key);
						if (delay > 0)
						{
							Thread.Sleep(delay);
						}
					}
				}

				ModifiersUp(action.Modifiers);
			}
		}

		[DllImport("user32.dll")]
		static extern int mouse_event(int flags, int x, int y, int button, int extraInfo);

		const int MOUSEEVENTF_MOVE = 0x0001;
		const int MOUSEEVENTF_LEFTDOWN = 0x0002;
		const int MOUSEEVENTF_LEFTUP = 0x0004;
		const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
		const int MOUSEEVENTF_RIGHTUP = 0x0010;
		const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
		const int MOUSEEVENTF_MIDDLEUP = 0x0040;
		const int MOUSEEVENTF_WHEEL = 0x800;
		const int MOUSEEVENTF_ABSOLUTE = 0x8000;

		/// <summary>
		/// Set cursor pos to specified screen location
		/// </summary>
		/// <param name="x">X coords (relative to screen)</param>
		/// <param name="y">Y coords (relative to screen)</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern int SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern int GetCursorPos(out Point point);

		/// <summary>
		/// Retrieve the cursor location
		/// </summary>
		/// <returns>Return a Point struct contains X and Y coords relative to screen</returns>
		public static Point GetCursorPos()
		{
			Point point = new Point(0, 0);
			GetCursorPos(out point);
			return point;
		}

		[DllImport("user32.dll")]
		static extern int GetKeyboardState(byte[] states);

		[DllImport("user32.dll")]
		static extern void keybd_event(byte vkCode, byte scan, int flags, int extraInfo);

		[DllImport("user32.dll")]
		static extern short GetAsyncKeyState(Keys key);

		[DllImport("user32.dll")]
		static extern int MapVirtualKey(uint code, uint mapType);		
	}

	#region SendKeys脚本解析
	/// <summary>
	/// 简单击键动作
	/// </summary>
	class KeyPress
	{
		/// <summary>
		/// 键值
		/// </summary>
		public Keys Key { get; set; }

		/// <summary>
		/// 按键次数
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 默认构造函数
		/// </summary>
		public KeyPress()
		{
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="name">表示按键动作的字符串</param>
		public KeyPress(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}

			int index = -1;
			Count = ParseCount(name, out index);
			if (index > 0)
			{
				name = name.Substring(0, index);
			}

			Key = ParseKey(name);
		}

		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (Count < 2)
			{
				return Key.ToString();
			}

			return string.Format("{0} x{1}", Key.ToString(), Count);
		}

		// 解析按键次数
		// {F1 6} // F1键按6次
		static readonly Regex Reg = new Regex(@" (\d+)$"); // (...)
		static int ParseCount(string name, out int index)
		{
			index = -1;
			Match match = Reg.Match(name);
			if (!match.Success)
			{
				return 1;
			}

			index = match.Index;
			try
			{
				int count = Convert.ToInt32(match.Groups[1].Value);
				return count > 1 ? count : 1;
			}
			catch
			{
				return 1;
			}
		}

		// 解析按键键值
		static Keys ParseKey(string name)
		{
			name = name.ToUpper();
			switch (name)
			{
				#region 鼠标键
				case "LBUTTON":
				case "LEFTBUTTON":
					name = "LButton";
					break;

				case "RBUTTON":
				case "RIGHTBUTTON":
					name = "RButton";
					break;

				case "MBUTTON":
				case "MIDDLEBUTTON":
					name = "MButton";
					break;

				case "XBUTTON1":
					name = "XButton1";
					break;

				case "XBUTTON2":
					name = "XButton2";
					break;
				#endregion

				#region 特殊键
				case "ESC":
				case "ESCAPE":
					name = "Escape";
					break;

				case "BACKSPACE":
				case "BS":
				case "BKSP":
					name = "Back";
					break;

				case "BREAK":
				case "PAUSE":
					name = "Pause";
					break;

				case "CAPSLOCK":
				case "CAPS":
					name = "CapsLock";
					break;

				case "DELETE":
				case "DEL":
					name = "Delete";
					break;

				case "ENTER":
				case "RETURN":
				case "\n":
					name = "Enter";
					break;

				case "HELP":
					name = "Help";
					break;

				case "HOME":
					name = "Home";
					break;

				case "END":
					name = "End";
					break;

				case "PGDN":
				case "PAGEDOWN":
					name = "PageDown";
					break;

				case "PGUP":
				case "PAGEUP":
					name = "PageUp";
					break;

				case "INSERT":
				case "INS":
					name = "Insert";
					break;

				case "PRTSC":
				case "PRINTSCREEN":
					name = "PrintScreen";
					break;

				case "SCROLLLOCK":
					name = "Scroll";
					break;

				case "TAB":
				case "\t":
					name = "Tab";
					break;

				case "WIN":
				case "WINDOWS":
					name = "LWin";
					break;
				#endregion

				#region 组合键
				case "SHIFT":
					name = "ShiftKey";
					break;

				case "CONTROL":
				case "CTRL":
					name = "ControlKey";
					break;

				case "ALT":
				case "MENU":
					name = "AltKey";
					break;
				#endregion

				#region 方向键
				case "UP":
					name = "Up";
					break;

				case "DOWN":
					name = "Down";
					break;

				case "LEFT":
					name = "Left";
					break;

				case "RIGHT":
					name = "Right";
					break;
				#endregion

				#region 可读键
				case "`":
					name = "Oem3";
					break;

				case "-":
					name = "OemMinus";
					break;

				case "=":
					name = "Oemplus";
					break;

				case "[":
					name = "Oem4";
					break;

				case "]":
					name = "Oem6";
					break;

				case "\\":
					name = "Oem5";
					break;

				case ";":
					name = "Oem1";
					break;

				case "'":
					name = "Oem7";
					break;

				case ",":
					name = "Oemcomma";
					break;

				case ".":
					name = "OemPeriod";
					break;

				case "/":
					name = "Oem2";
					break;

				case " ":
				case "SPACE":
					name = "Space";
					break;

				case "0":
				case "1":
				case "2":
				case "3":
				case "4":
				case "5":
				case "6":
				case "7":
				case "8":
				case "9":
					name = "D" + name;
					break;
				#endregion

				#region 小键盘
				case "NUMLOCK":
					name = "NumLock";
					break;

				case "NUM0":
				case "NUM1":
				case "NUM2":
				case "NUM3":
				case "NUM4":
				case "NUM5":
				case "NUM6":
				case "NUM7":
				case "NUM8":
				case "NUM9":
					name = "NumPad" + name.Substring(3, 1);
					break;

				case "NUMPAD0":
				case "NUMPAD1":
				case "NUMPAD2":
				case "NUMPAD3":
				case "NUMPAD4":
				case "NUMPAD5":
				case "NUMPAD6":
				case "NUMPAD7":
				case "NUMPAD8":
				case "NUMPAD9":
					name = "NumPad" + name.Substring(6, 1);
					break;

				case "NUM+":
				case "NUMPAD+":
					name = "Add";
					break;

				case "NUM-":
				case "NUMPAD-":
					name = "Subtract";
					break;

				case "NUM*":
				case "NUMPAD*":
					name = "Multiply";
					break;

				case "NUM/":
				case "NUMPAD/":
					name = "Divide";
					break;

				case "NUM.":
				case "NUMPAD.":
					name = "Decimal";
					break;
				#endregion

				default:
					break;
			}

			Keys key;
			if (Enum.TryParse<Keys>(name, out key))
			{
				return key;
			}

			return Keys.None;
		}
	}

	/// <summary>
	/// 复杂击键动作(包含组合键及重复键)
	/// </summary>
	class KeyAction
	{
		/// <summary>
		/// 组合键(多个组合键以|混合)
		/// </summary>
		public Keys Modifiers { get; set; }

		/// <summary>
		/// 击键内容
		/// </summary>
		public List<KeyPress> KeyList { get; private set; } = new List<KeyPress>();

		static readonly Regex RegexBrackets = new Regex(@"^\((.*?)\)"); // (...)
		static readonly Regex RegexCurly = new Regex(@"^{(.*?)}"); // {...}

		/// <summary>
		/// 将字符串解析为击键动作队列
		/// </summary>
		/// <param name="text">字符串</param>
		/// <returns>击键动作队列</returns>
		public static List<KeyAction> Parse(string text)
		{
			List<KeyAction> dataList = new List<KeyAction>();
			while (!string.IsNullOrEmpty(text))
			{
				KeyAction action = new KeyAction();
				int modCount = 0;
				action.Modifiers = ParseModifiers(text, out modCount);
				if (modCount > 0)
				{
					text = text.Substring(modCount);
				}

				if (text == "")
				{
					break;
				}

				// 查找(...)
				Match match = RegexBrackets.Match(text);
				if (match.Success)
				{
					ParseGroup(match.Groups[1].Value, action.KeyList);
					dataList.Add(action);
					text = text.Substring(match.Length);
					continue;
				}

				// 查找{...}
				match = RegexCurly.Match(text);
				if (match.Success)
				{
					action.KeyList.Add(new KeyPress(match.Groups[1].Value));
					dataList.Add(action);
					text = text.Substring(match.Length);
					continue;
				}

				action.KeyList.Add(new KeyPress(text.Substring(0, 1)));
				dataList.Add(action);
				text = text.Substring(1);
			}

			return dataList;
		}

		// 解析组合键
		static Keys ParseModifiers(string text, out int count)
		{
			count = 0;
			Keys modifiers = Keys.None;
			for (int i = 0; i < text.Length; i++)
			{
				Keys mod = IsModifier(text[i]);
				if (mod == Keys.None)
				{
					break;
				}

				modifiers |= mod;
				count++;
			}

			return modifiers;
		}

		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string text = string.Join(", ", KeyList);
			if (Modifiers != Keys.None)
			{
				text = Modifiers.ToString() + ' ' + text;
			}
			return text;
		}

		// 解析包裹于圆括号内部的内容
		static void ParseGroup(string text, List<KeyPress> keyList)
		{
			while (text.Length > 0)
			{
				int length = 0;
				string value = null;
				Match match = RegexCurly.Match(text);
				if (match.Success)
				{
					length = match.Length;
					value = match.Groups[1].Value;
				}
				else
				{
					length = 1;
					value = text.Substring(0, 1);
				}

				keyList.Add(new KeyPress(value));
				text = text.Substring(length);
			}
		}

		// 判断表示组合键的特殊字符
		static Keys IsModifier(char ch)
		{
			Keys result;
			switch (ch)
			{
				case '+':
					result = Keys.Shift;
					break;

				case '^':
					result = Keys.Control;
					break;

				case '%':
					result = Keys.Alt;
					break;

				default:
					result = Keys.None;
					break;
			}

			return result;
		}
	}
	#endregion
}
