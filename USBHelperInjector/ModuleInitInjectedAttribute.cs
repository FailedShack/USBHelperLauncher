using System;

namespace USBHelperInjector
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleInitInjectedAttribute : Attribute
    {
        public string Version { get; }

        public ModuleInitInjectedAttribute(string version)
        {
            Version = version;
        }
    }
}
