using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;

namespace LineLiveDownloader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private SaveFileDialog _sfd;
        private MataData _mataData;
        private bool _isStart;
        private Downloader _downloader = null;

        public void AppendLogln(string level, string logText)
        {
            Dispatcher.Invoke(() => {
                InfoBox.AppendText("[" + level + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + logText + "\n");
            });
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mataData = new MataData(this);
            _sfd = new SaveFileDialog {FileName = "filename.mp4"};
            _isStart = false;
            AppendLogln("INFO", "Initialization complete.");
        }

        private void OpenSaveDlgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_sfd.ShowDialog() == true)
            {
                SavePathBox.Text = System.IO.Path.GetDirectoryName(_sfd.FileName) + "\\{cid}-{bid}-{Y}-{M}-{d}-{H}-{m}-{s}.mp4";
            }
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isStart)
            {
                _downloader?.Stop();
                StartBtn.Content = "Stoping...";
                StartBtn.IsEnabled = false;
            }
            else
            {
                var channelId = ChannelIdBox.Text;
                var broadcastId = BroadcastIdBox.Text;

                AppendLogln("INFO", "Starting at channel " + channelId + " in broadcast id " + broadcastId);

                _mataData.Start(channelId, broadcastId);
                _mataData.OnGetMetaData += _mataData_OnGetMetaData;
                _mataData.OnGetM3u8Url += _mataData_OnGetM3u8Url;

                StartBtn.Content = "Processing...";
                StartBtn.IsEnabled = false;
                _isStart = true;
            }
        }

        private void _mataData_OnGetM3u8Url(object sender, M3u8Args e)
        {
            var savePath = CompilePath(SavePathBox.Text);
            _downloader = new Downloader(savePath, e.url, this);
            _downloader.OnStop += _downloader_OnStop;
            StartBtn.Content = "Stop";
            StartBtn.IsEnabled = true;
            AppendLogln("DEBUG", "True url is: " + e.url);
        }

        private void _downloader_OnStop(object sender, DownloaderStopArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StartBtn.Content = "Start";
                StartBtn.IsEnabled = true;
                _isStart = false;
            });
        }

        private string CompilePath(string path)
        {
            path = path.Replace("{cid}", ChannelIdBox.Text);
            path = path.Replace("{bid}", BroadcastIdBox.Text);
            path = path.Replace("{Y}", DateTime.Now.Year.ToString());
            path = path.Replace("{M}", DateTime.Now.Month.ToString());
            path = path.Replace("{d}", DateTime.Now.Day.ToString());
            path = path.Replace("{H}", DateTime.Now.Hour.ToString());
            path = path.Replace("{m}", DateTime.Now.Minute.ToString());
            path = path.Replace("{s}", DateTime.Now.Second.ToString());
            return path;
        }

        private void _mataData_OnGetMetaData(object sender, MetaDataArgs e)
        {
            //print info
            ChannelIdLabel.Content = "Channel Id: " + e.ChannelId;
            BroadCastIdLabel.Content = "Broadcast Id: " + e.Id;
            ChannelNameLabel.Content = "Channel Name: " + e.ChannelName;
            TitleLabel.Content = "Title: " + e.Title;
            StatusLabel.Content = "Status: " + e.Status;
            ArchiveStatusLabel.Content = "Archive Status: " + e.ArchiveStatus;
            ProgramInfo.Visibility = Visibility.Visible;
            AppendLogln("DEBUG", "LsaPath: " + e.LsaPath);

            //getUrl
            _mataData.StartUrl(e);
        }

        private void InfoBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InfoBox.ScrollToEnd();
        }

        private void AboutBtn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this,
                "LINE LD v" + Ver.VER + "\r\n\r\nAuthor: Ted Zyzsdy\r\n\r\nHomepage: https://www.zyzsdy.com", "About LINE LD");
        }
    }
}
