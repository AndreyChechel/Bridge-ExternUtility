using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BridgeExternUtility
{
    class Program
    {
        private static void Main()
        {
            const string FilePattern = "*.cs";
            string[] directories =
            {
                // ATTENTION:
                // Setup your local paths for Bridge and Bridge.Html5 projects
                // before running the Utility.

                @"D:\BridgeFork\Bridge",
                @"D:\BridgeFork\Html5"      // (!) Bridge.Html5 project should be configured to Suppress warning 0626
            };

            // ====================================================================================

            try
            {
                var stat = new Statistics(directories.Length);

                ProcessDirs(directories, FilePattern, stat);
                stat.PrintStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Can't process the specified directories. Please see the exception details below:");
                Console.WriteLine(ex.ToString());

                Console.WriteLine();
                Console.WriteLine("Press any key to exit..");
                Console.ReadKey();
            }
        }

        private static void ProcessDirs(string[] directories, string filePattern, Statistics stat)
        {

            foreach (var directory in directories)
            {
                var dirInfo = new DirectoryInfo(directory);
                var fileInfos = dirInfo.EnumerateFiles(filePattern, SearchOption.AllDirectories).ToList();
                stat.StartProcessingDir(dirInfo, fileInfos);

                foreach (var fileInfo in fileInfos)
                {
                    stat.StartProcessingFile(fileInfo.FullName);
                    stat.PrintStatus();

                    try
                    {
                        ProcessFile(fileInfo.FullName, stat);
                    }
                    catch
                    {
                        stat.ProcessingIsFailed(fileInfo.FullName);
                    }

                    stat.EndProcessingFile();
                }

                stat.EndProcessingDir();
            }

        }

        private static void ProcessFile(string fullName, Statistics stat)
        {
            var fileContent = File.ReadAllText(fullName);
            var fileSyntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = fileSyntaxTree.GetRoot();

            var updatedMethods = new Dictionary<BaseMethodDeclarationSyntax, BaseMethodDeclarationSyntax>();

            // Look at classes only:
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            foreach (var classDeclaration in classes)
            {
                var classModified = false;

                var methodAndOperators =
                    classDeclaration.DescendantNodes()
                        .OfType<BaseMethodDeclarationSyntax>()
                        .Where(x => x.Kind() == SyntaxKind.MethodDeclaration || x.Kind() == SyntaxKind.OperatorDeclaration)
                        .ToList();

                foreach (var method in methodAndOperators)
                {
                    var updatedMethod = method;
                    if (RoslynHelper.TransformEmptyMethodToExtern(ref updatedMethod))
                    {
                        updatedMethods[method] = updatedMethod;

                        stat.MethodWasModified();
                        classModified = true;
                    }
                }

                if (classModified)
                {
                    stat.ClassWasModified();
                }
            }

            // Modify the file if there are any changes
            if (updatedMethods.Count > 0)
            {
                root = root.ReplaceNodes(updatedMethods.Keys, (m1, m2) => updatedMethods[m1]);
                var updatedFileContent = root.ToFullString();
                File.WriteAllText(fullName, updatedFileContent);

                stat.FileWasModified();
            }
        }
    }
}
