using System;

namespace USBHelperInjector
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
