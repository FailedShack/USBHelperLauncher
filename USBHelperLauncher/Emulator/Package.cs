using System;
using System.Collections.Generic;

namespace USBHelperLauncher.Emulator
{
    public class Package
    {
        private const String Format = "{0} {1}";

        private Uri uri;
        private string name, version, installPath;
        private Dictionary<string, string> metadata = new Dictionary<string, string>();

        public Package(Uri uri, string name, string version)
        {
            this.uri = uri;
            this.name = name;
            this.version = version;
            this.installPath = "";
        }

        public Package(Uri uri, string name, string version, string installPath)
        {
            this.uri = uri;
            this.name = name;
            this.version = version;
            this.installPath = installPath;
        }

        public Uri GetURI()
        {
            return uri;
        }

        public string GetName()
        {
            return name;
        }

        public string GetVersion()
        {
            return version;
        }

        public string GetInstallPath()
        {
            return installPath;
        }

        public void SetMeta(string key, string value)
        {
            metadata.Add(key, value);
        }

        public string GetMeta(string key)
        {
            return metadata[key];
        }

        public Dictionary<string, string> GetMeta()
        {
            return metadata;
        }

        public override string ToString()
        {
            return String.Format(Format, name, version);
        }
    }
}
