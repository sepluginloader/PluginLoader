using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
                source.Add(new Source(mem, name, debugBuild));
            }
        }

        public byte[] Compile(string assemblyName, out byte[] symbols)
        {
            symbols = null;

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: source.Select(x => x.Tree),
                references: RoslynReferences.EnumerateAllReferences(),
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary, 
                    optimizationLevel: debugBuild ? OptimizationLevel.Debug : OptimizationLevel.Release));

            using (MemoryStream pdb = new MemoryStream())
            using (MemoryStream ms = new MemoryStream())
            {
                // write IL code into memory
                EmitResult result;
                if (debugBuild)
                {
                    result = compilation.Emit(ms, pdb,
                        embeddedTexts: source.Select(x => x.Text),
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: Path.ChangeExtension(assemblyName, "pdb")));
                }
                else
                {
                    result = compilation.Emit(ms);
                }
                 
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
                    if(debugBuild)
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        symbols = pdb.ToArray();
                    }
                    
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }

        }

        private class Source
        {
            public string Name { get; }
            public SyntaxTree Tree { get; }
            public EmbeddedText Text { get; }

            public Source(Stream s, string name, bool includeText)
            {
                Name = name;
                SourceText source = SourceText.From(s, canBeEmbedded: includeText);
                if (includeText)
                {
                    Text = EmbeddedText.FromSource(name, source);
                    Tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp7_3), name);
                }
                else
                {
                    Tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp7_3));
                }
            }
        }
    }

}
