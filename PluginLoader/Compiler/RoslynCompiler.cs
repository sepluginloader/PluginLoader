﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace avaness.PluginLoader.Compiler
{
    public class RoslynCompiler
    {
        private readonly List<Source> source = new List<Source>();

        public void Load(Source source)
        {
            this.source.Add(source);
        }

        public byte[] Compile()
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: source.Select(x => x.Tree),
               references: RoslynReferences.EnumerateAllReferences(),
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

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


        public class Source
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