using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace FFXIVClientStructs.Mapper
{
    public class DataConstruct
    {
        [YamlMember(Alias = "version")]
        public string Version;

        [YamlMember(Alias = "globals")]
        public Dictionary<long, string> Globals;

        [YamlMember(Alias = "functions")]
        public Dictionary<long, string> Functions;

        [YamlMember(Alias = "classes")]
        public Dictionary<string, ClassConstruct> Classes;
    }

    public class ClassConstruct
    {
        public string Name;

        [YamlMember(Alias = "inherits_from")]
        public string InheritsFrom;

        [YamlMember(Alias = "vtbl")]
        public long Vtbl;

        [YamlMember(Alias = "vfuncs")]
        public Dictionary<long, string> VtblFunctions;

        [YamlMember(Alias = "funcs")]
        public Dictionary<long, string> Functions;
    }
}
