using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector
{
    class ReflectionHelper
    {
        private static readonly Assembly assembly = Assembly.Load("WiiU_USB_Helper");

        public static Type[] Types { get; } = assembly.GetTypes();

        public static Module MainModule { get; } = assembly.GetModule("WiiU_USB_Helper.exe");

        public static Type Settings
        {
            get
            {
                return assembly.GetType("WIIU_Downloader.Properties.Settings");
            }
        }

        public static Type NusGrabberForm
        {
            get
            {
                return (from type in assembly.GetTypes()
                        where typeof(Form).IsAssignableFrom(type)
                        from prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                        where prop.Name == "Proxy"
                        select type).FirstOrDefault();
            }
        }
    }
}
