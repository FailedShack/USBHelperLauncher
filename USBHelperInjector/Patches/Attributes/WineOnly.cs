using System;

namespace USBHelperInjector.Patches.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class WineOnly : Attribute
    {
        public static bool IsWineOnly(Type type)
        {
            return GetCustomAttribute(type, typeof(WineOnly)) != null;
        }
    }
}
