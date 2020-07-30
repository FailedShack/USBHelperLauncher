using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using USBHelperInjector;
using USBHelperLauncher.Configuration;
using USBHelperLauncher.Properties;

namespace USBHelperLauncher
{
    class LocaleProvider
    {
#pragma warning disable 0649
        public struct LocaleInfo
        {
            public string Name;
            public string Native;
        }
#pragma warning restore 0649

        private Dictionary<string, LocaleInfo> _knownLocales, _availableLocales;

        public string ChosenLocale
        {
            get
            {
                var userLocale = Settings.Locale ?? CultureInfo.CurrentUICulture.Name;
                return AvailableLocales.ContainsKey(userLocale) ? userLocale : Localization.DefaultLocale;
            }
        }

        public Dictionary<string, LocaleInfo> KnownLocales
        {
            get
            {
                if (_knownLocales == null)
                    _knownLocales = JsonConvert.DeserializeObject<Dictionary<string, LocaleInfo>>(Resources.Locales);
                return _knownLocales;
            }
        }

        public Dictionary<string, LocaleInfo> AvailableLocales
        {
            get
            {
                if (_availableLocales == null || dirty)
                {
                    IEnumerable<string> locales = new string[] { };
                    if (Directory.Exists("locale"))
                    {
                        locales = Directory.GetFiles("locale", "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .Where(x => !x.EndsWith(".local"));
                    }
                    _availableLocales = KnownLocales
                        .Where(x => locales.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);
                    dirty = false;
                }
                return _availableLocales;
            }
        }

        private bool dirty = false;

        public async Task<bool> UpdateIfNeeded(ProgressDialog dialog)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, e) => dialog.Invoke(new Action(() => dialog.SetProgress(e.ProgressPercentage)));
                    if (AvailableLocales.Count > 0)
                    {
                        client.Headers[HttpRequestHeader.IfNoneMatch] = string.Format("\"{0}\"", Settings.TranslationsBuild);
                    }
                    await client.DownloadFileTaskAsync(new Uri("https://dl.nul.sh/USBHelperLauncher/translations.zip?v=1"), tempFile);
                    Settings.TranslationsBuild = client.ResponseHeaders[HttpResponseHeader.ETag].Trim('"');
                    Settings.Save();
                }
                if (!Directory.Exists("locale"))
                {
                    Directory.CreateDirectory("locale");
                }
                using (var zip = ZipFile.OpenRead(tempFile))
                {
                    foreach (var entry in zip.Entries)
                    {
                        var fileName = Path.Combine("locale", entry.Name);
                        entry.ExtractToFile(fileName, true);
                    }
                }
                dirty = true;
                return true;
            }
            catch (WebException e) when (((HttpWebResponse)e.Response)?.StatusCode == HttpStatusCode.NotModified)
            {
                return false;
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
