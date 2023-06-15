using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

namespace Geex.TemplateGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("项目和组织信息默认识别camel case. eg. geex=>Geex, bms=>Bms");
            Console.WriteLine("enter org name.");
            var orgName = Console.ReadLine().Trim();
            Console.WriteLine("enter project name.");
            var projName = Console.ReadLine().Trim();
            Console.WriteLine("enter module name.");
            var moduleName = Console.ReadLine().Trim();
            Console.WriteLine("enter default aggregate name.");
            var aggregateName = Console.ReadLine().Trim();
            Console.WriteLine("enter template path, press enter if it's `simple_module`");
            var templateName = Console.ReadLine();
            if (string.IsNullOrEmpty(templateName))
            {
                templateName = "simple_module";
            }

            var orgC = orgName.Camelize();
            var orgP = orgName.Pascalize();

            var projC = projName.Camelize();
            var projP = projName.Pascalize();

            var moduleC = moduleName.Camelize();
            var moduleP = moduleName.Pascalize();

            var aggregateC = aggregateName.Camelize();
            var aggregateP = aggregateName.Pascalize();

            var cwd = Directory.GetCurrentDirectory();
            var target = Path.Combine(cwd, $".generated/{orgP}.{projP}.{moduleP}");
            var templatePath = "../../../templates/" + templateName;
            var renameEntries = Directory.GetFileSystemEntries(templatePath, "*", SearchOption.AllDirectories);
            renameEntries = renameEntries
                .Where(x => !x.Contains("\\.vs\\"))
                .Where(x => !x.Contains("\\bin\\Debug\\"))
                .Where(x => !x.Contains("\\obj\\"))
                .Where(x => !x.Contains("\\.submodules")).ToArray();
            var total = renameEntries.Length;
            Parallel.ForEach(renameEntries, (renameEntry, _, index) =>
            {
                var destEntry = renameEntry.Replace(templatePath, target)
                    .Replace("x_org_x", orgC)
                    .Replace("x_proj_x", projC)
                    .Replace("x_mod_x", moduleC)
                    .Replace("x_aggregate_x", aggregateC)
                    .Replace("x_Org_x", orgP)
                    .Replace("x_Proj_x", projP)
                    .Replace("x_Mod_x", moduleP)
                    .Replace("x_Aggregate_x", aggregateP)
                    .Replace("_org_", orgName)
                    .Replace("_proj_", projName)
                    .Replace("_mod_", moduleName)
                    .Replace("_aggregate_", aggregateName)
                    ;
                if (File.Exists(renameEntry))
                {
                    var srcFile = new FileInfo(renameEntry);
                    var destFile = new FileInfo(destEntry);
                    if (!destFile.Directory.Exists)
                    {
                        destFile.Directory.Create();
                    }
                    using var sr = srcFile.OpenText();
                    using var sw = destFile.CreateText();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        sw.WriteLine(line
                            .Replace("x_org_x", orgC)
                            .Replace("x_proj_x", projC)
                            .Replace("x_mod_x", moduleC)
                            .Replace("x_aggregate_x", aggregateC)
                            .Replace("x_Org_x", orgP)
                            .Replace("x_Proj_x", projP)
                            .Replace("x_Mod_x", moduleP)
                            .Replace("x_Aggregate_x", aggregateP)
                            .Replace("_org_", orgName)
                            .Replace("_proj_", projName)
                            .Replace("_mod_", moduleName)
                            .Replace("_aggregate_", aggregateName));
                    }
                }
                else
                {
                    Directory.CreateDirectory(destEntry);
                }
                Console.WriteLine($"{index}/{total}");
            });
        }
    }
}
