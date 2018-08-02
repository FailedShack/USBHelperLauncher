using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Net
{
    // Allows easy access to hidden FiddlerCore config fields of interest
    class ProxyConfig
    {
        private static readonly Type CONFIG = typeof(Fiddler.CONFIG);

        public static string CertCommonName
        {
            get
            {
                return GetConfigFieldValue("\u001B") as string;
            }
            set
            {
                SetConfigFieldValue("\u001B", value);
            }
        }

        public static string CertOrganization
        {
            get
            {
                return GetConfigFieldValue("\u001C") as string;
            }
            set
            {
                SetConfigFieldValue("\u001C", value);
            }
        }

        private static FieldInfo GetConfigField(string fieldName)
        {
            return CONFIG.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static object GetConfigFieldValue(string fieldName)
        {
            return GetConfigField(fieldName).GetValue(null);
        }

        private static void SetConfigFieldValue(string fieldName, object value)
        {
            GetConfigField(fieldName).SetValue(null, value);
        }
    }
}
