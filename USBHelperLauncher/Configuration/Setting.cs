using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    class Setting : Attribute
    {
        public string Section { get; }
        public object Default { get; }

        public Setting(string section, object def)
        {
            Section = section;
            Default = def;
        }
    }
}
