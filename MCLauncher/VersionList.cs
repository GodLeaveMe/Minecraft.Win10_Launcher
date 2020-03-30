using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MCLauncher {
    class VersionList : ObservableCollection<WPFDataTypes.Version> {

        internal bool _isLoaded = false;
        internal bool _hasError = false;

        private readonly string _cacheFile;
        private readonly WPFDataTypes.ICommonVersionCommands _commands;
        private readonly HttpClient _client = new HttpClient();

        public VersionList(string cacheFile, WPFDataTypes.ICommonVersionCommands commands) {
            _cacheFile = cacheFile;
            _commands = commands;
        }

        private void ParseList(JArray data) {
            Clear();
            // ([name, uuid, isBeta])[]
            App.WriteLine("正在绘制版本列表...");
            foreach (JArray o in data.AsEnumerable().Reverse()) {
                Add(new WPFDataTypes.Version(o[1].Value<string>(), o[0].Value<string>(), o[2].Value<int>() == 1, _commands));
            }
            App.WriteLine("绘制完成");
        }

        public async Task LoadFromCache() {
            try {
                App.WriteLine("正在从缓存加载版本列表...");
                using (var reader = File.OpenText(_cacheFile)) {
                    var data = await reader.ReadToEndAsync();
                    ParseList(JArray.Parse(data));
                    _isLoaded = true;
                    App.WriteLine("加载完成");
                }
            } catch (FileNotFoundException) {
            }
        }

        public async Task DownloadList() {
            App.WriteLine("正在下载版本列表...");
            var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadAsStringAsync();
            File.WriteAllText(_cacheFile, data);
            ParseList(JArray.Parse(data));
            _isLoaded = true;
            App.WriteLine("下载完成");
        }

    }
}
