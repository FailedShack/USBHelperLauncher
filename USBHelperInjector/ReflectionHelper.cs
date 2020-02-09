using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;

namespace USBHelperInjector
{
    class ReflectionHelper
    {
        private static readonly Assembly assembly = Assembly.Load("WiiU_USB_Helper");

        public static Type[] Types { get; } = assembly.GetTypes();

        public static Module MainModule { get; } = assembly.GetModule("WiiU_USB_Helper.exe");

        public static MethodInfo EntryPoint { get; } = assembly.EntryPoint;

        public static Type Settings
        {
            get
            {
                return assembly.GetType("WIIU_Downloader.Properties.Settings");
            }
        }

        public static class NusGrabberForm
        {
            private static Type _type;
            public static Type Type
            {
                get
                {
                    if (_type == null)
                    {
                        _type = (from type in Types
                                 where typeof(Form).IsAssignableFrom(type)
                                 from prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                                 where prop.Name == "Proxy"
                                 select type).FirstOrDefault();
                    }
                    return _type;
                }
            }

            private static ConstructorInfo _constructor;
            public static ConstructorInfo Constructor
            {
                get
                {
                    if (_constructor == null)
                    {
                        _constructor = Type.GetConstructor(Type.EmptyTypes);
                    }
                    return _constructor;
                }
            }
        }

        public static class FrmAskTicket
        {
            private static Type _type;
            public static Type Type
            {
                get
                {
                    if (_type == null)
                    {
                        _type = (from type in Types
                                 where type.GetProperty("FileLocationWiiU") != null
                                 select type).FirstOrDefault();
                    }
                    return _type;
                }
            }

            private static MethodInfo _okButtonHandler;
            public static MethodInfo OkButtonHandler
            {
                get
                {
                    if (_okButtonHandler == null)
                    {
                        _okButtonHandler = (from method in Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                            let instructions = PatchProcessor.GetOriginalInstructions(method, out _)
                                            where instructions.Any(x => x.opcode == OpCodes.Call && ((MethodInfo)x.operand).Name == "set_FileLocation3DS")
                                            && instructions.Any(x => x.opcode == OpCodes.Ldfld && ((FieldInfo)x.operand).FieldType.Name == "RadTextBox")
                                            select method).FirstOrDefault();
                    }
                    return _okButtonHandler;
                }
            }

            private static List<FieldInfo> _textBoxes;
            public static List<FieldInfo> TextBoxes
            {
                get
                {
                    if (_textBoxes == null)
                    {
                        _textBoxes = (from instruction in PatchProcessor.GetOriginalInstructions(OkButtonHandler, out _)
                                      where instruction.opcode == OpCodes.Ldfld
                                      let field = (FieldInfo)instruction.operand
                                      where field.FieldType.Name == "RadTextBox"
                                      select field).ToList();
                    }
                    return _textBoxes;
                }
            }
        }
    }
}
