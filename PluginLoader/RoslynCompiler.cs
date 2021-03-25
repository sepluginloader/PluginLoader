using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace avaness.PluginLoader
{
    public class RoslynCompiler
    {
        private readonly List<SyntaxTree> source = new List<SyntaxTree>();
        
        public void Load(Stream s)
        {
            source.Add(CSharpSyntaxTree.ParseText(SourceText.From(s)));
        }

        public byte[] Compile(LogFile log)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: source,
               references: GetRequiredRefernces().ToList(),
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                // write IL code into memory
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    // handle exceptions
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                        log.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} {diagnostic.Location.GetLineSpan().StartLinePosition}");
                    throw new Exception("Compilation failed!");
                }
                else
                {
                    // load this 'virtual' DLL so that we can use
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }

        }

        private static IEnumerable<MetadataReference> GetRequiredRefernces()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies().Where(IsValidReference))
                yield return MetadataReference.CreateFromFile(a.Location);
        }

        private static bool IsValidReference(Assembly a)
        {
            return !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location);
        }
    }

}
