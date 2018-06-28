using Fiddler;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace USBHelperLauncher.Net
{
    class Endpoint
    {
        protected static readonly Logger logger = Program.GetLogger();
        protected static readonly Database database = Program.GetDatabase();

        private string hostName;

        public Endpoint(string hostName)
        {
            this.hostName = hostName;
        }

        public bool Handle(Session oS)
        {
            Request request = null;
            foreach (var method in GetType().GetMethods())
            {
                if ((request = method.GetCustomAttributes().OfType<Request>().FirstOrDefault()) != null && request.Matches(oS))
                {
                    method.Invoke(this, new object[] { oS });
                    return true;
                }
            }
            return false;
        }

        public bool Matches(Session oS)
        {
            return oS.HostnameIs(hostName);
        }

        public NameValueCollection GetRequestData(Session oS)
        {
            NameValueCollection data = null;
            if (oS.oRequest["Content-Type"] == "application/x-www-form-urlencoded")
            {
                data = HttpUtility.ParseQueryString(oS.GetRequestBodyAsString());
            }
            else
            {
                string path = oS.PathAndQuery;
                int index = path.IndexOf('?');
                if (index != -1)
                {
                    data = HttpUtility.ParseQueryString(path.Substring(index));
                }
            }
            return data;
        }

        protected void LoadResponseFromFile(Session oS, string folder)
        {
            string fileName = Path.GetFileName(oS.PathAndQuery);
            string localPath = Path.Combine(Program.GetLauncherPath(), folder, fileName);
            oS.utilCreateResponseAndBypassServer();
            oS.LoadResponseFromFile(localPath);
            Proxy.LogRequest(oS, this, "Sending local copy of " + fileName);
        }

        protected void LoadResponseFromByteArray(Session oS, byte[] bytes)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Length"] = bytes.Length.ToString();
            oS.responseBodyBytes = bytes;
        }
    }
}
