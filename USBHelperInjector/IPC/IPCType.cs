using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace USBHelperInjector.IPC
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IPCType
    {
        NamedPipe,
        TCP
    }
}
