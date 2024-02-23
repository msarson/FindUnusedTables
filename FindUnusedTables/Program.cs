using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace YourAppName
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No directories provided. Exiting.");
                return;
            }
            
            var directoryPath = Path.GetFullPath(args[0]);
            Console.WriteLine($"Checking existence of directory: {directoryPath}");
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory not found: {directoryPath}");
                return;
            }
            
            var cts = new CancellationTokenSource();

            var spinnerTask = Task.Run(() => Spinner(cts.Token));
            

           

            Console.WriteLine($"Using directory: {directoryPath}");

            // Pass 1: Build a list of all table definitions and their prefixes
            string tableDefinitionsDirectory = directoryPath;
            var tableDefinitions = BuildTableDefinitions(tableDefinitionsDirectory);
            

            // Initialize a collection to track table and prefix usages.
            var tableAndPrefixUsages = new HashSet<string>();
         //   CheckTableAndPrefixUsages(directoryPath, tableDefinitions, tableAndPrefixUsages);
            var paths = args.Select(arg => Path.GetFullPath(arg)).ToList();


            // Iterate over all provided directories, including the first one, for usage search.
            foreach (var directoryPaths in paths)
            {
                if (!Directory.Exists(directoryPaths))
                {
                    continue;
                }
                CheckTableAndPrefixUsages(directoryPaths, tableDefinitions, tableAndPrefixUsages);
            }

            var unusedTables = tableDefinitions.Keys.Except(tableAndPrefixUsages, StringComparer.OrdinalIgnoreCase);

            var sortedUnusedTables = unusedTables.OrderBy(name => name).ToList();

            Console.WriteLine("Unused Tables, File and Context:");
            foreach (var tableName in sortedUnusedTables)
            {
                var definition = tableDefinitions[tableName];
                Console.WriteLine($"Table: {definition.TableName} (Prefix: {definition.Prefix})\nFile: {definition.FileName}\nContext: {definition.Context}\n");

                // Writing to the file would follow the same logic
            }

            // After printing the unused tables to the console
            string resultFilePath = Path.Combine(directoryPath, "UnusedTablesResults.txt");
            using (StreamWriter writer = new StreamWriter(resultFilePath))
            {
                writer.WriteLine("Unused Tables, File and Context:");
                Console.WriteLine("Also writing results to: " + resultFilePath);

                foreach (var tableName in sortedUnusedTables)
                {
                    var definition = tableDefinitions[tableName];
                    string resultLine = $"Table: {definition.TableName} (Prefix: {definition.Prefix})\nFile: {definition.FileName}\nContext: {definition.Context}\n";
                    writer.WriteLine(resultLine);
                }
            }
            //End of program, cancel and clear spinner and exit.
            cts.Cancel();
            try
            {
                spinnerTask.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is TaskCanceledException); // Handle the cancellation exception.
            }
            finally
            {
                cts.Dispose();
            }

        }

        private static Dictionary<string, TableDefinition> BuildTableDefinitions(string directoryPath)
        {
            var tableDefinitions = new Dictionary<string, TableDefinition>();
            var regex = new Regex(@"^(\w+)\s+FILE,.*?PRE\((\w+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (var filePath in Directory.GetFiles(directoryPath, "*.clw"))
            {
                // Skip files matching the _BCx.clw pattern
                if (Regex.IsMatch(Path.GetFileName(filePath), @"_BC\d+\.clw$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(filePath);
                foreach (var line in File.ReadLines(filePath))
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        var tableName = match.Groups[1].Value;
                        var prefix = match.Groups[2].Value + ":";
                        if (!tableDefinitions.ContainsKey(tableName))
                        {
                            tableDefinitions.Add(tableName, new TableDefinition(tableName, prefix, fileName, line));
                        }
                    }
                }
            }

            return tableDefinitions;
        }

        private static void CheckTableAndPrefixUsages(string directoryPath, Dictionary<string, TableDefinition> tableDefinitions, HashSet<string> tableAndPrefixUsages)
        {
            // Patterns that represent valid table usage scenarios in Clarion, considering case insensitivity.
            //       var usagePatterns = new Regex(@"\b(?:ADD|PUT|DELETE|GET|NEXT|RELATE|TRYINSERT|TRYUPDATE|TRYFETCH|TRYDELETE|FETCH|INSERT|UPDATE|DELETE|NEXT)\s*\(\s*(\w+)|Access:(\w+)\.\w+\(\)",
            //                        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var usagePatterns = new Regex(
                @"\b(?:ADD|PUT|DELETE|GET|NEXT|RELATE|TRYINSERT|TRYUPDATE|TRYFETCH|TRYDELETE|FETCH|INSERT|UPDATE|DELETE|NEXT|APPEND|ERASE|CLEAR|OPEN|RECORDS|CLOSE)\s*\(\s*(\w+)|" +
                @"Access:(\w+)\.\w+\(\)|" +
                @"\b(GET|APPEND|ERASE|CLEAR|OPEN|RECORDS|CLOSE)\s*\(\s*(\w+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);




            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.clw"))
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    // Skip empty lines or lines starting directly with a comment.
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("!"))
                    {
                        continue;
                    }

                    // Process the line to handle comments.
                    var lineBeforeComment = line.Split(new[] { '!' }, 2)[0];

                    // Ensure the line doesn't start in the first column.
                    if (!lineBeforeComment.StartsWith(" "))
                    {
                        continue;
                    }
                    
                    var match = usagePatterns.Match(lineBeforeComment);
                    if (match.Success)
                    {
                        
                        string tableName = "";

                        // Check the first capturing group from the first part of the pattern
                        if (match.Groups[1].Success)
                        {
                            tableName = match.Groups[1].Value;
                        }
                        // The second capturing group is intended for the Access:TableName part
                        else if (match.Groups[2].Success)
                        {
                            tableName = match.Groups[2].Value;
                        }
                        // The fourth capturing group (which corresponds to the third part of the pattern due to the non-capturing group at the start)
                        else if (match.Groups[3].Success) // Adjusted to group 3 based on the current pattern structure
                        {
                            tableName = match.Groups[3].Value;
                        }

                        if (!string.IsNullOrEmpty(tableName))
                        {
                            
                            tableAndPrefixUsages.Add(tableName); // Add the found table name to usages
                        }
                    }

                }
            }
        }


        static void Spinner()
        {
            var spinnerChars = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;

            while (true) // You'll need a condition to stop this loop based on your processing completion
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(spinnerChars[spinnerIndex++ % spinnerChars.Length]);
                Thread.Sleep(100);
            }
        }

        static void Spinner(CancellationToken token)
        {
            var spinnerChars = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;

            while (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(spinnerChars[spinnerIndex++ % spinnerChars.Length]);
                Thread.Sleep(100);
            }
            Console.Write('\r'); // Clear the spinner character when done.
        }



        class TableDefinition
        {
            public string TableName { get; set; }
            public string Prefix { get; set; }
            public string FileName { get; set; }
            public string Context { get; set; }

            public TableDefinition(string tableName, string prefix, string fileName, string context)
            {
                TableName = tableName;
                Prefix = prefix;
                FileName = fileName;
                Context = context;
            }
        }
    }
}
