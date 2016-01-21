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
            const string filePattern = "*.cs";
            var directories = new[]
            {
                // ATTENTION:
                // Setup your local paths for Bridge and Bridge.Html5 projects
                // before running the Utility

                @"D:\BridgeFork\Bridge",
                @"D:\BridgeFork\Html5"      // (!) Bridge.Html5 project should be configured to Suppress warning 0626
            };

            int dirsProcessed = 0;
            foreach (var directory in directories)
            {
                var dirInfo = new DirectoryInfo(directory);
                var fileInfos = dirInfo.EnumerateFiles(filePattern, SearchOption.AllDirectories).ToList();

                int filesProcessed = 0;
                foreach (var fileInfo in fileInfos)
                {
                    Console.WriteLine($"{dirsProcessed}/{directories.Length} directories have been processed.");
                    Console.WriteLine($"{filesProcessed}/{fileInfos.Count} files have been processed.");

                    Console.WriteLine();
                    Console.WriteLine($"Current dir: {directory}");
                    Console.WriteLine($"Current file: {fileInfo.FullName}");

                    ProcessFile(fileInfo.FullName);

                    ++filesProcessed;
                    Console.Clear();
                }

                ++dirsProcessed;
            }

            Console.WriteLine("All files have been processed.");
            Console.ReadKey();
        }

        private static void ProcessFile(string fullName)
        {
            var fileContent = File.ReadAllText(fullName);
            var fileSyntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = fileSyntaxTree.GetRoot();

            var updatedMethods = new Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax>();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            foreach (var classDeclaration in classes)
            {
                var methods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
                foreach (var method in methods)
                {
                    var updatedMethod = method;
                    if (RoslynHelper.TransformEmptyMethodToExtern(ref updatedMethod))
                    {
                        updatedMethods[method] = updatedMethod;
                    }
                }
            }

            if (updatedMethods.Count > 0)
            {
                root = root.ReplaceNodes(updatedMethods.Keys, (m1, m2) => updatedMethods[m1]);
                var updatedFileContent = root.ToFullString();
                File.WriteAllText(fullName, updatedFileContent);
            }
        }
    }
}
