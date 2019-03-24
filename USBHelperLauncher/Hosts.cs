using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace USBHelperLauncher
{
    class Hosts
    {
        private Dictionary<string, IPAddress> hosts;

        public Hosts()
        {
            hosts = new Dictionary<string, IPAddress>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Add(string host, IPAddress ip)
        {
            if (hosts.ContainsKey(host))
            {
                return false;
            }
            hosts.Add(host, ip);
            return true;
        }

        public IPAddress Get(string host)
        {
            return hosts.ContainsKey(host) ? hosts[host] : null;
        }

        public IEnumerable<string> GetHosts()
        {
            return hosts.Keys;
        }

        public void Clear()
        {
            hosts.Clear();
        }

        public static Hosts Load(string path)
        {
            var hosts = new Hosts();
            using (var reader = new JsonTextReader(File.OpenText(path)))
            {
                var json = JToken.ReadFrom(reader);
                foreach (var entry in json.Children<JProperty>())
                {
                    string host = entry.Name;
                    if (entry.Value.Type != JTokenType.String)
                    {
                        throw new ArgumentException("Host does contain a valid value: " + host);
                    }
                    string ipStr = (string) entry.Value;
                    if (!IPAddress.TryParse(ipStr, out IPAddress ip))
                    {
                        throw new ArgumentException("IP address could not be parsed: " + ipStr);
                    }
                    if (!hosts.Add(host, ip))
                    {
                        throw new ArgumentException("Host is assigned more than once: " + host);
                    }
                }
            }
            return hosts;
        }

        public void Save(string path)
        {
            var obj = new JObject();
            foreach (var entry in hosts)
            {
                obj.Add(entry.Key, entry.Value.ToString());
            }
            File.WriteAllText(path, obj.ToString());
        }
    }
}
