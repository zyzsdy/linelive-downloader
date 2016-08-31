using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LineLiveDownloader
{
    class Downloader
    {
        private string _savePath;
        private string _url;
        private MainWindow _mw;

        private Process _proc;
        private bool _isStart = false;

        public event DownloaderStopEvent OnStop;

        public Downloader(string savePath, string url, MainWindow mw)
        {
            _savePath = savePath;
            _url = url;
            _mw = mw;

            var thread = new Thread(StartProc);
            thread.Start();
        }

        private void StartProc()
        {
            _proc = new Process
            {
                StartInfo =
                {
                    FileName = "ffmpeg.exe",
                    Arguments = "-i \"" + _url + "\" -acodec copy -vcodec copy -absf aac_adtstoasc \"" +
                                _savePath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true,
            };

            _proc.Start();
            _isStart = true;
            _proc.Exited += _proc_Exited;

            using (var reader = _proc.StandardError)
            {
                var thisline = reader.ReadLine();
                while (_isStart)
                {
                    if (thisline != null)
                    {
                        _mw.AppendLogln("INFO", thisline);
                    }
                    else
                    {
                        _mw.AppendLogln("DEBUG", "ffmpeg running");
                    }
                    thisline = reader.ReadLine();
                }
            }

            _proc.WaitForExit();
            _mw.AppendLogln("INFO", "Download successful!");
        }

        private void _proc_Exited(object sender, EventArgs e)
        {
            _isStart = false;
            _mw.AppendLogln("INFO", "ffmpeg Exited.");
            OnStop?.Invoke(this, new DownloaderStopArgs
            {
                msg = "Stop"
            });
        }

        public void Stop()
        {
            var buffer = _proc.StandardInput;
            buffer.WriteLineAsync("q");
        }
    }

    public delegate void DownloaderStopEvent(object sender, DownloaderStopArgs e);

    public class DownloaderStopArgs
    {
        public string msg;
    }
}
