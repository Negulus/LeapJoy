using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Leap;
using vJoyInterfaceWrap;

namespace LeapJoy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILeapEventDelegate
    {
        private Controller Leap_Controller;
        private LeapEventListener Leap_Listener;        
        private Boolean isClosing = false;

        static public vJoy vJoy_Joystick;
        static public uint vJoy_id = 1;

        DataSet Data_Set;

        Brush Trig_En;
        Brush Trig_Dis;

        bool rdy = false;
        
        public MainWindow()
        {
            InitializeComponent();
            this.Leap_Controller = new Controller();
            this.Leap_Listener = new LeapEventListener(this);
            Leap_Controller.AddListener(Leap_Listener);
            Leap_Controller.SetPolicyFlags(Controller.PolicyFlag.POLICYBACKGROUNDFRAMES);

            vJoy_Joystick = new vJoy();
            if (!vJoy_Joystick.vJoyEnabled()) return;
            UInt32 DllVer = 0, DrvVer = 0;
            vJoy_Joystick.DriverMatch(ref DllVer, ref DrvVer);
            vJoy_Joystick.AcquireVJD(vJoy_id);
            vJoy_Joystick.ResetVJD(vJoy_id);

            Trig_En = new SolidColorBrush(Colors.LightGreen);
            Trig_Dis = Text_Trig_Max_Up.Background;

            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("LeapJoySets.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                Data_Set = (DataSet)formatter.Deserialize(stream);
                stream.Close();
            }
            catch
            {
                Data_Set = new DataSet();
            }

            Text_Max_Up.Text = Data_Set.Axis_Max_Up.ToString();
            Text_Max_Pitch.Text = Data_Set.Axis_Max_Pitch.ToString();
            Text_Max_Roll.Text = Data_Set.Axis_Max_Roll.ToString();
            Text_Max_Yaw.Text = Data_Set.Axis_Max_Yaw.ToString();

            Text_Min_Up.Text = Data_Set.Axis_Min_Up.ToString();
            Text_Min_Pitch.Text = Data_Set.Axis_Min_Pitch.ToString();
            Text_Min_Roll.Text = Data_Set.Axis_Min_Roll.ToString();
            Text_Min_Yaw.Text = Data_Set.Axis_Min_Yaw.ToString();

            Text_Trig_Max_Up.Text = Data_Set.Axis_Trig_Max_Up.ToString();
            Text_Trig_Max_Pitch.Text = Data_Set.Axis_Trig_Max_Pitch.ToString();
            Text_Trig_Max_Roll.Text = Data_Set.Axis_Trig_Max_Roll.ToString();
            Text_Trig_Max_Yaw.Text = Data_Set.Axis_Trig_Max_Yaw.ToString();

            Text_Trig_Min_Up.Text = Data_Set.Axis_Trig_Min_Up.ToString();
            Text_Trig_Min_Pitch.Text = Data_Set.Axis_Trig_Min_Pitch.ToString();
            Text_Trig_Min_Roll.Text = Data_Set.Axis_Trig_Min_Roll.ToString();
            Text_Trig_Min_Yaw.Text = Data_Set.Axis_Trig_Min_Yaw.ToString();

            rdy = true;
        }

        delegate void LeapEventDelegate(string EventName);
        public void LeapEventNotification(string EventName)
        {
            if (this.CheckAccess())
            {
                switch (EventName)
                {
                    case "onInit":
//                        Debug.WriteLine("Init");
                        break;
                    case "onConnect":
                        this.connectHandler();
                        break;
                    case "onFrame":
                        if(!this.isClosing)
                            this.newFrameHandler(this.Leap_Controller.Frame());
                        break;
                }
            }
            else
            {
                Dispatcher.Invoke(new LeapEventDelegate(LeapEventNotification), new object[] { EventName });
            }
        }

        void connectHandler()
        {
            this.Leap_Controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            this.Leap_Controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            this.Leap_Controller.Config.SetFloat("Gesture.Swipe.MinLength", 100.0f);
        }

        void newFrameHandler(Leap.Frame frame)
        {
            //Установка осей и кнопок в 0 при отсутствии руки
            if (!frame.Hands[0].IsValid)
            {
                Rect_Up.Height = 150f * ((float)0.5f);
                Rect_Pitch.Height = 150f * ((float)0.5f);
                Rect_Roll.Height = 150f * ((float)0.5f);
                Rect_Yaw.Height = 150f * ((float)0.5f);

                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_X);
                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_Y);
                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_Z);
                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_RX);
                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_RY);
                vJoy_Joystick.SetAxis(16384, vJoy_id, HID_USAGES.HID_USAGE_RZ);
                vJoy_Joystick.SetBtn(false, vJoy_id, 5);
                vJoy_Joystick.SetBtn(false, vJoy_id, 6);
                return;
            }

            Data_Set.Axis_Raw_Up = frame.Hands[0].PalmPosition.y;
            Data_Set.Axis_Raw_Pitch = (float)Math.Acos((frame.Hands[0].WristPosition.y - frame.Hands[0].PalmPosition.y) / frame.Hands[0].PalmWidth) - 1;
            Data_Set.Axis_Raw_Roll = (float)Math.Acos((frame.Hands[0].Fingers[1].StabilizedTipPosition.y - frame.Hands[0].Fingers[4].StabilizedTipPosition.y) / frame.Hands[0].PalmWidth) - 1;
            Data_Set.Axis_Raw_Yaw = (float)Math.Acos((frame.Hands[0].WristPosition.x - frame.Hands[0].PalmPosition.x) / frame.Hands[0].PalmWidth) - 1;

            if (Data_Set.Axis_Max_Up == Data_Set.Axis_Min_Up) Data_Set.Axis_Max_Up += 0.001f;
            Data_Set.Axis_Joy_Up = (int)(1000 * (Data_Set.Axis_Raw_Up - Data_Set.Axis_Min_Up) / (Data_Set.Axis_Max_Up - Data_Set.Axis_Min_Up));
            if (Data_Set.Axis_Joy_Up > 1000) Data_Set.Axis_Joy_Up = 1000; else if (Data_Set.Axis_Joy_Up < 0) Data_Set.Axis_Joy_Up = 0;

            if (Data_Set.Axis_Max_Pitch == Data_Set.Axis_Min_Pitch) Data_Set.Axis_Max_Pitch += 0.001f;
            Data_Set.Axis_Joy_Pitch = (int)(1000 * (Data_Set.Axis_Raw_Pitch - Data_Set.Axis_Min_Pitch) / (Data_Set.Axis_Max_Pitch - Data_Set.Axis_Min_Pitch));
            if (Data_Set.Axis_Joy_Pitch > 1000) Data_Set.Axis_Joy_Pitch = 1000; else if (Data_Set.Axis_Joy_Pitch < 0) Data_Set.Axis_Joy_Pitch = 0;

            if (Data_Set.Axis_Max_Roll == Data_Set.Axis_Min_Roll) Data_Set.Axis_Max_Roll += 0.001f;
            Data_Set.Axis_Joy_Roll = (int)(1000 * (Data_Set.Axis_Raw_Roll - Data_Set.Axis_Min_Roll) / (Data_Set.Axis_Max_Roll - Data_Set.Axis_Min_Roll));
            if (Data_Set.Axis_Joy_Roll > 1000) Data_Set.Axis_Joy_Roll = 1000; else if (Data_Set.Axis_Joy_Roll < 0) Data_Set.Axis_Joy_Roll = 0;

            if (Data_Set.Axis_Max_Yaw == Data_Set.Axis_Min_Yaw) Data_Set.Axis_Max_Yaw += 0.001f;
            Data_Set.Axis_Joy_Yaw = (int)(1000 * (Data_Set.Axis_Raw_Yaw - Data_Set.Axis_Min_Yaw) / (Data_Set.Axis_Max_Yaw - Data_Set.Axis_Min_Yaw));
            if (Data_Set.Axis_Joy_Yaw > 1000) Data_Set.Axis_Joy_Yaw = 1000; else if (Data_Set.Axis_Joy_Yaw < 0) Data_Set.Axis_Joy_Yaw = 0;

            Text_Raw_Up.Text = Data_Set.Axis_Raw_Up.ToString();
            Text_Raw_Pitch.Text = Data_Set.Axis_Raw_Pitch.ToString();
            Text_Raw_Roll.Text = Data_Set.Axis_Raw_Roll.ToString();
            Text_Raw_Yaw.Text = Data_Set.Axis_Raw_Yaw.ToString();

            Rect_Up.Height = 150f * ((float)Data_Set.Axis_Joy_Up / 1000f);
            Rect_Pitch.Height = 150f * ((float)Data_Set.Axis_Joy_Pitch / 1000f);
            Rect_Roll.Height = 150f * ((float)Data_Set.Axis_Joy_Roll / 1000f);
            Rect_Yaw.Height = 150f * ((float)Data_Set.Axis_Joy_Yaw / 1000f);

            Text_Trig_Max_Up.Background = (Data_Set.Axis_Joy_Up >= Data_Set.Axis_Trig_Max_Up) ? Trig_En : Trig_Dis;
            Text_Trig_Max_Pitch.Background = (Data_Set.Axis_Joy_Pitch >= Data_Set.Axis_Trig_Max_Pitch) ? Trig_En : Trig_Dis;
            Text_Trig_Max_Roll.Background = (Data_Set.Axis_Joy_Roll >= Data_Set.Axis_Trig_Max_Roll) ? Trig_En : Trig_Dis;
            Text_Trig_Max_Yaw.Background = (Data_Set.Axis_Joy_Yaw >= Data_Set.Axis_Trig_Max_Yaw) ? Trig_En : Trig_Dis;

            Text_Trig_Min_Up.Background = (Data_Set.Axis_Joy_Up <= Data_Set.Axis_Trig_Min_Up) ? Trig_En : Trig_Dis;
            Text_Trig_Min_Pitch.Background = (Data_Set.Axis_Joy_Pitch <= Data_Set.Axis_Trig_Min_Pitch) ? Trig_En : Trig_Dis;
            Text_Trig_Min_Roll.Background = (Data_Set.Axis_Joy_Roll <= Data_Set.Axis_Trig_Min_Roll) ? Trig_En : Trig_Dis;
            Text_Trig_Min_Yaw.Background = (Data_Set.Axis_Joy_Yaw <= Data_Set.Axis_Trig_Min_Yaw) ? Trig_En : Trig_Dis;

            vJoy_Joystick.SetAxis((int)(Data_Set.Axis_Joy_Roll * 32.768f), vJoy_id, HID_USAGES.HID_USAGE_X);
            vJoy_Joystick.SetAxis((int)((1000 - Data_Set.Axis_Joy_Pitch) * 32.768f), vJoy_id, HID_USAGES.HID_USAGE_Y);
            vJoy_Joystick.SetAxis((int)((1000 - Data_Set.Axis_Joy_Up) * 32.768f), vJoy_id, HID_USAGES.HID_USAGE_Z);
            vJoy_Joystick.SetBtn((Data_Set.Axis_Joy_Yaw < Data_Set.Axis_Trig_Min_Yaw), vJoy_id, 5);
            vJoy_Joystick.SetBtn((Data_Set.Axis_Joy_Yaw > Data_Set.Axis_Trig_Max_Yaw), vJoy_id, 6);
        }

        void MainWindow_Closing(object sender, EventArgs e)
        {
            this.isClosing = true;
            this.Leap_Controller.RemoveListener(this.Leap_Listener);
            this.Leap_Controller.Dispose();
        }

        private void Button_Max_Up_Click(object sender, RoutedEventArgs e)
        {
            Text_Max_Up.Text = Data_Set.Axis_Raw_Up.ToString();
        }

        private void Button_Max_Pitch_Click(object sender, RoutedEventArgs e)
        {
            Text_Max_Pitch.Text = Data_Set.Axis_Raw_Pitch.ToString();
        }

        private void Button_Max_Roll_Click(object sender, RoutedEventArgs e)
        {
            Text_Max_Roll.Text = Data_Set.Axis_Raw_Roll.ToString();
        }

        private void Button_Max_Yaw_Click(object sender, RoutedEventArgs e)
        {
            Text_Max_Yaw.Text = Data_Set.Axis_Raw_Yaw.ToString();
        }

        private void Button_Min_Up_Click(object sender, RoutedEventArgs e)
        {
            Text_Min_Up.Text = Data_Set.Axis_Raw_Up.ToString();
        }

        private void Button_Min_Pitch_Click(object sender, RoutedEventArgs e)
        {
            Text_Min_Pitch.Text = Data_Set.Axis_Raw_Pitch.ToString();
        }

        private void Button_Min_Roll_Click(object sender, RoutedEventArgs e)
        {
            Text_Min_Roll.Text = Data_Set.Axis_Raw_Roll.ToString();
        }

        private void Button_Min_Yaw_Click(object sender, RoutedEventArgs e)
        {
            Text_Min_Yaw.Text = Data_Set.Axis_Raw_Yaw.ToString();
        }

        private void Button_Trig_Max_Up_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Max_Up.Text = Data_Set.Axis_Joy_Up.ToString();
        }

        private void Button_Trig_Max_Pitch_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Max_Pitch.Text = Data_Set.Axis_Joy_Pitch.ToString();
        }

        private void Button_Trig_Max_Roll_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Max_Roll.Text = Data_Set.Axis_Joy_Roll.ToString();
        }

        private void Button_Trig_Max_Yaw_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Max_Yaw.Text = Data_Set.Axis_Joy_Yaw.ToString();
        }

        private void Button_Trig_Min_Up_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Min_Up.Text = Data_Set.Axis_Joy_Up.ToString();
        }

        private void Button_Trig_Min_Pitch_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Min_Pitch.Text = Data_Set.Axis_Joy_Pitch.ToString();
        }

        private void Button_Trig_Min_Roll_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Min_Roll.Text = Data_Set.Axis_Joy_Roll.ToString();
        }

        private void Button_Trig_Min_Yaw_Click(object sender, RoutedEventArgs e)
        {
            Text_Trig_Min_Yaw.Text = Data_Set.Axis_Joy_Yaw.ToString();
        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!rdy) return;

            float.TryParse(Text_Max_Up.Text, out Data_Set.Axis_Max_Up);
            float.TryParse(Text_Max_Pitch.Text, out Data_Set.Axis_Max_Pitch);
            float.TryParse(Text_Max_Roll.Text, out Data_Set.Axis_Max_Roll);
            float.TryParse(Text_Max_Yaw.Text, out Data_Set.Axis_Max_Yaw);

            float.TryParse(Text_Min_Up.Text, out Data_Set.Axis_Min_Up);
            float.TryParse(Text_Min_Pitch.Text, out Data_Set.Axis_Min_Pitch);
            float.TryParse(Text_Min_Roll.Text, out Data_Set.Axis_Min_Roll);
            float.TryParse(Text_Min_Yaw.Text, out Data_Set.Axis_Min_Yaw);

            int.TryParse(Text_Trig_Max_Up.Text, out Data_Set.Axis_Trig_Max_Up);
            int.TryParse(Text_Trig_Max_Pitch.Text, out Data_Set.Axis_Trig_Max_Pitch);
            int.TryParse(Text_Trig_Max_Roll.Text, out Data_Set.Axis_Trig_Max_Roll);
            int.TryParse(Text_Trig_Max_Yaw.Text, out Data_Set.Axis_Trig_Max_Yaw);

            int.TryParse(Text_Trig_Min_Up.Text, out Data_Set.Axis_Trig_Min_Up);
            int.TryParse(Text_Trig_Min_Pitch.Text, out Data_Set.Axis_Trig_Min_Pitch);
            int.TryParse(Text_Trig_Min_Roll.Text, out Data_Set.Axis_Trig_Min_Roll);
            int.TryParse(Text_Trig_Min_Yaw.Text, out Data_Set.Axis_Trig_Min_Yaw);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("LeapJoySets.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, Data_Set);
            stream.Close();
        }
    }

    public interface ILeapEventDelegate
    {
        void LeapEventNotification(string EventName);
    }

    public class LeapEventListener : Listener
    {
        ILeapEventDelegate eventDelegate;

        public LeapEventListener(ILeapEventDelegate delegateObject)
        {
            this.eventDelegate = delegateObject;
        }
        public override void OnInit(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onInit");
        }
        public override void OnConnect(Controller controller)
        {
            controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            this.eventDelegate.LeapEventNotification("onConnect");
        }

        public override void OnFrame(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onFrame");
        }
        public override void OnExit(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onExit");
        }
        public override void OnDisconnect(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onDisconnect");
        }

    }

    [Serializable]
    public class DataSet
    {
        public float Axis_Raw_Up;
        public float Axis_Raw_Pitch;
        public float Axis_Raw_Roll;
        public float Axis_Raw_Yaw;

        public float Axis_Min_Up;
        public float Axis_Min_Pitch;
        public float Axis_Min_Roll;
        public float Axis_Min_Yaw;

        public float Axis_Max_Up;
        public float Axis_Max_Pitch;
        public float Axis_Max_Roll;
        public float Axis_Max_Yaw;

        public int Axis_Joy_Up;
        public int Axis_Joy_Pitch;
        public int Axis_Joy_Roll;
        public int Axis_Joy_Yaw;

        public int Axis_Trig_Max_Up;
        public int Axis_Trig_Max_Pitch;
        public int Axis_Trig_Max_Roll;
        public int Axis_Trig_Max_Yaw;

        public int Axis_Trig_Min_Up;
        public int Axis_Trig_Min_Pitch;
        public int Axis_Trig_Min_Roll;
        public int Axis_Trig_Min_Yaw;
    }
}
