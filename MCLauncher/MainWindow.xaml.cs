using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Management.Automation.Runspaces;

namespace MCLauncher {
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Windows.Foundation;
    using Windows.Management.Core;
    using Windows.Management.Deployment;
    using Windows.System;
    using WPFDataTypes;

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, ICommonVersionCommands {

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd,int Msg,IntPtr wParam,string lParam);

        [DllImport("User32.dll", EntryPoint = "ShowWindow")]
        private static extern bool ShowWindow(IntPtr hWnd,int Type);

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

        public class Powershell
        {
            public static bool AddAppxPackage(string PackagePath)
            {
                return Invoke("Add-AppxPackage -Verbose \"" + PackagePath + "\"");
            }

            public static bool RemoveAppxPackage(string PackageName)
            {
                return Invoke("Remove-AppxPackage -Verbose \"" + PackageName + "\"");
            }

            public static bool Invoke(string cmd)
            {
                try
                {
                    List<string> ps = new List<string>
                    {
                        "Set-ExecutionPolicy RemoteSigned",
                        "Set-ExecutionPolicy -ExecutionPolicy Unrestricted",
                        cmd
                    };
                    Runspace runspace = RunspaceFactory.CreateRunspace();
                    runspace.Open();
                    Pipeline pipeline = runspace.CreatePipeline();
                    foreach (var scr in ps)
                    {
                        pipeline.Commands.AddScript(scr);
                    }
                    var test = pipeline.Invoke();
                    if (pipeline.HadErrors || (pipeline.Error.Count > 0))
                    {
                        string ctf = "";
                        foreach (var item in runspace.Debugger.GetCallStack().ToArray())
                        {
                            ctf += item.ToString() + "\n";
                        }
                        App.WriteLine("错误: 在运行PowerShell命令时出现问题\n" + "调用堆栈:" + ctf);
                        return false;
                    }
                    runspace.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    App.WriteLine("PowerShell命令运行失败错误:\n" + ex.ToString());
                    return false;
                }
            }

        }

        //
        //
        //
        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";
        private static readonly string PREFS_PATH = @"preferences.json";

        private VersionList _versions;
        public static Preferences UserPrefs { get; set; }
        private readonly VersionDownloader _anonVersionDownloader = new VersionDownloader();
        private readonly VersionDownloader _userVersionDownloader = new VersionDownloader();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;
        private volatile bool _hasLaunchTask = false;
        public static string _DownloadDir;
        public bool isLoaded = false;
        public int ticktimes;
        public int ticktimes2;
        public bool hasdep;
        public bool hasrer;
        public bool hasrest;
        public bool haslaunch;
        public bool hasdeperror;
        public bool hasps;
        public ProgressWindow progressWindow = new ProgressWindow()
        {
            Topmost = true
        };

