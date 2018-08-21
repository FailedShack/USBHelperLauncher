using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
