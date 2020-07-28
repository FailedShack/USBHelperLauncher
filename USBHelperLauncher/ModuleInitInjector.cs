using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.Linq;
using USBHelperInjector;

namespace USBHelperLauncher
{
    class ModuleInitInjector
    {
        private readonly string path;

        public ModuleInitInjector(string path)
        {
            this.path = path;
        }

        public bool RequiresInject(string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                return true;
            }
            try
            {
                using (var module = ModuleDefMD.Load(outputPath))
                {
                    // skip injection if file already exists and is up-to-date
                    var attr = module.Assembly.CustomAttributes.FirstOrDefault(
                        a => a.TypeFullName == typeof(ModuleInitInjectedAttribute).FullName
                    );
                    return attr == null || (attr.ConstructorArguments[0].Value as UTF8String).String != Program.GetVersion();
                }
            }
            catch
            {
                return true;
            }
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

                // add injected ModuleInitInjected attribute
                var injectAttrDef = typeof(ModuleInitInjectedAttribute).GetConstructor(new[] { typeof(string) });
                var injectAttrRef = module.Import(injectAttrDef);
                var newAttribute = new CustomAttribute(injectAttrRef as MemberRef, new[]
                {
                    new CAArgument(injectAttrRef.GetParam(0), Program.GetVersion())
                });
                module.Assembly.CustomAttributes.Add(newAttribute);

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
