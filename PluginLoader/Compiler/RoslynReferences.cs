using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace avaness.PluginLoader.Compiler
{
    public static class RoslynReferences
    {
        private static Dictionary<string, MetadataReference> allReferences = new Dictionary<string, MetadataReference>();

        public static void GenerateAssemblyList()
        {
            if (allReferences.Count > 0)
                return;

            Stack<Assembly> loadedAssemblies = new Stack<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(IsValidReference));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            string line = "===================================";
            sb.AppendLine(line);
            sb.AppendLine("Assembly References");
            sb.AppendLine(line);

            try
            {
                foreach (Assembly a in loadedAssemblies)
                {
                    AddAssemblyReference(a);
                    sb.AppendLine(a.FullName);
                }
                sb.AppendLine(line);
                while (loadedAssemblies.Count > 0)
                {
                    Assembly a = loadedAssemblies.Pop();

                    foreach (AssemblyName name in a.GetReferencedAssemblies())
                    {
                        if (!ContainsReference(name) && TryLoadAssembly(name, out Assembly aRef) && !aRef.IsDynamic)
                        {
                            AddAssemblyReference(aRef);
                            sb.AppendLine(name.FullName);
                            loadedAssemblies.Push(aRef);
                        }
                    }
                }
                sb.AppendLine(line);
            }
            catch (Exception e)
            {
                sb.Append("Error: ").Append(e).AppendLine();
            }

            LogFile.WriteLine(sb.ToString(), false);
        }

        private static bool ContainsReference(AssemblyName name)
        {
            return allReferences.ContainsKey(name.Name);
        }

        private static bool TryLoadAssembly(AssemblyName name, out Assembly aRef)
        {
            try
            {
                aRef = Assembly.Load(name);
                return true;
            }
            catch (IOException)
            {
                aRef = null;
                return false;
            }
        }

        private static void AddAssemblyReference(Assembly a)
        {
            string name = a.GetName().Name;
            if (!allReferences.ContainsKey(name))
                allReferences.Add(name, MetadataReference.CreateFromFile(a.Location));
        }

        public static IEnumerable<MetadataReference> EnumerateAllReferences()
        {
            return allReferences.Values;
        }

        private static bool IsValidReference(Assembly a)
        {
            return !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location);
        }

        public static void LoadReference(string name)
        {
            try
            {
                AssemblyName aName = new AssemblyName(name);
                if (!allReferences.ContainsKey(aName.Name))
                {
                    Assembly a = Assembly.Load(aName);
                    LogFile.WriteLine("Reference added at runtime: " + a.FullName);
                    MetadataReference aRef = MetadataReference.CreateFromFile(a.Location);
                    allReferences[a.GetName().Name] = aRef;
                }
            }
            catch (IOException)
            {
                LogFile.WriteLine("WARNING: Unable to find the assembly '" + name + "'!");
            }
        }
    }
}
