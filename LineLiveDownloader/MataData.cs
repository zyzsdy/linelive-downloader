using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LineLiveDownloader
{
    class MataData
    {
        private MainWindow _mw;
        public event MetaDataEvent OnGetMetaData;
        public event M3u8Event OnGetM3u8Url;

        public MataData(MainWindow mw)
        {
            _mw = mw;
        }

        public async void Start(string cid, string bid)
        {
            var json = await GetMetaJSON(cid, bid);
            if (json == null)
            {
                _mw.AppendLogln("ERROR", "Can not get mata data from LINE live server.");
                return;
            }
            var obj = JObject.Parse(json);

            OnGetMetaData?.Invoke(this, new MetaDataArgs
            {
                ArchiveStatus = obj["item"]["archiveStatus"].ToString(),
                ChannelId = obj["item"]["channelId"].ToString(),
                ChannelName = obj["item"]["channel"]["name"].ToString(),
                Id = obj["item"]["id"].ToString(),
                LsaPath = obj["lsaPath"].ToString(),
                Status = obj["item"]["liveStatus"].ToString(),
                Title = obj["item"]["title"].ToString()
            });
        }

        private Task<string> GetMetaJSON(string cid, string bid)
        {
            return Task.Run(() =>
            {
                var mainPageUrl = "https://live.line.me/r/channels/" + cid + "/broadcast/" + bid;
                var wc = new WebClient();
                wc.Headers.Add("Accept: text/html");
                wc.Headers.Add("User-Agent: " + Ver.UA);
                wc.Headers.Add("Accept-Language: ja;q=0.8,zh-CN,zh;q=0.6,en;q=0.4");

                //Send a http request to get home page for this web broadcast.
                string html;

                try
                {
                    html = wc.DownloadString(mainPageUrl);
                }
                catch (Exception e)
                {
                    _mw.AppendLogln("ERROR", "Failed to load the home page: " + e.Message);
                    return null;
                }

                //Extract metadata json string.
                const string pattern = @"(?<=data-broadcast="")(.+)(?="")";
                var colls = Regex.Matches(html, pattern);
                foreach (Match mat in colls)
                {
                    _mw.AppendLogln("INFO", "Successfully get matadata.");
                    return mat.Value.Replace("&quot;", "\"");
                }

                _mw.AppendLogln("INFO", "Failed to parse html.");
                return null;
            });
        }

        public async void StartUrl(MetaDataArgs data)
        {
            var m3U8Url = await GetM3U8Url(data);
            OnGetM3u8Url?.Invoke(this, new M3u8Args {url = m3U8Url});
        }

        private Task<string> GetM3U8Url(MetaDataArgs data)
        {
            return Task.Run(() =>
            {
                string url = null;
                if (data.Status == "LIVE")
                {
                    url = "https://lssapi.line-apps.com/v1/live/playInfo?contentId=" + data.LsaPath;
                }
                else if (data.Status == "FINISHED")
                {
                    url = "https://lssapi.line-apps.com/v1/vod/playInfo?contentId=" + data.LsaPath;
                }
                else
                {
                    _mw.AppendLogln("ERROR", "Undefined status: " + data.Status);
                    return null;
                }

                var wc = new WebClient();
                wc.Headers.Add("Accept: text/html,*/*");
                wc.Headers.Add("User-Agent: " + Ver.UA);
                wc.Headers.Add("Accept-Language: ja;q=0.8,zh-CN,zh;q=0.6,en;q=0.4");

                //download json
                string json;
                try
                {
                    json = wc.DownloadString(url);
                }
                catch (Exception e)
                {
                    _mw.AppendLogln("ERROR", "Failed to load m3u8 url: " + e.Message);
                    return null;
                }

                //parse json
                var obj = JObject.Parse(json);
                var m3U8Url = obj["playUrls"]["720"].ToString();

                return m3U8Url;
            });
        }
    }

    public delegate void MetaDataEvent(object sender, MetaDataArgs e);

    public class MetaDataArgs
    {
        public string Id;
        public string ChannelId;
        public string Title;
        public string ChannelName;
        public string Status;
        public string LsaPath;
        public string ArchiveStatus;
    }

    public delegate void M3u8Event(object sender, M3u8Args e);

    public class M3u8Args
    {
        public string url;
    }
}
