using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MCLauncher {

    public partial class App : Application {

        public void SetBinPath(string Path)
        {
            AppDomain.CurrentDomain.SetData("PRIVATE_BINPATH", Path);
            AppDomain.CurrentDomain.SetData("BINPATH_PROBE_ONLY", Path);
            var m =
                typeof(AppDomainSetup).GetMethod("UpdateContextProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var funsion =
                typeof(AppDomain).GetMethod("GetFusionContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m.Invoke(null, new object[]
            {
                funsion.Invoke(AppDomain.CurrentDomain,null),"PRIVATE_BINPATH",Path
            });
        }

        public Stream logstream;

        public App()
        {
            SetBinPath("lib\\ui;lib\\;lib\\system");
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnhandledTaskException;
        }

        protected void OnUnhandledTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageWindow MW1 = new MessageWindow()
            {
                Topmost = true
            };
            WriteLine("[捕捉到未处理的Task程错误]" + Environment.NewLine + e.Exception.ToString());
            MW1.ShowDialog("程序遇见了一个关键性错误,但也许可以继续运行,错误信息:\n类型:Task线程异常\n" + e.Exception.ToString(), "错误到捕捉");
            e.SetObserved();
        }

        protected void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StringBuilder Ex1 = new StringBuilder();
            if (e.IsTerminating)
            {
                if (e.ExceptionObject is Exception)
                {
                    Ex1.Append(((Exception)e.ExceptionObject).ToString());
                }
                else
                {
                    Ex1.Append(e.ExceptionObject);
                }
                MessageWindow MW1 = new MessageWindow()
                {
                    Topmost = true
                };
                WriteLine("[捕捉到未处理的UI主线程错误]" + Environment.NewLine + Ex1.ToString());
                MW1.ShowDialog("程序遇见了一个关键性错误,且无法继续运行,错误信息:\n类型:UI主线程异常\n" + Ex1.ToString(), "致命性错误");
            }
            else
            {
                if (e.ExceptionObject is Exception)
                {
                    Ex1.Append(((Exception)e.ExceptionObject).ToString());
                }else
                {
                    Ex1.Append(e.ExceptionObject);
                }
                MessageWindow MW1 = new MessageWindow()
                {
                    Topmost = true
                };
                WriteLine("[捕捉到未处理的UI主线程错误]" + Environment.NewLine + Ex1.ToString());
                MW1.ShowDialog("程序遇见了一个关键性错误,但也许可以继续运行,错误信息:\n类型:UI主线程异常\n" + Ex1.ToString(), "错误到捕捉");
            }
        }

        protected void OnDispatcherUnhandledException(object sender,DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageWindow MW1 = new MessageWindow()
                {
                    Topmost = true
                };
                WriteLine("[捕捉到未处理的非UI线程错误]" + Environment.NewLine + e.Exception.ToString());
                MW1.ShowDialog("程序遇见了一个关键性错误,但也许可以继续运行,错误信息:\n类型:非UI主线程异常\n" + e.Exception.ToString(), "错误到捕捉");
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageWindow MW1 = new MessageWindow()
                {
                    Topmost = true
                };
                WriteLine("[捕捉到未处理的非UI线程错误]" + Environment.NewLine + ex.ToString());
                MW1.ShowDialog("程序遇见了一个关键性错误,且无法恢复,错误信息:\n类型:非UI主线程异常\n" + ex.ToString(), "致命性错误");
            }
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            DateTime NowDate = DateTime.Now;

            const string n = "-";

            string logsroot = NowDate.Year + n + NowDate.Month + n + NowDate.Day + n + NowDate.Hour + n + NowDate.Minute + n + NowDate.Second;

            if (!Directory.Exists(@"logs"))
            {
                Directory.CreateDirectory(@"logs");
            }
            if (!Directory.Exists(@"logs\" + logsroot))
            {
                Directory.CreateDirectory(@"logs\" + logsroot);
            }
            string logdir = @"logs\" + logsroot + @"\Log.log";

            logstream = File.Open(logdir, FileMode.OpenOrCreate);

            TextWriterTraceListener textWriterTraceListener = new TextWriterTraceListener(logstream);

            textWriterTraceListener.TraceOutputOptions = TraceOptions.Callstack | TraceOptions.DateTime;
            Debug.Listeners.Add(textWriterTraceListener);
            Debug.AutoFlush = true;
            WriteLine("应用初始设置完成");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            logstream.Close();
            base.OnExit(e);
        }

        static internal void WriteLine(string m_string)
        {
            DateTime NowDate = DateTime.Now;

            const string n = "-";

            string longdate = NowDate.Year + n + NowDate.Month + n + NowDate.Day + " " + NowDate.Hour + "时" + NowDate.Minute + "分" + NowDate.Second + "秒";

            Debug.WriteLine("[" + longdate + "]" + Environment.NewLine + m_string);
        }

    }
}
