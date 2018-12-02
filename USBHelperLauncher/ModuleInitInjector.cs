using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher
{
    class ModuleInitInjector
    {
        private string path;

        public ModuleInitInjector(string path)
        {
            this.path = path;
        }

        public void Inject(string outputPath)
        {
            using (var module = ModuleDefMD.Load(path))
            {
                // find module class
                var moduleClass = module.Types.FirstOrDefault(x => x.Name == "<Module>");

                // find (or create) static constructor
                var cctor = moduleClass.Methods.FirstOrDefault(x => x.Name == ".cctor");
                if (cctor == null)
                {
                    var attributes = MethodAttributes.Private
                                     | MethodAttributes.HideBySig
                                     | MethodAttributes.Static
                                     | MethodAttributes.SpecialName
                                     | MethodAttributes.RTSpecialName;
                    cctor = new MethodDefUser(".cctor", MethodSig.CreateStatic(module.CorLibTypes.Void), attributes);
                    moduleClass.Methods.Add(cctor);
                }

                // add call to our dll
                var usbHelperInjector = ModuleDefMD.Load("USBHelperInjector.dll");
                var testMethodDef = usbHelperInjector
                    .Types.First(t => t.FullName == "USBHelperInjector.InjectorService")
                    .Methods.First(m => m.Name == "Init");
                var testMethodRef = module.Import(testMethodDef);

                if (cctor.Body == null)
                {
                    cctor.Body = new CilBody();
                    cctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(testMethodRef));
                    cctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                }
                else
                {
                    cctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(testMethodRef));
                }

                // write new file
                var options = new ModuleWriterOptions(module);
                options.MetadataOptions.PreserveHeapOrder(module, true);
                options.MetadataOptions.Flags |=
                    MetadataFlags.AlwaysCreateBlobHeap | MetadataFlags.AlwaysCreateGuidHeap | MetadataFlags.AlwaysCreateStringsHeap | MetadataFlags.AlwaysCreateUSHeap
                    | MetadataFlags.KeepOldMaxStack | MetadataFlags.PreserveAll;
                module.Write(outputPath, options);
            }
        }
    }
}
