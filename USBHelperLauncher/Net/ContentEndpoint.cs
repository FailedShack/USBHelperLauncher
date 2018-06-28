using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Net
{
    class ContentEndpoint : Endpoint
    {
        public ContentEndpoint() : base("cdn.wiiuusbhelper.com") { }

        protected ContentEndpoint(string hostName) : base(hostName) { }

        [Request("/wiiu/info/icons/*")]
        public void GetWiiUIcon(Session oS)
        {
            LoadResponseFromFile(oS, @"images\wiiu\icons");
        }

        [Request("/3ds/icons/*")]
        public void Get3DSIcon(Session oS)
        {
            LoadResponseFromFile(oS, @"images\3ds\icons");
        }

        [Request("/res/emulators/*")]
        public void GetEmulator(Session oS)
        {
            LoadResponseFromFile(oS, "emulators");
        }

        [Request("/res/prerequisites/*")]
        public void GetRedistPackage(Session oS)
        {
            LoadResponseFromFile(oS, "redist");
        }

        [Request("/res/db/data.usb")]
        public void GetDatabase(Session oS)
        {
            LoadResponseFromDatabase(oS);
        }

        [Request("/res/db/datav4.enc")]
        public void GetDatabaseV4(Session oS)
        {
            LoadResponseFromDatabase(oS, Database.EncryptionVersion.DATA_V4);
        }

        [Request("/res/db/datav6.enc")]
        public void GetDatabaseV6(Session oS)
        {
            LoadResponseFromDatabase(oS, Database.EncryptionVersion.DATA_V6);
        }

        [Request("/res/db/*")]
        public void GetData(Session oS)
        {
            LoadResponseFromFile(oS, "data");
        }

        private void LoadResponseFromDatabase(Session oS, Database.EncryptionVersion? version = null)
        {
            string message = "Sending database contents {0} encryption.";
            byte[] bytes;
            if (version.HasValue)
            {
                bytes = database.Encrypt(version.Value).ToArray();
                message = string.Format(message, "with " + version.Value.ToString());
            }
            else
            {
                bytes = database.ToArray();
                message = string.Format(message, "without");
            }
            LoadResponseFromByteArray(oS, bytes);
            Proxy.LogRequest(oS, this, message);
        }
    }
}
