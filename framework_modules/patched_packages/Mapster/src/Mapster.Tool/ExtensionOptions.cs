﻿using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Mapster.Tool
{
    [Verb("extension", HelpText = "Generate extensions")]
    public class ExtensionOptions
    {
        [Option('a', "assembly", Required = true, HelpText = "Assembly to scan")]
        public string Assembly { get; set; }

        [Option('o', "output", Required = false, Default = "Models", HelpText = "Output directory.")]
        public string Output { get; set; }

        [Option('n', "namespace", Required = false, HelpText = "Namespace for extensions")]
        public string? Namespace { get; set; }

        [Option('p', "printFullTypeName", Required = false, HelpText = "Set true to print full type name")]
        public bool PrintFullTypeName { get; set; }

        [Option('b', "baseNamespace", Required = false, HelpText = "Provide base namespace to generate nested output & namespace")]
        public string? BaseNamespace { get; set; }

        [Option('s', "skipExisting", Required = false, HelpText = "Set true to skip generating already existing files")]
        public bool SkipExistingFiles { get; set; }

        [Option('N', "nullableDirective", Required = false, HelpText = "Set true to add \"#nullable enable\" to the top of generated extension files")]
        public bool GenerateNullableDirective { get; set; }

        [Usage(ApplicationAlias = "dotnet mapster extension")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Generate extensions", new MapperOptions
                {
                    Assembly = "/Path/To/YourAssembly.dll",
                    Output = "Models"
                })
            };
    }
}