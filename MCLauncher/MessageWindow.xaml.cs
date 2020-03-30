using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MCLauncher
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWindow : Window
    {

        public Brush WindowBrush;
        public Brush m_BorderBrush;

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public void EnableBlur()
        {
            WindowInteropHelper windowHelper = new WindowInteropHelper(this);

            AccentPolicy accent = new AccentPolicy()
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var hGc = GCHandle.Alloc(accent, GCHandleType.Pinned);
            WindowCompositionAttributeData data = new WindowCompositionAttributeData()
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = Marshal.SizeOf(accent),
                Data = hGc.AddrOfPinnedObject()
            };
            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            hGc.Free();
        }

        internal void Show(string ContentText,string TitleText)
        {
            Title = TitleText;
            ContentLabel.Text = ContentText;
            TitleLabel.Content = TitleText;
            Show();
        }
           
        internal bool? ShowDialog(string ContentText, string TitleText)
        {
            Title = TitleText;
            ContentLabel.Text = ContentText;
            TitleLabel.Content = TitleText;
            return ShowDialog();
        }

        public MessageWindow()
        {
            InitializeComponent();
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

            Close();
        }

        private void MiniSizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
            WindowBrush = TitlePanel.Background;
            m_BorderBrush = BorderBrush;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (TitlePanel.Background == WindowBrush)
            {
                TitlePanel.Background = Brushes.Transparent;
                BorderBrush = Brushes.Gray;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (TitlePanel.Background == Brushes.Transparent)
            {
                TitlePanel.Background = WindowBrush;
                BorderBrush = m_BorderBrush;
            }
        }
    }
}
