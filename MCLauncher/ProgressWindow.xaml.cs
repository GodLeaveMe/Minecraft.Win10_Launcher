using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MCLauncher
{
    /// <summary>
    /// ProgressWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        internal void Show(string ContentText, string TitleText, int _Max = 100)
        {
            Title = TitleText;
            ContentLabel.Content = ContentText;
            TitleLabel.Content = TitleText;
            MainProgressBar.Maximum = _Max;
            Show();
        }

        internal bool? ShowDialog(string ContentText, string TitleText, Func<int> func, int _Max = 100)
        {
            func();
            Title = TitleText;
            ContentLabel.Content = ContentText;
            TitleLabel.Content = TitleText;
            MainProgressBar.Maximum = _Max;
            return ShowDialog();
        }

        internal void SetValue(int _Value)
        {
            MainProgressBar.Value = _Value;
        }

        internal void SetMaxValue(int MaxValue)
        {
            MainProgressBar.Maximum = MaxValue;
        }

        internal void SetContent(string ContentText)
        {
            ContentLabel.Content = ContentText;
        }

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
