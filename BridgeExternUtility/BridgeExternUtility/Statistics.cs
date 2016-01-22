using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BridgeExternUtility
{
    internal class Statistics
    {
        private readonly List<string> _failedFiles = new List<string>();

        private string CurrentDir { get; set; }

        private string CurrentFile { get; set; }

        private int DirsProcessed { get; set; }

        private int FilesProcessedInCurrDir { get; set; }

        private int TotalDirsCount { get; }

        private int TotalFilesProcessed { get; set; }

        private int TotalFilesInCurrDir { get; set; }

        private int TotalFilesModified { get; set; }

        private int TotalClassesModified { get; set; }

        private int TotalMethodsModified { get; set; }

        private int TotalOperatorsModified { get; set; }

        public Statistics(int totalDirsCount)
        {
            TotalDirsCount = totalDirsCount;
        }

        public void StartProcessingDir(DirectoryInfo dir, List<FileInfo> files)
        {
            CurrentDir = dir.FullName;
            TotalFilesInCurrDir = files.Count;
        }

        public void StartProcessingFile(string fileName)
        {
            CurrentFile = fileName;
        }

        public void EndProcessingFile()
        {
            ++FilesProcessedInCurrDir;
            ++TotalFilesProcessed;
        }

        public void EndProcessingDir()
        {
            ++DirsProcessed;
            FilesProcessedInCurrDir = 0;
        }

        public void MethodWasModified()
        {
            ++TotalMethodsModified;
        }

        public void OperatorWasModified()
        {
            ++TotalOperatorsModified;
        }

        public void ClassWasModified()
        {
            ++TotalClassesModified;
        }

        public void FileWasModified()
        {
            ++TotalFilesModified;
        }

        public void ProcessingIsFailed(string fileName)
        {
            _failedFiles.Add(fileName);
        }

        public void PrintStatus()
        {
            Console.Clear();
            Console.WriteLine($"{DirsProcessed}/{TotalDirsCount} directories have been processed.");
            Console.WriteLine($"{FilesProcessedInCurrDir}/{TotalFilesInCurrDir} files have been processed.");

            Console.WriteLine();
            Console.WriteLine($"Current dir: {CurrentDir}");
            Console.WriteLine($"Current file: {CurrentFile}");
            Thread.Sleep(10);
        }

        public void PrintStatistics()
        {
            Console.Clear();
            Console.WriteLine("Done!");
            Console.WriteLine();

            Console.WriteLine("Statistics:");
            Console.WriteLine($"1. Modified files: \t{TotalFilesModified}");
            Console.WriteLine($"2. Modified classes: \t{TotalClassesModified}");
            Console.WriteLine($"3. Modified methods: \t{TotalMethodsModified}");
            Console.WriteLine($"4. Modified operators: \t{TotalOperatorsModified}");

            if (_failedFiles.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("The next files were not processed due errors:");
                for (int i = 0; i < _failedFiles.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {_failedFiles[i]}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit..");
            Console.ReadKey();
        }
    }
}