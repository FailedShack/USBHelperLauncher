using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    public abstract class EmulatorConfiguration
    {
        public enum Emulator
        {
            Citra, Cemu, Project64, Snes9x, Dolphin, DeSmuME, Fceux, VisualBoyAdvance
        }

        private static Dictionary<Emulator, EmulatorConfiguration> emulators = new Dictionary<Emulator, EmulatorConfiguration>()
        {
            { Emulator.Citra, new CitraConfiguration() },
            { Emulator.Cemu, new CemuConfiguration() },
            { Emulator.Project64, new Project64Configuration() },
            { Emulator.Snes9x, new Snes9xConfiguration() },
            { Emulator.Dolphin, new DolphinConfiguration() },
            { Emulator.DeSmuME, new DesmumeConfiguration() },
            { Emulator.Fceux, new FceuxConfiguration() },
            { Emulator.VisualBoyAdvance, new VbaConfiguration() }
        };

        private string name;
        protected Dictionary<string, GetPackage> versions = new Dictionary<string, GetPackage>();
        protected List<GetPackage> extensions = new List<GetPackage>();

        public EmulatorConfiguration(string name)
        {
            this.name = name;
        }

        public string GetName()
        {
            return name;
        }

        public delegate Task<Package> GetPackage();

        public Dictionary<string, GetPackage> GetVersions()
        {
            return versions;
        }

        public List<GetPackage> GetExtensions()
        {
            return extensions;
        }

        public static EmulatorConfiguration GetConfiguration(Emulator emulator)
        {
            return emulators[emulator];
        }
    }
}
