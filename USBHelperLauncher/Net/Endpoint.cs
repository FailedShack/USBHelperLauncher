﻿using Fiddler;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace USBHelperLauncher.Net
{
    class Endpoint
    {
        public string HostName { get; }

        public Endpoint(string hostName)
        {
            HostName = hostName;
        }

        public bool Handle(Session oS)
        {
            foreach (var method in GetType().GetMethods())
            {
                if (method.GetCustomAttributes().OfType<Request>().Any(request => request.Matches(oS)))
                {
                    method.Invoke(this, new object[] { oS });
                    return true;
                }
            }
            return false;
        }

        public bool Matches(Session oS)
        {
            return oS.HostnameIs(HostName);
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

            // Treat request as GET to keep response body
            string temp = oS.RequestMethod;
            oS.RequestMethod = "GET";
            oS.LoadResponseFromFile(localPath);
            oS.RequestMethod = temp;
            oS.oResponse["ETag"] = oS.GetResponseBodyHashAsBase64("md5");
            if (oS.HTTPMethodIs("HEAD"))
            {
                oS.responseBodyBytes = Utilities.emptyByteArray;
            }

            Proxy.LogRequest(oS, this, "Sending local copy of " + fileName);
        }

        protected void LoadResponseFromByteArray(Session oS, byte[] bytes)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Length"] = bytes.Length.ToString();
            oS.oResponse["ETag"] = Utilities.GetHashAsBase64("md5", bytes);
            if (!oS.HTTPMethodIs("HEAD"))
            {
                oS.responseBodyBytes = bytes;
            }
        }
    }
}
