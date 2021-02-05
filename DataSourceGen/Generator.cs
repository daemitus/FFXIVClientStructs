using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace DataSourceGen
{
    [Generator]
    public class GenerateFromData : ISourceGenerator
    {
        private DataConstruct Data;

        public void Initialize(GeneratorInitializationContext context)
        {
            // System.Diagnostics.Debugger.Launch();

            var dataPath = Path.Combine(Environment.CurrentDirectory, "..", "ida", "data.yml");
            var dataText = File.ReadAllText(dataPath);
            var deserializer = new DeserializerBuilder().Build();
            Data = deserializer.Deserialize<DataConstruct>(dataText);

            foreach (var kvp in Data.Classes)
                UpdateVtblFuncs(kvp.Value);
        }

        private void UpdateVtblFuncs(ClassConstruct cls)
        {
            if (cls == null || cls.Vtbl == 0x0)
                return;

            var parentName = cls.InheritsFrom;
            if (parentName != null && Data.Classes.TryGetValue(parentName, out var parent))
            {
                if (parent == null)
                    return;

                UpdateVtblFuncs(parent);

                if (parent.VtblFunctions == null)
                    return;

                if (cls.VtblFunctions == null)
                    cls.VtblFunctions = new Dictionary<long, string>();

                foreach (var kvp in parent.VtblFunctions)
                    cls.VtblFunctions[kvp.Key] = kvp.Value;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import("g_fmt", new Func<string, string>(FormatGlobalName));
            scriptObject.Import("fmt", new Func<string, string>(FormatName));
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
            return FormatName(name);
        }

        private string FormatName(string name)
        {
            var rex = new Regex("[^a-zA-Z0-9_]");
            name = name.Replace("::", "_");
            name = rex.Replace(name, "").Trim('_');
            name = name.First().ToString().ToUpper() + name.Substring(1);
            return name;
        }

        private const string BiggusTemplatus = @"
using System;

namespace FFXIVClientStructs.Mapper
{
    public static partial class Game
    {
        public static string Version { get; internal set; } = ""{{ data.version }}"";
    }

    public static partial class Globals
    {
        {% for kvp in data.globals %}
        {% assign name = kvp.value %}
        {% assign addr = kvp.key %}
        [DataName(""{{ name }}"")]
        public static IntPtr {{ name | g_fmt }} { get; internal set; } = new IntPtr({{ addr }});
        {% endfor %}
    }

    public static partial class Functions
    {
        {% for kvp in data.functions %}
        {% assign name = kvp.value %}
        {% assign addr = kvp.key %}
        [DataName(""{{ name }}"")]
        public static IntPtr {{ name | fmt }} { get; internal set; } = new IntPtr({{ addr }});
        {% endfor %}
    }

    public static partial class Classes
    {
        {% for cls_kvp in data.classes %}
        {% assign name = cls_kvp.key %}
        {% assign cls = cls_kvp.value %}
        [DataName(""{{ name }}"")]
        public static class {{ name | fmt }}
        {
            {% if cls %}

            {% if cls.vtbl_functions %}
            {% for kvp in cls.vtbl_functions %}
            {% assign name = kvp.value %}
            {% assign index = kvp.key %}
            [DataName(""{{ name }}"")]
            public static IntPtr {{ name | fmt }} { get; internal set; } = new IntPtr({{ cls.vtbl }} + {{ index }} * 8);
            {% endfor %}
            {% endif %}

            {% if cls.functions %}
            {% for kvp in cls.functions %}
            {% assign name = kvp.value %}
            {% assign addr = kvp.key %}
            [DataName(""{{ name }}"")]
            public static IntPtr {{ name | fmt }} { get; internal set; } = new IntPtr({{ addr }});
            {% endfor %}
            {% endif %}

            {% endif %}
        }
        {% endfor %}
    }
}
";
    }
}
