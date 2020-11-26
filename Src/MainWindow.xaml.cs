using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Timer = System.Timers.Timer;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace HotMouse_2020
{
    public partial class MainWindow : Window
    {
        //Globals
        public int row = 1;
        private int charmode = 0;
        public static bool ReturnCursor = false;
        public static bool Repeating = false;
        public static int x;
        public static int y;
        public static int rx;
        public static int ry;
        public static int hangTime = 1;
        public static int repeatTime = 1;
        public static int holdCounter = 0;
        public static string _x;
        public static string _y;
        public static string _rx;
        public static string _ry;
        public static string keystring;
        public static string ammend = "   ";
        private Listener _listener;
        private static Point returnpos = new Point();
        public static Timer ClickLoopTimer = new Timer();
        public static Timer KeyRepeatTimer = new Timer();
        private static readonly Timer ClickStart = new Timer();
        private static readonly Timer ClickStop = new Timer();
        public static List<Key> LastKeys = new List<Key>();
        public Dictionary<int, Button> Toggles = new Dictionary<int, Button>();
        public Dictionary<int, List<TextBox>> rowBoxes = new Dictionary<int, List<TextBox>>();
        public List<TextBox> ConfigBoxes = new List<TextBox>();

        internal struct Win32Point
        {
            public int X;
            public int Y;
        }

        public MainWindow() 
        {
            InitializeComponent();
            ClickLoopTimer.Enabled = false;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this._listener = new Listener();
            this._listener.OnKeyPressed += new EventHandler<KeyPressedArgs>(this.OnKeyPressed);
            this._listener.HookKeyboard();
            var boxes = new List<TextBox>();
            var counter = new Double();
            counter = 1;
            foreach (Button Toggle in FindVisualChildren<Button>(this))
            {
                if (Toggle.Content.ToString() == "OFF")
                {
                    this.Toggles.Add(Convert.ToInt32(counter), Toggle);
                    counter++;
                }
            }
            counter = 1;
            foreach (TextBox tb in FindVisualChildren<TextBox>(this))
            {
                boxes.Add(tb);
                var underRow = counter / 3;
                var thisRow = Math.Ceiling(underRow);
                if (counter == 33)
                {
                    this.ConfigBoxes.Add(boxes[1]);
                    this.ConfigBoxes.Add(boxes[2]);
                }
                else if (((underRow % 1) == 0))
                {
                    this.rowBoxes.Add(Convert.ToInt32(thisRow), boxes);
                    boxes = new List<TextBox>();
                }
                counter++;
            }
            this._row = 1;
        }

        private string ValidateKeyString (string keystring)
        {
            return keystring.Replace("Oem", "").Replace("Space", "_")
                            .Replace("Left", "").Replace("Right", "")
                            .Replace("D1", "1").Replace("D2", "2")
                            .Replace("D3", "3").Replace("D4", "4")
                            .Replace("D5", "5").Replace("D6", "6")
                            .Replace("D7", "7").Replace("D8", "8")
                            .Replace("D9", "9").Replace("D0", "0")
                            .Replace("Question", "/").Replace("Comma", ",")
                            .Replace("Period", ".").Replace("Minus", "-")
                            .Replace("Plus", "=").Replace("OpenBrackets", "[")
                            .Replace("Quotes", "'").Replace("Shift", "^")
                            .Replace("Ctrl", "#").Replace("Alt", "*")
                            .Replace("Tab", ">");
        }

        private void OnKeyPressed(object sender, KeyPressedArgs e)
        {
            MainWindow.keystring += e.KeyPressed.ToString();
            MainWindow.ammend = ValidateKeyString(MainWindow.keystring);

            if (MainWindow.keystring.Length > 700)
            {
                MainWindow.keystring = MainWindow.keystring.Substring(MainWindow.keystring.Length - 700);
            }

            MainWindow._rx = MainWindow.GetMousePosition().X.ToString();
            MainWindow._ry = MainWindow.GetMousePosition().Y.ToString();


            if (!Repeating)
            {
                switch (this.charmode)
                {
                    case 0:
                        LastKeys.Clear();
                        LastKeys.Add(e.KeyPressed);
                        this.KeyBox.Text = ValidateKeyString(LastKeys[0].ToString());
                        break;
                    case 1:
                        LastKeys.Add(e.KeyPressed);
                        while (LastKeys.Count() > 2)
                        {
                            LastKeys.RemoveAt(0);
                        }
                        this.KeyBox.Text = ValidateKeyString(LastKeys[0].ToString() + LastKeys[1].ToString());
                        break;
                    case 2:
                        LastKeys.Add(e.KeyPressed);
                        while (LastKeys.Count() > 3)
                        {
                            LastKeys.RemoveAt(0);
                        }
                        this.KeyBox.Text = ValidateKeyString(LastKeys[0].ToString() + LastKeys[1].ToString() + LastKeys[2].ToString());
                        break;
                }

            }


            foreach (KeyValuePair<int, List<TextBox>> TextBoxRow in this.rowBoxes)
            {
                if (Toggles.Keys.Contains(TextBoxRow.Key)) {
                    if (Toggles[TextBoxRow.Key].Content.ToString() == "ON" &&
                        this.KeyBox.Text == TextBoxRow.Value[1].Text)
                    {
                        MainWindow.ReturnCursor = false;
                        var boxTextArray = TextBoxRow.Value.First().Text.Split(',');
                        var active_x = int.Parse(((IEnumerable<string>)boxTextArray).First<string>());
                        var active_y = int.Parse(((IEnumerable<string>)boxTextArray).Last<string>());
                        var startTime = DateTime.Now;
                        MainWindow.ClickStart.Elapsed += MainWindow.OnClickStartEvent;
                        MainWindow.ClickStart.Interval = 1;
                        MainWindow.ClickStop.Elapsed += MainWindow.OnClickStopEvent;
                        MainWindow.ClickStop.Interval = 2;
                        MainWindow.SetCursorPos(active_x, active_y);
                        MainWindow.mouse_event(2, active_x, active_y, 0, 0);
                        MainWindow.mouse_event(4, active_x, active_y, 0, 0);
                        MainWindow.ClickStart.Enabled = true;
                        while ((DateTime.Now - startTime).TotalMilliseconds < hangTime)
                        {
                            //Do Nothing;
                        }
                        MainWindow.ReturnCursor = true;
                    }
                }
            }
        }

        private static void OnClickStartEvent(object source, ElapsedEventArgs e)
        {
            if (MainWindow.ReturnCursor)
            {
                MainWindow.GetMousePosition();
                if (MainWindow.returnpos.X >= (double)(MainWindow.x - 15) && MainWindow.returnpos.X <= (double)(MainWindow.x + 15) && MainWindow.returnpos.Y >= (double)(MainWindow.y - 15) && MainWindow.returnpos.Y <= (double)(MainWindow.y + 15))
                {
                    MainWindow.rx = int.Parse(MainWindow._rx);
                    MainWindow.ry = int.Parse(MainWindow._ry);
                    MainWindow.SetCursorPos(MainWindow.rx, MainWindow.ry);
                    MainWindow.ClickStart.Stop();
                    MainWindow.ClickStart.Enabled = false;
                }
                else
                {
                    MainWindow.ClickStart.Stop();
                    MainWindow.ClickStart.Enabled = false;
                }
            }
        }

        private static void OnClickStopEvent(object source, ElapsedEventArgs e)
        {
            MainWindow.ClickStart.Stop();
        }

        private void ModifyTimer(object sender, MouseButtonEventArgs e)
        {
            List<String> RepeatNames = new List<String>();
            RepeatNames.Add("RepeatUp");
            RepeatNames.Add("RepeatDown");
            MainWindow.holdCounter = 0;
            Button TheButton = ((Button)sender);
            TextBox TheBox = RepeatNames.Contains(TheButton.Name.ToString()) ? this.RepeatBox : this.DurationBox;
            String Symbol = TheButton.Content.ToString();
            String TimeUnit = TheBox == this.RepeatBox ? "s" : "ms";
            IncrementTimer(Symbol, TheBox, TimeUnit);
            ClickLoopTimer.Enabled = true;
            ClickLoopTimer.Elapsed += (s, ev) => IncrementTimer(Symbol, TheBox, TimeUnit);
            ClickLoopTimer.Interval = 250;
            ClickLoopTimer.AutoReset = true;
        }

        private void IncrementTimer(String Symbol, TextBox TheBox, String TimeUnit)
        {
            int boxValue = TheBox == this.RepeatBox ? MainWindow.repeatTime : MainWindow.hangTime;
            MainWindow.holdCounter++;
            if ((Symbol == ">") && (boxValue < 999))
            {
                boxValue++;
            }
            else if ((Symbol == "<") && (boxValue > 1))
            {
                boxValue--;
            }
            this.Dispatcher.Invoke(() =>
            {
                TheBox.Text = (boxValue.ToString() + TimeUnit);
            });
            if (holdCounter > 2 && holdCounter < 10)
            {
                ClickLoopTimer.Interval = 50;
            }
            else if (holdCounter >= 10 && holdCounter < 50)
            {
                ClickLoopTimer.Interval = 10;
            }
            if (TheBox == this.RepeatBox)
            {
                MainWindow.repeatTime = boxValue;
                KeyRepeatTimer.Interval = MainWindow.repeatTime;
            }
            else
            {
                MainWindow.hangTime = boxValue;
            }
        }

        private void StopChangingTimer(object sender, MouseButtonEventArgs e)
        {
            MainWindow.ClickLoopTimer.Dispose();
            MainWindow.ClickLoopTimer = new Timer();
            ClickLoopTimer.Enabled = false;
        }

        public int _row
        {
            get
            {
                return this.row;
            }
            set
            {
                this.row = value;
                foreach (KeyValuePair<int, List<TextBox>> the_row in this.rowBoxes)
                {
                    if (the_row.Key != value)
                    {
                        foreach (TextBox box in the_row.Value)
                        {
                            box.Background = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
                            box.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
                        }
                    }
                    else
                    {
                        foreach (TextBox box in the_row.Value)
                        {
                            box.Background = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
                            box.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
                        }
                    }
                }
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            this._listener.UnHookKeyboard();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "HotMouse Profiles (*.hmp)|*.hmp";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (StreamWriter file = new StreamWriter(sfd.FileName))
                {
                    file.Write("{\"Rows\":[");
                    int counter = 0;
                    foreach (KeyValuePair<int, List<TextBox>> item in this.rowBoxes)
                    {
                        counter++;
                        file.Write("[");
                        foreach (TextBox box in item.Value)
                        {
                            file.Write("\"" + box.Text + "\",");
                        }
                        String FinalDelim = counter == this.rowBoxes.Count ? "]]," : "],";
                        file.Write("\"" + Toggles[item.Key].Content + "\"");
                        file.Write(FinalDelim);
                    }
                    file.Write("\"Config\":[[");
                    foreach (TextBox box in ConfigBoxes)
                    {
                        if (box == ConfigBoxes[1])
                        {
                            file.Write("\"" + box.Text + "\"]]}");
                        }
                        else
                        {
                            file.Write("\"" + box.Text + "\",");
                        }
                    }
                }
            }
        }

        private void LoadButton(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "HotMouse Profiles (*.hmp)|*.hmp";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadFile(ofd.FileName);
            }
        }

        public void LoadFile (string fileName)
        {
            string allText = System.IO.File.ReadAllText(fileName);
            Dictionary<String, List<List<String>>> AllContent = JsonSerializer.Deserialize<Dictionary<String, List<List<String>>>>(allText);
            List<List<String>> Rows = AllContent["Rows"];
            List<String> Config = AllContent["Config"][0];

            foreach (KeyValuePair<int, List<TextBox>> item in this.rowBoxes)
            {
                List<String> row = Rows[item.Key - 1]; 
                for (var j = 0; j<item.Value.Count(); j++)
                {
                    item.Value[j].Text = row[j];
                }
            }
            foreach (KeyValuePair<int, Button> item in this.Toggles)
            {
                List<String> row = Rows[item.Key - 1];
                if (row[3] == "ON")
                {
                    item.Value.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
            int counter = 0;
            foreach (TextBox box in ConfigBoxes)
            {
                box.Text = Config[counter];
                switch (counter)
                {
                    case 0:
                        MainWindow.repeatTime = Int32.Parse(Config[counter].Replace("s", ""));
                        break;
                    case 1:
                        MainWindow.hangTime = Int32.Parse(Config[counter].Replace("ms", ""));
                        break;
                }
                counter += 1;
            }

        }

        private void ClearRow(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<int, List<TextBox>> TextBoxRow in this.rowBoxes)
            {
                if (TextBoxRow.Key == this.row)
                {
                    for (int i = 0; i < TextBoxRow.Value.Count(); i++)
                    {
                        if (i == 0)
                        {
                            TextBoxRow.Value[i].Text = "0, 0";
                        }
                        else
                        {
                            TextBoxRow.Value[i].Text = (string)null;
                        }
                    }
                }
            }
        }

        private void KeyCap(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<int, List<TextBox>> TextBoxRow in this.rowBoxes)
            {
                if (TextBoxRow.Key == this.row)
                {
                    TextBoxRow.Value[1].Text = this.KeyBox.Text;
                }
            }
        }

        private void MouseCap(object sender, MouseEventArgs e)
        {
            MainWindow._x = MainWindow.GetMousePosition().X.ToString();
            MainWindow._y = MainWindow.GetMousePosition().Y.ToString();
            string[] strArray = new string[2]
            {
                MainWindow._x,
                MainWindow._y
            };
            string separator = ", ";
            foreach (KeyValuePair<int, List<TextBox>> TextBoxRow in this.rowBoxes)
            {
                if (TextBoxRow.Key == this.row)
                {
                    TextBoxRow.Value[0].Text = string.Join(separator, strArray);
                }
            }
        }

        public static Point GetMousePosition()
        {
            MainWindow.Win32Point pt = new MainWindow.Win32Point();
            MainWindow.GetCursorPos(ref pt);
            return new Point((double)pt.X, (double)pt.Y);
        }

        private void CharMode(object sender, RoutedEventArgs e)
        {
            switch (this.charmode)
            {
                case 0:
                    this.charmode = 1;
                    this.label3.Content = (object)"DOUBLE KEY";
                    this.charmodebtn.Content = (object)"Use 3 Keys";
                    this.keycap_btn.Content = (object)"Capture Keys";
                    break;
                case 1:
                    this.charmode = 2;
                    this.label3.Content = (object)"TRIPLE KEY";
                    this.charmodebtn.Content = (object)"Use 1 Key";
                    this.keycap_btn.Content = (object)"Capture Keys";
                    break;
                case 2:
                    this.charmode = 0;
                    this.label3.Content = (object)"SINGLE KEY";
                    this.charmodebtn.Content = (object)"Use 2 Keys";
                    this.keycap_btn.Content = (object)"Capture Key";
                    break;
            }
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Button TheButton = ((Button)sender);
            if (TheButton.Content.ToString() == "OFF")
            {
                TheButton.Content = (object)"ON";
                TheButton.Background = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
                TheButton.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
            }
            else
            {
                TheButton.Content = (object)"OFF";
                TheButton.Background = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
                TheButton.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
            }
        }

        private void RowClick(object sender, RoutedEventArgs e)
        {
            this._row = Grid.GetRow((TextBox)sender) - 2;
        }

        private void RepeatClick(object sender, RoutedEventArgs e)
        {
            Repeating = !Repeating;
            Button RepeatButton = (Button)sender;
            if (Repeating)
            {
                RepeatButton.Background = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
                RepeatButton.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
                KeyRepeatTimer.Enabled = true;
                KeyRepeatTimer.Elapsed += (s, ev) => RepeatKeys();
                KeyRepeatTimer.Interval = repeatTime*1000;
                KeyRepeatTimer.AutoReset = true;
            }
            else
            {
                RepeatButton.Background = (Brush)(new BrushConverter().ConvertFrom("#FF232323"));
                RepeatButton.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF7D7D7D"));
                KeyRepeatTimer.Dispose();
                MainWindow.KeyRepeatTimer = new Timer();
                KeyRepeatTimer.Enabled = false;
            }
        }

        private void RepeatKeys()
        {
            foreach (Key PressedKey in LastKeys)
            {
                keybd_event((byte)KeyInterop.VirtualKeyFromKey(PressedKey), 0, 0x0001 | 0, 0); 
                keybd_event((byte)KeyInterop.VirtualKeyFromKey(PressedKey), 0, 0x0002 | 0, 0);
            }

        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref MainWindow.Win32Point pt);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(
          int dwFlags,
          int dx,
          int dy,
          int cButtons,
          int dwExtraInfo);
    }
}
