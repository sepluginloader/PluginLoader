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
        private static Dictionary<string, AssemblyReference> allReferences = new Dictionary<string, AssemblyReference>();

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
                        if (!ContainsReference(name) && TryLoadAssembly(name, out Assembly aRef))
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
            return allReferences.TryGetValue(name.Name, out AssemblyReference refs) && refs.Contains(name);
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
            if (allReferences.TryGetValue(name, out AssemblyReference refs))
                refs.Add(a);
            else
                allReferences.Add(name, new AssemblyReference(a));
        }

        public static IEnumerable<MetadataReference> EnumerateAllReferences()
        {
            foreach (AssemblyReference refs in allReferences.Values)
            {
                foreach (MetadataReference reference in refs)
                    yield return reference;
            }
        }

        private static bool IsValidReference(Assembly a)
        {
            return !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location);
        }

        public static void LoadReferences(Stream s)
        {
            // Source: https://stackoverflow.com/a/28694200
            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument projDefinition = XDocument.Load(s);
            IEnumerable<string> references = projDefinition
                .Element(msbuild + "Project")
                .Elements(msbuild + "ItemGroup")
                .Elements(msbuild + "Reference")
                .Attributes("Include")    // This is where the reference is mentioned       
                .Select(refElem => refElem.Value);
            foreach (string reference in references)
                LoadReference(reference);
        }

        private static void LoadReference(string name)
        {
            try
            {
                AssemblyName aName = new AssemblyName(name);
                if (!allReferences.ContainsKey(aName.Name))
                {
                    Assembly a = Assembly.Load(aName);
                    LogFile.WriteLine("Reference added at runtime: " + a.FullName);
                    AssemblyReference aRef = new AssemblyReference(a);
                    allReferences[a.GetName().Name] = aRef;
                }
            }
            catch (IOException)
            {
                LogFile.WriteLine("WARNING: Unable to find the assembly '" + name + "'!");
            }
        }

        private class AssemblyReference : IEnumerable<MetadataReference>
        {
            public readonly Dictionary<string, MetadataReference> fullNames = new Dictionary<string, MetadataReference>();

            public AssemblyReference(Assembly a)
            {
                Add(a);
            }

            public void Add(Assembly a)
            {
                fullNames[a.GetName().Version.ToString()] = MetadataReference.CreateFromFile(a.Location);
            }

            public bool Contains(AssemblyName name)
            {
                return fullNames.ContainsKey(name.Version.ToString());
            }

            public IEnumerator<MetadataReference> GetEnumerator()
            {
                return fullNames.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return fullNames.Values.GetEnumerator();
            }
        }
    }
}
