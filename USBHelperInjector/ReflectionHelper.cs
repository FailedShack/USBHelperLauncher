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

        public static readonly Type[] Types = assembly.GetTypes();

        public static readonly Module MainModule = assembly.GetModule("WiiU_USB_Helper.exe");

        public static readonly MethodInfo EntryPoint = assembly.EntryPoint;

        public static readonly Type Settings = assembly.GetType("WIIU_Downloader.Properties.Settings");

        public static class NusGrabberForm
        {
            private static readonly Lazy<Type> _type = new Lazy<Type>(
                () => (from type in Types
                       where typeof(Form).IsAssignableFrom(type)
                       from prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                       where prop.Name == "Proxy"
                       select type).FirstOrDefault()
            );
            public static Type Type => _type.Value;

            private static readonly Lazy<ConstructorInfo> _constructor = new Lazy<ConstructorInfo>(
                () => Type.GetConstructor(Type.EmptyTypes)
            );
            public static ConstructorInfo Constructor => _constructor.Value;

            public static class Methods
            {
                private static readonly Lazy<MethodBase> _playGame = new Lazy<MethodBase>(
                    () => (from method in NusGrabberForm.Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                           where method.GetParameters().Length == 1
                              && method.GetParameters()[0].ParameterType == ReflectionHelper.TitleTypes.Game
                              && method.GetMethodBody().LocalVariables.Count == 2
                              && method.GetMethodBody().LocalVariables.Any(info => info.LocalType == ReflectionHelper.MainModule.GetType("NusHelper.DataSize"))
                           select method).FirstOrDefault()
                );

                public static MethodBase PlayGame => _playGame.Value;
            }
        }

        public static class FrmAskTicket
        {
            private static readonly Lazy<Type> _type = new Lazy<Type>(
                () => (from type in Types
                       where type.GetProperty("FileLocationWiiU") != null
                       select type).FirstOrDefault()
            );
            public static Type Type => _type.Value;

            private static readonly Lazy<MethodInfo> _okButtonHandler = new Lazy<MethodInfo>(
                () => (from method in Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                       let instructions = PatchProcessor.GetOriginalInstructions(method, out _)
                       where instructions.Any(x => x.opcode == OpCodes.Call && ((MethodInfo)x.operand).Name == "set_FileLocation3DS")
                          && instructions.Any(x => x.opcode == OpCodes.Ldfld && ((FieldInfo)x.operand).FieldType.Name == "RadTextBox")
                       select method).FirstOrDefault()
            );
            public static MethodInfo OkButtonHandler => _okButtonHandler.Value;

            private static readonly Lazy<List<FieldInfo>> _textBoxes = new Lazy<List<FieldInfo>>(
                () => (from instruction in PatchProcessor.GetOriginalInstructions(OkButtonHandler, out _)
                       where instruction.opcode == OpCodes.Ldfld
                       let field = (FieldInfo)instruction.operand
                       where field.FieldType.Name == "RadTextBox"
                       select field).ToList()
            );
            public static List<FieldInfo> TextBoxes => _textBoxes.Value;
        }

        public static class TitleTypes
        {
            private static readonly Lazy<Type> _game = new Lazy<Type>(
                () => (from type in Types
                       where type.GetProperty("Dlc", BindingFlags.Public | BindingFlags.Instance) != null
                       select type).FirstOrDefault()
            );
            public static Type Game => _game.Value;

            private static readonly Lazy<Type> _update = new Lazy<Type>(
                () => Game.GetProperty("Updates", BindingFlags.Public | BindingFlags.Instance).PropertyType.GenericTypeArguments[0]
            );
            public static Type Update => _update.Value;

            private static readonly Lazy<Type> _dlc = new Lazy<Type>(
                () => Game.GetProperty("Dlc", BindingFlags.Public | BindingFlags.Instance).PropertyType
            );
            public static Type Dlc => _dlc.Value;
        }
    }
}
