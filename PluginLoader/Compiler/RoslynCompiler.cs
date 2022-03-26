using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace avaness.PluginLoader.Compiler
{
    public class RoslynCompiler
    {
        private readonly List<Source> source = new List<Source>();
        private bool debugBuild;

        public RoslynCompiler(bool debugBuild = false)
        {
            this.debugBuild = debugBuild;
        }

        public void Load(Stream s, string name)
        {
            MemoryStream mem = new MemoryStream();
            using (mem)
            {
                s.CopyTo(mem);
                source.Add(new Source(mem, name));
            }
        }

        public byte[] Compile(string assemblyName)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
               assemblyName,
               syntaxTrees: source.Select(x => x.Tree),
               references: RoslynReferences.EnumerateAllReferences(),
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
               optimizationLevel: debugBuild ? OptimizationLevel.Debug : OptimizationLevel.Release));

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
                    {
                        Location location = diagnostic.Location;
                        Source source = this.source.FirstOrDefault(x => x.Tree == location.SourceTree);
                        LogFile.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} in file:\n{source?.Name ?? "null"} ({location.GetLineSpan().StartLinePosition})");
                    }
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


        private class Source
        {
            public string Name { get; }
            public SyntaxTree Tree { get; }

            public Source(Stream s, string name)
            {
                Name = name;
                Tree = CSharpSyntaxTree.ParseText(SourceText.From(s), new CSharpParseOptions(LanguageVersion.Latest));
            }
        }
    }

}
