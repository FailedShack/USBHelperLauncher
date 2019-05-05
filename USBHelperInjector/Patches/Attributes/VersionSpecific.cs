using System;

namespace USBHelperInjector.Patches.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class VersionSpecific : Attribute
    {
        public string From { get; }
        public string To { get; }

        public VersionSpecific(string from, string to)
        {
            From = from;
            To = to;
        }

        public VersionSpecific(string version) : this(version, version) { }

        public static bool Applies(Type type, string version)
        {
            var range = (VersionSpecific)GetCustomAttribute(type, typeof(VersionSpecific));
            return range == null ? true : version.CompareTo(range.From) >= 0 && version.CompareTo(range.To) <= 0;
        }
    }
}
