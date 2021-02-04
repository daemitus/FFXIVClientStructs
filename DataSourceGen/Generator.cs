using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Scriban.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace DataSourceGen
{
    [Generator]
    public class GenerateFromData : ISourceGenerator
    {
        private DataConstruct Data;
        public void Initialize(GeneratorInitializationContext context)
        {
            var dataPath = Path.Combine(Environment.CurrentDirectory, "..", "ida", "data.yml");
            var dataText = File.ReadAllText(dataPath);
            var deserializer = new DeserializerBuilder().Build();
            Data = deserializer.Deserialize<DataConstruct>(dataText);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import("fmt_global", new Func<string, string>(FormatGlobalName));
            scriptObject.Import("fmt_func", new Func<string, string>(FormatFunctionName));
            scriptObject.Import("fmt_class", new Func<string, string>(FormatClassName));
            scriptObject.Add("data", Data);

            var templateContext = new TemplateContext();
            templateContext.PushGlobal(scriptObject);
            var templateText = Template.ParseLiquid(BiggusTemplatus).Render(templateContext);
            templateContext.PopGlobal();

            context.AddSource("FFXIVClientStructs.Mapper.Generated.cs", SourceText.From(templateText, Encoding.UTF8));
        }

        private string FormatGlobalName(string name)
        {
            var prefix = "g_";
            if (name.StartsWith(prefix))
                name = name.Substring(prefix.Length);
            return name;
        }

        private string FormatFunctionName(string name)
        {
            return name.Replace("::", "_");
        }

        private string FormatClassName(string name)
        {
            return name.Replace("::", "_").Replace("(", "").Replace(")", "").TrimEnd('_');
        }

        private const string BiggusTemplatus = @"
using System;

namespace FFXIVClientStructs.Mapper
{
    public static partial class Globals
    {
        {% for kvp in data.globals %}
        {% assign name = kvp.value %}
        {% assign addr = kvp.key %}
        [DataName(""{{ name }}"")]
        public static IntPtr {{ name | fmt_global }} { get; internal set; } = new IntPtr({{ addr }});
        {% endfor %}
    }

    public static partial class Functions
    {
        {% for kvp in data.functions %}
        {% assign name = kvp.value %}
        {% assign addr = kvp.key %}
        [DataName(""{{ name }}"")]
        public static IntPtr {{ name | fmt_func }} { get; internal set; } = new IntPtr({{ addr }});
        {% endfor %}
    }

    public static partial class Classes
    {
        {% for cls_kvp in data.classes %}
        {% assign cls_name = cls_kvp.key %}
        {% assign cls = cls_kvp.value %}
        [DataName(""{{ cls_name }}"")]
        public static class {{ cls_name | fmt_class }}
        {
            {% if cls.vtbl_functions %}
            {% for vfunc_kvp in cls.vtbl_functions %}
            {% assign vfunc_name = vfunc_kvp.value %}
            {% assign vfunc_index = vfunc_kvp.key %}
            [DataName(""{{ vfunc_name }}"")]
            public static IntPtr {{ vfunc_name | fmt_func }} { get; internal set; } = new IntPtr({{ cls.vtbl }} + {{ vfunc_index}} * 8);
            {% endfor %}
            {% endif %}

            {% if cls.functions %}
            {% for func_kvp in cls.functions %}
            {% assign func_name = func_kvp.value %}
            {% assign func_addr = func_kvp.key %}
            [DataName(""{{ func_name }}"")]
            public static IntPtr {{ func_name | fmt_func }} { get; internal set; } = new IntPtr({{ func_addr }});
            {% endfor %}
            {% endif %}
        }
        {% endfor %}
    }
}
";
    }
}

/*
            



       
 */