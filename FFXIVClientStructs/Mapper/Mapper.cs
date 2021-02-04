using System;
using System.Diagnostics;
using System.Reflection;

namespace FFXIVClientStructs.Mapper
{
    public static class Mapper
    {
        private static readonly long ProgramBaseAddress;
        private const long BaseAddress = 0x140000000;

        static Mapper()
        {
            var targetModule = Process.GetCurrentProcess().MainModule;
            ProgramBaseAddress = targetModule.BaseAddress.ToInt64();

            UpdateGlobals();
            UpdateFunctions();
            UpdateClasses();
        }

        private static void UpdateGlobals() => UpdateSimple(typeof(Globals));

        private static void UpdateFunctions() => UpdateSimple(typeof(Functions));

        private static void UpdateClasses() { }

        private static void UpdateSimple(Type staticType)
        {
            var propInfos = staticType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var propInfo in propInfos)
            {
                if (propInfo.PropertyType != typeof(IntPtr))
                    continue;

                var currentValue = (IntPtr)propInfo.GetValue(null);  // Because static classes cannot be instanced
                propInfo.SetValue(null, CalculateAddress(currentValue.ToInt64()));
            }
        }

        private static IntPtr CalculateAddress(long address)
        {
            if (ProgramBaseAddress == 0)
                throw new InvalidOperationException("Mapper is not initialized");

            return new IntPtr(address - BaseAddress + ProgramBaseAddress);
        }
    }

    public class DataNameAttribute : Attribute
    {
        public string DataName { get; private set; }

        public DataNameAttribute(string name)
        {
            DataName = name;
        }
    }

    public static partial class Globals { }

    public static partial class Functions { }

    public static partial class Classes { }
}