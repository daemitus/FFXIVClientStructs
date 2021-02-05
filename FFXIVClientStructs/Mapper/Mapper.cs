using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using YamlDotNet.Serialization;

namespace FFXIVClientStructs.Mapper
{
    public static class Mapper
    {
        private static readonly long ProgramBaseAddress = Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64();
        private const long BaseAddress = 0x140000000;

        static Mapper()
        {
            var gameVersion = GetCurrentVersion();
            if (gameVersion != Game.Version)
            {
                var data = GetLocalData();
                if (data == null || data.Version != gameVersion)
                {
                    DownloadData();
                    data = GetLocalData();
                }
                if (data == null || data.Version != gameVersion)
                {
                    UpdateAddresses(ZeroOutAddress);
                }
                else
                {
                    UpdateAddresses(CalculateAddress);
                }
            }
            else
            {
                UpdateAddresses(CalculateAddress);
            }
        }

        private static string GetCurrentVersion()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            var gameDir = Path.GetDirectoryName(filePath);
            var versionPath = Path.Combine(gameDir, "ffxivgame.ver");
            var version = File.ReadAllText(versionPath).Trim();
            return version;
        }

        private static DataConstruct GetLocalData()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var dllDir = Path.GetDirectoryName(dllPath);
            var dataPath = Path.Combine(dllDir, "data.yml");
            if (!File.Exists(dataPath))
                return null;

            var dataText = File.ReadAllText(dataPath);
            var deserializer = new DeserializerBuilder().Build();
            var data = deserializer.Deserialize<DataConstruct>(dataText);
            return data;
        }

        private static void DownloadData()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var dllDir = Path.GetDirectoryName(dllPath);
            var dataPath = Path.Combine(dllDir, "data.yml");

            var web = new WebClient();  // TODO: Error checking
            web.DownloadFile("https://raw.githubusercontent.com/aers/FFXIVClientStructs/main/ida/data.yml", dataPath);
        }

        private static void UpdateAddresses(Func<long, IntPtr> getNewValue)
        {
            UpdateGlobals(getNewValue);
            UpdateFunctions(getNewValue);
            UpdateClasses(getNewValue);
        }

        private static void UpdateGlobals(Func<long, IntPtr> getNewValue) => UpdateSimple(typeof(Globals), getNewValue);

        private static void UpdateFunctions(Func<long, IntPtr> getNewValue) => UpdateSimple(typeof(Functions), getNewValue);

        private static void UpdateClasses(Func<long, IntPtr> getNewValue)
        {
            var nestedTypes = typeof(Classes).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (var nestedType in nestedTypes)
            {
                if (!nestedType.IsClass)
                    continue;

                if (nestedType.GetCustomAttribute<DataNameAttribute>() == null)
                    continue;

                UpdateSimple(nestedType, getNewValue);
            }
        }

        private static void UpdateSimple(Type staticType, Func<long, IntPtr> getNewValue)
        {
            var propInfos = staticType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var propInfo in propInfos)
            {
                if (propInfo.PropertyType != typeof(IntPtr))
                    continue;

                if (propInfo.GetCustomAttribute<DataNameAttribute>() == null)
                    continue;

                var currentValue = (IntPtr)propInfo.GetValue(null);
                propInfo.SetValue(null, getNewValue(currentValue.ToInt64()));
            }
        }

        private static IntPtr CalculateAddress(long address) => new IntPtr(address - BaseAddress + ProgramBaseAddress);

        private static IntPtr ZeroOutAddress(long _) => IntPtr.Zero;
    }

    public static partial class Game { }

    public static partial class Globals { }

    public static partial class Functions { }

    public static partial class Classes { }

    public class DataNameAttribute : Attribute
    {
        public string DataName { get; private set; }

        public DataNameAttribute(string name)
        {
            DataName = name;
        }
    }
}