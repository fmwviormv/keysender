using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class Program : Form
{
	const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x0100;
	delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool UnhookWindowsHookEx(IntPtr hhk);
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern IntPtr GetModuleHandle(string lpModuleName);

	static IntPtr hookid = IntPtr.Zero;
	static Program program;
	static readonly Keys[] keys = {Keys.E, Keys.Space};
	static List<byte> key_scans = new List<byte>();

	static IntPtr KeyboardHandler(int nCode, IntPtr wParam, IntPtr lParam)
	{
		int ind = key_scans.Count;
		if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN &&
		    Marshal.ReadInt32(lParam) == (int)keys[ind])
		{
			if (ind >= keys.Length - 1 && hookid != IntPtr.Zero)
			{
				UnhookWindowsHookEx(hookid);
				hookid = IntPtr.Zero;
			}
			int code = Marshal.ReadInt32(lParam, 4);
			program.ScanCode(ind, code);
		}
		return CallNextHookEx(hookid, nCode, wParam, lParam);
	}

	static void SendKey(int key, bool up)
	{
		keybd_event((byte)keys[key], key_scans[key], up ? 2 : 0, 0);
	}

	[STAThread]
	static void Main(string[] args)
	{
		//Application.SetHighDpiMode(HighDpiMode.SystemAware);
		Application.EnableVisualStyles();
		program = new Program();
		using (var proc = Process.GetCurrentProcess())
		using (var mod = proc.MainModule)
		{
			hookid = SetWindowsHookEx(WH_KEYBOARD_LL,
			    new KeyboardProc(KeyboardHandler),
			    GetModuleHandle(mod.ModuleName),
			    0);
		}
		Application.Run(program);
		if (hookid != IntPtr.Zero)
			UnhookWindowsHookEx(hookid);
		hookid = IntPtr.Zero;
	}

	Label prompt;

	public Program()
	{
		this.FormBorderStyle = FormBorderStyle.Fixed3D;
		MaximizeBox = false;
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(800, 450);
		Text = "Key Sender";
		prompt = new Label();
		prompt.Location = new Point(80, 80);
		prompt.Size = new Size(640, 80);
		prompt.Font = new Font(prompt.Font.Name, 60);
		prompt.Text = string.Format("Press {0}", keys[0]);
		Controls.Add(prompt);
		key_scans = new List<byte>();
	}

	void ScanCode(int ind, int scanCode)
	{
		Label label = new Label();
		label.Location = new Point(80, 200 + 20 * ind);
		label.Size = new Size(640, 20);
		label.Text = string.Format("{0}: {1}", keys[ind], scanCode);
		Controls.Add(label);
		key_scans.Add((byte)scanCode);
		if (++ind < keys.Length)
			prompt.Text = string.Format("Press {0}", keys[ind]);
		else
			Start();
	}

	void Start()
	{
		var timer = new System.Windows.Forms.Timer();
		timer.Interval = 1000;
		int count = 5;
		prompt.Text = string.Format("Start in {0}...", count);
		timer.Tick += new EventHandler(delegate(object obj, EventArgs args)
		{
			if (--count > 0)
				prompt.Text = string.Format("Start in {0}...", count);
			else
			{
				timer.Stop();
				timer.Dispose();
				SendE();
			}
		});
		timer.Start();
	}

	void SendE()
	{
		var timer = new System.Windows.Forms.Timer();
		timer.Interval = 100;
		bool up = true;
		timer.Tick += new EventHandler(delegate(object obj, EventArgs args)
		{
			SendKey(0, up = !up);
			if (up)
			{
				timer.Stop();
				timer.Dispose();
				SendJump();
			}
		});
		timer.Start();
	}

	void SendJump()
	{
		var timer = new System.Windows.Forms.Timer();
		var watch = Stopwatch.StartNew();
		timer.Interval = 50;
		bool up = true;
		int count = 34;
		var next = Stopwatch.Frequency;
		prompt.Text = string.Format("Send Jump {0}...", count);
		timer.Tick += new EventHandler(delegate(object obj, EventArgs args)
		{
			if (watch.ElapsedTicks >= next && --count > 0)
			{
				next += Stopwatch.Frequency;
				prompt.Text = string.Format("Send Jump {0}...", count);
			}
			SendKey(1, up = !up);
			if (up && count <= 0)
			{
				timer.Stop();
				timer.Dispose();
				Wait();
			}
		});
		timer.Start();
	}

	void Wait()
	{
		var timer = new System.Windows.Forms.Timer();
		timer.Interval = 1000;
		int count = 19;
		prompt.Text = string.Format("Wait {0}...", count);
		timer.Tick += new EventHandler(delegate(object obj, EventArgs args)
		{
			if (--count > 0)
				prompt.Text = string.Format("Wait {0}...", count);
			else
			{
				timer.Stop();
				timer.Dispose();
				SendE();
			}
		});
		timer.Start();
	}
}
