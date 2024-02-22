using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FindUnusedTables
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = args.Length > 0 ? args[0] : throw new ArgumentException("Directory path not provided.");

            // Pass 1: Build a list of all table definitions
            var tableDefinitions = BuildTableDefinitions(directoryPath);

            // Debugging: Output all table definitions found
            foreach (var def in tableDefinitions)
            {
                Debug.WriteLine($"Table Definition Found: {def.Key} in {def.Value.FileName}, Context: {def.Value.Context}");
            }

            // Pass 2: Check for usage of each table
            var tableUsages = new HashSet<string>();
            CheckTableUsages(directoryPath, tableDefinitions, tableUsages);

            var unusedTables = tableDefinitions.Keys.Except(tableUsages);

            Console.WriteLine("Unused Tables, File and Context:");
            foreach (var table in unusedTables)
            {
                var definition = tableDefinitions[table];
                Console.WriteLine($"Table: {definition.TableName}\nFile: {definition.FileName}\nContext: {definition.Context}\n");
            }
        }

        private static Dictionary<string, TableDefinition> BuildTableDefinitions(string directoryPath)
        {
            var tableDefRegex = new Regex(@"^(\w+)\s+FILE,", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var tableDefinitions = new Dictionary<string, TableDefinition>();

            foreach (var filePath in Directory.GetFiles(directoryPath, "*.clw"))
            {
                var fileName = Path.GetFileName(filePath);
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    var processedLine = line.Split(new[] { '!', '|' })[0].Trim(); // Ignore comments and continuation
                    var match = tableDefRegex.Match(processedLine);
                    if (match.Success)
                    {
                        var tableName = match.Groups[1].Value; // Capture the table name
                        if (!tableDefinitions.ContainsKey(tableName))
                        {
                            tableDefinitions.Add(tableName, new TableDefinition(tableName, fileName, processedLine));
                        }
                    }
                }
            }

            return tableDefinitions;
        }

        private static void CheckTableUsages(string directoryPath, Dictionary<string, TableDefinition> tableDefinitions, HashSet<string> tableUsages)
        {
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.clw"))
            {
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    var processedLine = line.Split(new[] { '!', '|' })[0].Trim(); // Ignore comments and continuation
                                                                                  // Check if the line starts with a table name (which we want to exclude from being considered usage)
                    bool startsWithTableName = tableDefinitions.Any(td => processedLine.StartsWith(td.Key + " "));

                    if (!startsWithTableName) // Proceed only if the line does not start with a table name
                    {
                        var words = processedLine.Split(new[] { ' ', '(', ')', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var word in words)
                        {
                            if (tableDefinitions.ContainsKey(word) && !tableUsages.Contains(word))
                            {
                                tableUsages.Add(word);
                                Debug.WriteLine($"Table Usage Found: {word} in {filePath}, Line: {line}");
                            }
                        }
                    }
                }
            }
        }

    }

    class TableDefinition
    {
        public string TableName { get; set; }
        public string FileName { get; set; }
        public string Context { get; set; }

        public TableDefinition(string tableName, string fileName, string context)
        {
            TableName = tableName;
            FileName = fileName;
            Context = context;
        }
    }
}
