using System;
using System.Linq;
using System.Reflection;

namespace USBHelperLauncher.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    class Setting : Attribute
    {
        public string Section { get; }
        public object Default { get; }
        public bool Forgetful { get; }

        public Setting(string section, object def = null, bool forgetful = false)
        {
            Section = section;
            Default = def;
            Forgetful = forgetful;
        }

        public static Setting From(PropertyInfo prop)
        {
            return prop.GetCustomAttributes().OfType<Setting>().FirstOrDefault();
        }
    }
}
