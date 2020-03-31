using System;
using System.Net;
using System.Net.Http;

namespace USBHelperLauncher.Utils
{
    class RedirectRequest
    {
        public Uri Uri { get; set; }
        public HttpMethod Method { get; set; }
        public WebProxy Proxy { get; set; }
        public WebHeaderCollection Headers { get; }
        public event EventHandler BeforeSubmit;

        public RedirectRequest(Uri uri)
        {
            Uri = uri;
            Method = HttpMethod.Get;
            Headers = new WebHeaderCollection();
        }

        public HttpWebResponse GetResponse()
        {
            var args = new SubmitArgs() { FollowRedirect = true };
            BeforeSubmit?.Invoke(this, args);
            HttpWebResponse resp;
            bool redirect;
            do
            {
                var request = (HttpWebRequest)WebRequest.Create(Uri);
                request.Method = Method.Method;
                request.Proxy = Proxy;
                request.AllowAutoRedirect = false;
                request.Headers = Headers;
                resp = (HttpWebResponse)request.GetResponse();
                var status = (int)resp.StatusCode;
                var location = resp.Headers["Location"];
                redirect = status >= 300 && status < 400 && location != null;
                if (redirect)
                {
                    resp.Close();
                    Uri = new Uri(location);
                    BeforeSubmit?.Invoke(this, args);
                }
            } while (redirect && args.FollowRedirect);
            return resp;
        }

        class SubmitArgs : EventArgs
        {
            public bool FollowRedirect { get; set; }
        }
    }
}
