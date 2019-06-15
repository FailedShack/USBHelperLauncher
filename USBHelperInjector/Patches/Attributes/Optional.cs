using System;

namespace USBHelperInjector.Patches.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class Optional : Attribute
    {
        public static bool IsOptional(Type type)
        {
            return GetCustomAttribute(type, typeof(Optional)) != null;
        }
    }
}
