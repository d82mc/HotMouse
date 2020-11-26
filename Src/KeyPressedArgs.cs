using System;
using System.Windows.Input;

namespace HotMouse_2020
{
  public class KeyPressedArgs : EventArgs
  {
    public Key KeyPressed { get; private set; }

    public KeyPressedArgs(Key key)
    {
      this.KeyPressed = key;
    }
  }
}
