using System;

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

        public Setting(string section) : this(section, null) { }
    }
}
