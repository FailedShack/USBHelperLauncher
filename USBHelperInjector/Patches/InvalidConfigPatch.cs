using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using HarmonyLib;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class InvalidConfigPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.Settings.Type.GetConstructor(Type.EmptyTypes);
        }

        static void Postfix()
        {
            try
            {
                // try to load user.config
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            }
            catch (ConfigurationErrorsException e) when (e.GetBaseException() is XmlException)
            {
                // can't use `ConfigurationManager.OpenExeConfiguration` for getting the config path here,
                //  as it would preload the file and throw again
                var configPathsType = Assembly.GetAssembly(typeof(Configuration)).GetType("System.Configuration.ClientConfigPaths");
                var configPaths = AccessTools.Property(configPathsType, "Current").GetValue(null);
                var fileName = (string)AccessTools.Property(configPathsType, "LocalConfigFilename").GetValue(configPaths);
                File.Delete(fileName);
                MessageBox.Show("The configuration file has been corrupted. You'll need to go through the setup again.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