        DispatcherTimer dispatcherTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 1),
            IsEnabled = false
        };

        DispatcherTimer dispatcherTimer2 = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 1),
            IsEnabled = false
        };

        public void dispatcherTimer_tick(object sender,EventArgs e)
        {
            ticktimes += 1;
            if (_versions._hasError & (!_versions._isLoaded))
            {
                if (ticktimes == 15)
                {
                    progressWindow.SetContent("加载失败");
                    MessageBox.Show("加载版本列表时遇见问题:系统回报了加载错误标签,且到达了等待时间的阈值\n如果列表稍后正常显示,请忽略此消息\n如果列表没有正确显示,请重启应用后再试", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    progressWindow.Close();
                    dispatcherTimer.Stop();
                }
            }
            if (_versions._isLoaded)
            {
                progressWindow.SetValue(1);
                progressWindow.SetContent("加载完成");
                progressWindow.Close();
                dispatcherTimer.Stop();
            }
            if (ticktimes == 30)
            {
                progressWindow.SetContent("这次加载所耗的时间比平时长,可能是网络速度太慢...");
            }
            if (ticktimes == 60)
            {
                progressWindow.SetContent("加载时间已经达到一分钟,可能已经加载失败");
                if (MessageBox.Show("加载时间已经达到一分钟,可能已经加载失败,建议删除程序根目录的versions.json文件后再试","提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    progressWindow.Close();
                    Close();
                }
            }
            if (ticktimes == 3600)
            {
                progressWindow.SetContent("加载时间已经超过1小时,恭喜您获得了成就:时间多就任性");
            }
        }

        public void dispatcherTimer2_tick(object sender,EventArgs e)
        {
            ticktimes2 += 1;
            if (hasdep & !hasrer)
            {
                progressWindow.SetValue(1);
                progressWindow.SetContent("正在部署软件包...");
            }
            //备用部署方法
            if (hasdeperror & !hasps)
            {
                progressWindow.SetValue(2);
                progressWindow.SetContent("常规部署失败,正在尝试使用PowerShell进行部署...");
            }
            if (hasdeperror & hasps)
            {
                progressWindow.SetContent("成功使用Powershell部署了应用");
                progressWindow.SetValue(3);
                progressWindow.Close();
                dispatcherTimer2.Stop();
                hasdeperror = false;
                hasps = false;
                hasdep = false;
                hasrer = false;
                hasrest = false;
                haslaunch = false;
            }
            if (!hasdep & hasrer)
            {
                progressWindow.SetValue(2);
                progressWindow.SetContent("正在迁移数据...");
            }
            if (hasrer & !hasrest)
            {
                progressWindow.SetValue(2);
                progressWindow.SetContent("正在迁移数据...");
            }
            if (haslaunch)
            {
                progressWindow.SetValue(3);
                progressWindow.SetContent("启动成功");
                progressWindow.Close();
                dispatcherTimer2.Stop();
                Focus();
                hasdep = false;
                hasrer = false;
                hasrest = false;
                haslaunch = false;
            }
            //
            if (ticktimes2 == 120)
            {
                if (MessageBox.Show("本次部署过程所耗时间已经达到2分钟,但系统仍没有回报状态\n这表示本程序进行的两套部署方案可能都已经失败\n您现在可以按确定来取消本次操作,本程序将为您打开安装包保存目录,您可以手动选择对应版本进行安装,如果您按下取消或关闭本提示,本程序将继续等待","提示",MessageBoxButton.OKCancel,MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    progressWindow.Close();
                    if (UserPrefs.DownloadDir == string.Empty)
                    {
                        Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),AppDomain.CurrentDomain.BaseDirectory);
                    }
                    else
                    {
                        Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), UserPrefs.DownloadDir);
                    }
                }
            }
        }

        public MainWindow() {
            InitializeComponent();
            ShowBetasCheckbox.DataContext = this;

            dispatcherTimer.Tick += dispatcherTimer_tick;
            dispatcherTimer2.Tick += dispatcherTimer2_tick;

            progressWindow.Show("正在获取用户配置", "初始化", 1);

            if (File.Exists(PREFS_PATH)) {
                UserPrefs = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH));
                _DownloadDir = UserPrefs.DownloadDir;
            } else {
                UserPrefs = new Preferences();
                RewritePrefs();
            }

            progressWindow.SetValue(1);
            progressWindow.SetContent("正在获取Minecraft版本信息");
            progressWindow.SetValue(0);


            dispatcherTimer.Start();
            _versions = new VersionList("versions.json", this);
            VersionList.ItemsSource = _versions;
            var view = CollectionViewSource.GetDefaultView(VersionList.ItemsSource) as CollectionView;
            view.Filter = VersionListBetaFilter;
            _userVersionDownloaderLoginTask = new Task(() => {
                _userVersionDownloader.EnableUserAuthorization();
            });
            Dispatcher.Invoke(async () => {
                try {
                    await _versions.LoadFromCache();
                } catch (Exception e) {
                    App.WriteLine("列表缓存加载失败:\n" + e.ToString());
                    _versions._hasError = true;
                }
                try {
                    await _versions.DownloadList();
                } catch (Exception e) {
                    App.WriteLine("列表下载失败:\n" + e.ToString());
                    _versions._hasError = true;
                }
            });
        }

        public ICommand LaunchCommand => new RelayCommand((v) => InvokeLaunch((Version)v));

        public ICommand RemoveCommand => new RelayCommand((v) => InvokeRemove((Version)v));

        public ICommand DownloadCommand => new RelayCommand((v) => InvokeDownload((Version)v,UserPrefs.DownloadDir));

        private void InvokeLaunch(Version v) {
            if (_hasLaunchTask)
            {
                MessageBox.Show("已经有另一个启动任务在运行", "注意", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            progressWindow = null;
            progressWindow = new ProgressWindow()
            {
                Topmost = true
            };
            progressWindow.Show("正在重注册软件包...","启动",3);
            progressWindow.SetValue(0);
            dispatcherTimer2.Start();
            _hasLaunchTask = true;
            Task.Run(async () => {
                string gameDir = Path.GetFullPath(v.GameDirectory);
                try {
                    await ReRegisterPackage(gameDir);
                } catch (Exception e) {
                    App.WriteLine("应用重注册失败:\n" + e.ToString());
                    MessageWindow MW1 = new MessageWindow();
                    MW1.Show("应用重注册失败:\n" + e.ToString(),"错误");
                    _hasLaunchTask = false;
                    return;
                }

                try {
                    var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(MINECRAFT_PACKAGE_FAMILY);
                    if (pkg.Count > 0)
                        await pkg[0].LaunchAsync();
                    haslaunch = true;
                    App.WriteLine("应用启动完成!");
                    _hasLaunchTask = false;
                } catch (Exception e) {
                    App.WriteLine("应用启动失败:\n" + e.ToString());
                    MessageWindow MW1 = new MessageWindow();
                    MW1.Show("应用启动失败:\n" + e.ToString(),"错误");
                    _hasLaunchTask = false;
                    return;
                }
            });
        }

        private async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t) {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) => {
                switch (p.state)
                {
                    case DeploymentProgressState.Queued:
                        App.WriteLine("部署进度: 正在排队部署请求 " + p.percentage + "%");
                        break;
                    case DeploymentProgressState.Processing:
                        App.WriteLine("部署进度: 正在部署 " + p.percentage + "%");
                        break;
                    default:
                        App.WriteLine("部署进度: 未知状态");
                        break;
                }
            };
            t.Completed += (v, p) => {
                if (p == AsyncStatus.Error)
                {
                    App.WriteLine("部署软件包时出现错误,将会尝试使用PowerShell进行部署.");
                    hasdeperror = true;
                }
                switch (p)
                {
                    case AsyncStatus.Canceled:
                        App.WriteLine("常规部署结束: 操作被取消");
                        break;
                    case AsyncStatus.Completed:
                        App.WriteLine("常规部署结束: 部署成功");
                        break;
                    case AsyncStatus.Error:
                        App.WriteLine("常规部署结束: 部署失败");
                        break;
                    case AsyncStatus.Started:
                        App.WriteLine("常规部署未结束");
                        break;
                    default:
                        App.WriteLine("常规部署结束: 未知结果");
                        break;
                }
                App.WriteLine("常规部署结束: " + p);
                src.SetResult(1);
            };
            await src.Task;
        }

        private string GetBackupMinecraftDataDir() {
            App.WriteLine("正在获取数据备份目录");
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tmpDir = Path.Combine(localAppData, "TmpMinecraftLocalState");
            return tmpDir;
        }

        private void BackupMinecraftDataForRemoval() {
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            string tmpDir = GetBackupMinecraftDataDir();
            if (Directory.Exists(tmpDir)) {
                App.WriteLine("BackupMinecraftDataForRemoval error: " + tmpDir + " already exists");
                Process.Start("explorer.exe", tmpDir);
                MessageWindow MW1 = new MessageWindow();
                MW1.Show("用于备份Minecraft数据的临时目录已存在.这可能意味着上次备份数据失败,请手动备份目录.","提示");
                throw new Exception("临时目录已存在");
            }
            App.WriteLine("将Minecraft的数据移动到: " + tmpDir);
            Directory.Move(data.LocalFolder.Path, tmpDir);
        }

        private void RestoreMove(string from, string to) {
            foreach (var f in Directory.EnumerateFiles(from)) {
                string ft = Path.Combine(to, Path.GetFileName(f));
                if (File.Exists(ft)) {
                    if (MessageBox.Show("目标目录中已存在文件 " + ft + " \n要替换它吗?否则旧文件将丢失.", "从以前的安装还原数据目录", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    File.Delete(ft);
                }
                File.Move(f, ft);
            }
            foreach (var f in Directory.EnumerateDirectories(from)) {
                string tp = Path.Combine(to, Path.GetFileName(f));
                if (!Directory.Exists(tp)) {
                    if (File.Exists(tp) && MessageBox.Show("文件 " + tp + " 不是一个目录. 要删除它吗? 否则旧目录中的数据将丢失.", "从以前的安装还原数据目录", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    Directory.CreateDirectory(tp);
                }
                RestoreMove(f, tp);
            }
        }

        private void RestoreMinecraftDataFromReinstall() {
            string tmpDir = GetBackupMinecraftDataDir();
            if (!Directory.Exists(tmpDir))
                return;
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            App.WriteLine("将Minecraft的备份数据移动到: " + data.LocalFolder.Path);
            RestoreMove(tmpDir, data.LocalFolder.Path);
            Directory.Delete(tmpDir, true);
            hasrest = true;
        }

        private async Task ReRegisterPackage(string gameDir) {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY)) {
                if (pkg.InstalledLocation.Path == gameDir) {
                    App.WriteLine("正在跳过软件包移除操作 - 存在相同路径: " + pkg.Id.FullName + " " + pkg.InstalledLocation.Path);
                    hasrer = true;
                    return;
                }
                App.WriteLine("移除软件包: " + pkg.Id.FullName + " " + pkg.InstalledLocation.Path);
                if (!pkg.IsDevelopmentMode) {
                    BackupMinecraftDataForRemoval();
                    await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, 0));
                } else {
                    App.WriteLine("此软件包处于开发模式");
                    await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData));
                }
                App.WriteLine("软件包移除完成: " + pkg.Id.FullName);
                hasdep = false;
                break;
            }
            App.WriteLine("正在注册软件包...");
            string manifestPath = Path.Combine(gameDir, "AppxManifest.xml");
            await DeploymentProgressWrapper(new PackageManager().RegisterPackageAsync(new Uri(manifestPath), null, DeploymentOptions.DevelopmentMode));
            if (hasdeperror)
            {
                App.WriteLine("默认方法注册包失败,尝试执行PowerShell方法...");
                string appx;char[] chars;
                chars = gameDir.ToCharArray();
                if (chars.ElementAt(chars.Length -1).ToString() == "\\")
                {
                    appx = gameDir.Remove(chars.Length - 1);
                }
                else
                {
                    appx = gameDir;
                }
                if (Powershell.AddAppxPackage(appx + ".Appx"))
                {
                    hasps = true;
                }
                else
                {
                    hasps = false;
                }
                return;
            }
            App.WriteLine("应用重注册完成!");
            hasrer = true;
            RestoreMinecraftDataFromReinstall();
        }

        private void InvokeDownload(Version v,string m_downloaddir = "") {
            System.Threading.CancellationTokenSource cancelSource = new System.Threading.CancellationTokenSource();
            v.DownloadInfo = new VersionDownloadInfo();
            v.DownloadInfo.IsInitializing = true;
            v.DownloadInfo.CancelCommand = new RelayCommand((o) => cancelSource.Cancel());

            App.WriteLine("下载开始");
            Task.Run(async () => {
                string dlPath = m_downloaddir + "Minecraft-" + v.Name + ".Appx";
                VersionDownloader downloader = _anonVersionDownloader;
                if (v.IsBeta) {
                    downloader = _userVersionDownloader;
                    if (System.Threading.Interlocked.CompareExchange(ref _userVersionDownloaderLoginTaskStarted, 1, 0) == 0) {
                        _userVersionDownloaderLoginTask.Start();
                    }
                    await _userVersionDownloaderLoginTask;
                }
                try {
                    await downloader.Download(v.UUID, "1", dlPath, (current, total) => {
                        if (v.DownloadInfo.IsInitializing) {
                            App.WriteLine("开始从链接下载文件");
                            v.DownloadInfo.IsInitializing = false;
                            if (total.HasValue)
                                v.DownloadInfo.TotalSize = total.Value;
                        }
                        v.DownloadInfo.DownloadedBytes = current;
                    }, cancelSource.Token);
                    App.WriteLine("下载完成");
                } catch (Exception e) {
                    App.WriteLine("下载失败:\n" + e.ToString());
                    if (!(e is TaskCanceledException))
                    {
                        MessageWindow MW1 = new MessageWindow();
                        MW1.Show("下载失败:\n" + e.ToString(),"错误");
                    }
                    v.DownloadInfo = null;
                    return;
                }
                try {
                    v.DownloadInfo.IsExtracting = true;
                    string dirPath = v.GameDirectory;
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);
                    ZipFile.ExtractToDirectory(dlPath, dirPath);
                    v.DownloadInfo = null;
                    File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                } catch (Exception e) {
                    App.WriteLine("提取失败:\n" + e.ToString());
                    MessageWindow MW1 = new MessageWindow();
                    MW1.ShowDialog("提取失败:\n" + e.ToString(),"错误");
                    v.DownloadInfo = null;
                    return;
                }
                v.DownloadInfo = null;
                v.UpdateInstallStatus();
            });
        }

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

        public string UnFixPathString(string m_String)
        {
            int length = m_String.ToCharArray().Length - 1;
            char[] chara = m_String.ToCharArray();
            if (chara[length].ToString() == "\\")
            {
                string sstring = string.Empty;
                for (int i = 0; i < length; i++)
                {
                    sstring += chara[i].ToString();
                }
                return sstring;
            }
            else
            {
                return m_String;
            }
        }

        public void DelDir(Version v)
        {
            if (Directory.Exists(v.GameDirectory))
            {
                Directory.Delete(v.GameDirectory, true);
            }
            string appxdir = UnFixPathString(v.GameDirectory) + ".Appx";
            App.WriteLine(appxdir);
            if (File.Exists(appxdir) & UserPrefs.DelAppx)
            {
                App.WriteLine("正在删除安装程序...");
                File.Delete(appxdir);
            }
            Latch.Signal();
        }

        System.Threading.CountdownEvent Latch = new System.Threading.CountdownEvent(1);

        private void InvokeRemove(Version v) {
            App.WriteLine("正在移除指定版本: " + v.Name);
            ProgressWindow PW1 = new ProgressWindow
            {
                Topmost = true
            };
            PW1.Show("正在移除: Minecraft-" + v.Name, "移除", 1);
            if (Directory.Exists(v.GameDirectory))
            {
                System.Threading.Thread thread = new System.Threading.Thread(delegate ()
                { DelDir(v); }) ;
                thread.Start();
                Latch.Wait();
                PW1.SetValue(1);
                PW1.Close();
            }
            else
            {
                PW1.SetContent("指定的目录不存在!");
                MessageBox.Show("指定的目录不存在:\n" + v.GameDirectory);
                PW1.Close();
            }
            Latch.Reset();
            v.UpdateInstallStatus();
            App.WriteLine("移除操作完成");
        }

        private void ShowBetaVersionsCheck_Changed(object sender, RoutedEventArgs e) {
            if (isLoaded)
            {
                UserPrefs.ShowBetas = ShowBetasCheckbox.IsChecked ?? false;
                CollectionViewSource.GetDefaultView(VersionList.ItemsSource).Refresh();
                RewritePrefs();
                App.WriteLine("修改用户配置: ShowBetas --> " + UserPrefs.ShowBetas);
            }
        }

        private bool VersionListBetaFilter(object obj) {
            return !(obj as Version).IsBeta || UserPrefs.ShowBetas;
        }

        private void RewritePrefs() {
            App.WriteLine("正在重写用户配置文件...");
            File.WriteAllText(PREFS_PATH, JsonConvert.SerializeObject(UserPrefs));
        }

        public static void ReloadPrefs()
        {
            if (File.Exists(PREFS_PATH))
            {
                App.WriteLine("正在重写用户配置文件并应用配置...");
                UserPrefs = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH));
                _DownloadDir = UserPrefs.DownloadDir;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageWindow MW1 = new MessageWindow();
            MW1.Show("程序版本: 2.0.3 \n \n本作者对原版的改动内容:\n1.重新设计了UI,在美化界面的同时,添加了一些载入过程的可视化,让用户清楚当前的处理状态\n2.添加了对未处理错误的捕捉,大幅度减少程序异常退出几率\n3.对原作者代码进行了一些修复,优化了处理逻辑,添加了备用部署方案\n4.添加自定义设置项(实验)\n5.添加中文支持\n \n原作者: McMrARM\n修改作者: GodLeaveMe\n \n*本程序免费使用,不可付费传播\n \n注意:因自愿使用本程序而对用户造成的损失,与本程序或本程序作者无关\n \n第三方库:\nMaterialDesignColors\nMaterialDesignThemes.Wpf\nNewtonsoft.Json", "关于本程序");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
            WindowBrush = TitlePanel.Background;
            m_BorderBrush = BorderBrush;
            isLoaded = true;
            ShowBetasCheckbox.IsChecked = UserPrefs.ShowBetas;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiniSizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            MiniSizeButton.IsChecked = false;
        }

        private void TitlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MaxSizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState m_WindowState = WindowState;
            bool m_WindowTopMost = Topmost;
            ResizeMode m_WindowResizeMode = ResizeMode;
            Rect m_WindowRect = new Rect();
            m_WindowRect.X = Left;
            m_WindowRect.Y = Top;
            m_WindowRect.Width = Width;
            m_WindowRect.Height = Height;
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                Topmost = true;
            }else
            {
                WindowState = WindowState.Normal;
                Topmost = false;
            }
            e.Handled = true;
            MaxSizeButton.IsChecked = false;
        }

        public Brush WindowBrush;
        public Brush m_BorderBrush;

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

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow mSteeingWindow = new SettingWindow();
            mSteeingWindow.Show(UserPrefs.DownloadDir);
        }
    }

    namespace WPFDataTypes {

        public class NotifyPropertyChangedBase : INotifyPropertyChanged {

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name) {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
            }

        }

        public interface ICommonVersionCommands {

            ICommand LaunchCommand { get; }

            ICommand DownloadCommand { get; }

            ICommand RemoveCommand { get; }

        }

        public class Versions : List<Object> {
        }

        public class Version : NotifyPropertyChangedBase {

            public Preferences UserPrefs { get; set; }
            private static readonly string PREFS_PATH = @"preferences.json";

            public Version() { }
            public Version(string uuid, string name, bool isBeta, ICommonVersionCommands commands) {

                if (File.Exists(PREFS_PATH))
                {
                    UserPrefs = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(PREFS_PATH));
                    DownloadDir = UserPrefs.DownloadDir;
                }

                this.UUID = uuid;
                this.Name = name;
                this.IsBeta = isBeta;
                this.DownloadCommand = commands.DownloadCommand;
                this.LaunchCommand = commands.LaunchCommand;
                this.RemoveCommand = commands.RemoveCommand;
            }

            public string UUID { get; set; }
            public string Name { get; set; }
            public bool IsBeta { get; set; }
            public string DownloadDir { get; set; }

            public string GameDirectory => FixPathString(DownloadDir + "Minecraft-" + Name);

            public bool IsInstalled => Directory.Exists(GameDirectory);

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

            public string DisplayName {
                get {
                    return Name + (IsBeta ? " (测试版)" : "");
                }
            }
            public string DisplayInstallStatus {
                get {
                    return IsInstalled ? "已安装" : "未安装";
                }
            }

            public ICommand LaunchCommand { get; set; }
            public ICommand DownloadCommand { get; set; }
            public ICommand RemoveCommand { get; set; }

            private VersionDownloadInfo _downloadInfo;
            public VersionDownloadInfo DownloadInfo {
                get { return _downloadInfo; }
                set { _downloadInfo = value; OnPropertyChanged("DownloadInfo"); OnPropertyChanged("IsDownloading"); }
            }

            public bool IsDownloading => DownloadInfo != null;

            public void UpdateInstallStatus() {
                OnPropertyChanged("IsInstalled");
            }

        }

        public class VersionDownloadInfo : NotifyPropertyChangedBase {

            private bool _isInitializing;
            private bool _isExtracting;
            private long _downloadedBytes;
            private long _totalSize;

            public bool IsInitializing {
                get { return _isInitializing; }
                set { _isInitializing = value; OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsExtracting {
                get { return _isExtracting; }
                set { _isExtracting = value; OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsProgressIndeterminate {
                get { return IsInitializing || IsExtracting; }
            }

            public long DownloadedBytes {
                get { return _downloadedBytes; }
                set { _downloadedBytes = value; OnPropertyChanged("DownloadedBytes"); OnPropertyChanged("DisplayStatus"); }
            }

            public long TotalSize {
                get { return _totalSize; }
                set { _totalSize = value; OnPropertyChanged("TotalSize"); OnPropertyChanged("DisplayStatus"); }
            }

            public string DisplayStatus {
                get {
                    if (IsInitializing)
                        return "下载中...";
                    if (IsExtracting)
                        return "提取中...";
                    return "下载中... " + (DownloadedBytes / 1024 / 1024) + "MiB/" + (TotalSize / 1024 / 1024) + "MiB";
                }
            }

            public ICommand CancelCommand { get; set; }

        }

    }
}
