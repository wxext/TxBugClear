using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace TxBugClear
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// https://github.com/wxext/TxBugClear
    /// </summary>
    public partial class MainWindow : Window
    {
        DirectoryInfo TxBugReportDir;
        DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick; ;
            timer.Start();

            var TxBugReportPath= Path.Combine(
                Path.GetTempPath(),
                "Tencent"
                 );
            TxBugReportDir = new DirectoryInfo(TxBugReportPath);
            txtLog.Text = "正在监测bug文件...";
            try
            {
                bugStr = File.ReadAllText(".bugStr.ini");
                bugTime = long.Parse(File.ReadAllText(".bugTime.ini"));
                txtLog.Text = new DateTime(bugTime).ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + bugStr;
            }
            catch (Exception)
            {
            }
        }
        long bugTime = 0;
        string bugStr = "";
        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var item in Process.GetProcessesByName("TxBugReport")) KillProcessAndChildren(item.Id);
            if (TxBugReportDir.Exists)
            {
                foreach (var item in TxBugReportDir.GetFiles("*"))
                {

                    try
                    {
                        if (Regex.IsMatch(item.Name, "^WeChat[0-9a-z]+.txt$"))
                        {
                            if (item.CreationTime.Ticks > bugTime)
                            {
                                bugTime = item.CreationTime.Ticks;
                                var arr = File.ReadAllText(item.FullName).Split('\n');
                                foreach (var str in arr)
                                {
                                    if (str.Contains("show in line:")) bugStr = str;
                                    else if (Regex.IsMatch(str, "^\\[[0-9A-F]+,[0-9A-F]+\\] "))
                                    {
                                        var mstr = str.ToUpper();
                                        if (mstr.Contains("\\SYSTEM")) continue;
                                        if (mstr.Contains("\\WINDOWS\\")) continue;
                                        if (mstr.Contains("\\MICROSOFT\\")) continue;
                                        if (mstr.Contains("\\WECHAT\\"))
                                        {
                                            if (!mstr.Contains("\\WECHATWIN")) continue;
                                        }
                                        bugStr += str;
                                        File.WriteAllText(".bugStr.ini", bugStr);
                                        File.WriteAllText(".bugTime.ini", bugTime.ToString());
                                    }
                                }
                                txtLog.Text = new DateTime(bugTime).ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + bugStr;
                            }
                        }
                        item.Delete();
                    }
                    catch (Exception ex)
                    {
                        txtLog.Text = ex.Message + Environment.NewLine + txtLog.Text;
                    }
                }
            }
        }

        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (Exception)
            {
                /* process already exited */
            }
        }
    }
}
