// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration.Templating.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private static readonly ConcurrentDictionary<string, MetadataReference> _metadataFileCache =
            new ConcurrentDictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        ILibraryManager libraryManager)
        {
            _environment = environment;
            _loader = loaderEngine;
            _libraryManager = libraryManager;
        }

        public CompilationResult Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };

            var references = GetApplicationReferences();

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    EmitResult result;

                    if (PlatformHelper.IsMono)
                    {
                        result = compilation.Emit(ms, pdbStream: null);
                    }
                    else
                    {
                        result = compilation.Emit(ms, pdbStream: pdb);
                    }

                    if (!result.Success)
                    {
                        var formatter = new DiagnosticFormatter();

                        var messages = result.Diagnostics
                                             .Where(IsError)
                                             .Select(d => formatter.Format(d));

                        return CompilationResult.Failed(content, messages);
                    }

                    Assembly assembly;
                    ms.Seek(0, SeekOrigin.Begin);

                    if (PlatformHelper.IsMono)
                    {
                        assembly = _loader.LoadStream(ms, pdbStream: null);
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = _loader.LoadStream(ms, pdb);
                    }

                    var type = assembly.GetExportedTypes()
                                       .First();

                    return CompilationResult.Successful(string.Empty, type);
                }
            }
        }

        //Todo: This is using application references to compile the template,
        //perhaps that's not right, we should use the dependencies of the caller
        //who calls the templating API.
        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            var export = _libraryManager.GetLibraryExport(_environment.ApplicationName);

            foreach (var metadataReference in export.MetadataReferences)
            {
                var fileMetadataReference = metadataReference as IMetadataFileReference;

                if (fileMetadataReference != null)
                {
                    references.Add(CreateMetadataFileReference(fileMetadataReference.Path));
                }
                else
                {
                    var roslynReference = metadataReference as IRoslynMetadataReference;

                    if (roslynReference != null)
                    {
                        references.Add(roslynReference.MetadataReference);
                    }
                }
            }

            return references;
        }

        private MetadataReference CreateMetadataFileReference(string path)
        {
            return _metadataFileCache.GetOrAdd(path, _ =>
            {
                // TODO: What about access to the file system? We need to be able to 
                // read files from anywhere on disk, not just under the web root
                using (var stream = File.OpenRead(path))
                {
                    return new MetadataImageReference(stream);
                }
            });
        }

        private bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}
