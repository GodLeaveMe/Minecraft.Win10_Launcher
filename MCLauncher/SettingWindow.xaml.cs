using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MCLauncher
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
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

        public string DownloadDir;
        public bool DelAppx;

        internal void Show(string _DownloadDir)
        {
            AppDir = FixPathString(AppDomain.CurrentDomain.BaseDirectory);
            DownloadDir = _DownloadDir;
            if (DownloadDir == string.Empty)
            {
                DownloadPathBox.Text = AppDir;
            }
            else
            {
                DownloadPathBox.Text = DownloadDir;
            }
            DelAppx = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH)).DelAppx;
            Show();
            UserPrefs.DelAppx = DelAppx;
            UserPrefs.DownloadDir = DownloadPathBox.Text;
        }

        internal bool? ShowDialog(string _DownloadDir)
        {
            DownloadDir = _DownloadDir;
            DelAppx = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH)).DelAppx;
            return ShowDialog();
        }

        public Preferences UserPrefs { get; set; } = new Preferences();

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            if ((!(DownloadPathBox.Text == DownloadDir)) || (!(DelAppx == UserPrefs.DelAppx)))
            {
                if (!File.Exists(PREFS_PATH))
                {
                    File.Create(PREFS_PATH);
                    UserPrefs = new Preferences();
                    RewritePrefs();
                }
                UserPrefs = new Preferences()
                {
                    DownloadDir = DownloadPathBox.Text,
                    DelAppx = DelAppxBox.IsChecked ?? false,
                    ShowBetas = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH)).ShowBetas
                };
                RewritePrefs();
                MainWindow.ReloadPrefs();
                System.Windows.MessageBox.Show("用户配置已经保存", "提示");
                App.WriteLine("修改用户配置: DownloadDir --> \"" + UserPrefs.DownloadDir + "\"");
                App.WriteLine("修改用户配置: DelAppx --> " + UserPrefs.DelAppx);
            }
            Close();
        }

        private void RewritePrefs()
        {
            File.WriteAllText(PREFS_PATH, JsonConvert.SerializeObject(UserPrefs));
        }

        private static readonly string PREFS_PATH = @"preferences.json";

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiniSizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitlePanel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
            AppDir = FixPathString(AppDomain.CurrentDomain.BaseDirectory);
            if (DownloadDir == string.Empty)
            {
                DownloadPathBox.Text = AppDir;
                if (!(System.Windows.MessageBox.Show("本功能尚在实验阶段,可能会造成应用出现异常,您确定要继续吗?", "警告",MessageBoxButton.OKCancel,MessageBoxImage.Warning,MessageBoxResult.Cancel) == MessageBoxResult.OK))
                {
                    Close(); 
                }
            }
            else
            {
                DownloadPathBox.Text = DownloadDir;
            }
            DelAppxBox.IsChecked = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH)).DelAppx;
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

        public string AppDir;

        public string FixPathString(string m_String)
        {
            int lenth = m_String.ToCharArray().Length - 1;
            if (m_String.ToCharArray()[lenth].ToString() == "\\")
            {
                return m_String;
            }
            else
            {
                return m_String + "\\";
            }
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            FBD.Description = "选择一个目录来保存下载的文件";
            FBD.ShowNewFolderButton = true;
            if (DownloadPathBox.Text == AppDir)
            {
                FBD.SelectedPath = AppDir;
            }
            else
            {
                FBD.SelectedPath = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH)).DownloadDir;
            }
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DownloadPathBox.Text = FixPathString(FBD.SelectedPath);
            }
        }

        private void DelAppxBox_Checked(object sender, RoutedEventArgs e)
        {
            DelAppx = DelAppxBox.IsChecked ?? false;
        }
    }
}