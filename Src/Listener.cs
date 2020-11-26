using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace HotMouse_2020
{
  public class Listener
  {
    private IntPtr _hookID = IntPtr.Zero;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 256;
    private const int WM_SYSKEYDOWN = 260;
    private Listener.LowLevelKeyboardProc _proc;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
      int idHook,
      Listener.LowLevelKeyboardProc lpfn,
      IntPtr hMod,
      uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
      IntPtr hhk,
      int nCode,
      IntPtr wParam,
      IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public event EventHandler<KeyPressedArgs> OnKeyPressed;

    public Listener()
    {
      this._proc = new Listener.LowLevelKeyboardProc(this.HookCallback);
    }

    public void HookKeyboard()
    {
      this._hookID = this.SetHook(this._proc);
    }

    public void UnHookKeyboard()
    {
      Listener.UnhookWindowsHookEx(this._hookID);
    }

    private IntPtr SetHook(Listener.LowLevelKeyboardProc proc)
    {
      using (Process currentProcess = Process.GetCurrentProcess())
      {
        using (ProcessModule mainModule = currentProcess.MainModule)
          return Listener.SetWindowsHookEx(13, proc, Listener.GetModuleHandle(mainModule.ModuleName), 0U);
      }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0 && wParam == (IntPtr) 256 || wParam == (IntPtr) 260)
      {
        int virtualKey = Marshal.ReadInt32(lParam);
        if (this.OnKeyPressed != null)
          this.OnKeyPressed((object) this, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(virtualKey)));
      }
      return Listener.CallNextHookEx(this._hookID, nCode, wParam, lParam);
    }

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
  }
}
