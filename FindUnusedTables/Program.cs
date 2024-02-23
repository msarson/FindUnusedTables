using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FindUnusedTables
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
            var paths = args.Select(arg => Path.GetFullPath(arg)).ToList();

            Console.WriteLine($"Checking existence of directory: {paths[0]}");
            if (!Directory.Exists(paths[0]))
            {
                Console.WriteLine($"Directory not found: {paths[0]}");
                return;
            }

            var cts = new CancellationTokenSource();
            //var spinnerTask = Task.Run(() => Spinner(cts.Token));
            var spinnerTask = Task.Run(() => NightRiderBar(cts.Token));
            Console.WriteLine($"Using directory: {paths[0]}");

            // Pass 1: Build a list of all table definitions and their prefixes

            var tableDefinitions = BuildTableDefinitions(paths[0]);

            // Initialize a collection to track table and prefix usages.
            var tableAndPrefixUsages = new HashSet<string>();
            //   CheckTableAndPrefixUsages(directoryPath, tableDefinitions, tableAndPrefixUsages);


            // Iterate over all provided directories, including the first one, for usage search.
            foreach (var directoryPaths in paths)
            {
                if (!Directory.Exists(directoryPaths))
                {
                    continue;
                }
                CheckTableAndPrefixUsages(directoryPaths, tableDefinitions, tableAndPrefixUsages);
            }

            EndNightRiderBar(cts, spinnerTask);

            var unusedTables = tableDefinitions.Keys.Except(tableAndPrefixUsages, StringComparer.OrdinalIgnoreCase);

            var sortedUnusedTables = unusedTables.OrderBy(name => name).ToList();

            OutputResultsToConsole(tableDefinitions, sortedUnusedTables);

            // After printing the unused tables to the console
            OutputResultsToFile(paths, tableDefinitions, sortedUnusedTables);
            //End of program, cancel and clear spinner and exit.


        }

        private static void OutputResultsToFile(List<string> paths, Dictionary<string, TableDefinition> tableDefinitions, List<string> sortedUnusedTables)
        {
            string resultFilePath = Path.Combine(paths[0], "UnusedTablesResults.txt");
            using (StreamWriter writer = new StreamWriter(resultFilePath))
            {
                // Calculate maximum lengths for each field
                int maxTableNameLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].TableName.Length).Max();
                int maxPrefixLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].Prefix.Length).Max();
                int maxFileNameLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].FileName.Length).Max();
                int maxContextLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].Context.Length).Max();

                // Ensure minimum column width to accommodate headers
                maxTableNameLength = Math.Max(maxTableNameLength, "Table".Length);
                maxPrefixLength = Math.Max(maxPrefixLength, "Prefix".Length);
                maxFileNameLength = Math.Max(maxFileNameLength, "File".Length);
                maxContextLength = Math.Max(maxContextLength, "Context".Length);

                // Write headers
                writer.WriteLine($"{"Table".PadRight(maxTableNameLength)}  {"Prefix".PadRight(maxPrefixLength)}  {"File".PadRight(maxFileNameLength)}  {"Context".PadRight(maxContextLength)}");
                writer.WriteLine($"{new string('-', maxTableNameLength)}  {new string('-', maxPrefixLength)}  {new string('-', maxFileNameLength)}  {new string('-', maxContextLength)}");

                // Write rows
                foreach (var tableName in sortedUnusedTables)
                {
                    var definition = tableDefinitions[tableName];
                    writer.WriteLine($"{definition.TableName.PadRight(maxTableNameLength)}  {definition.Prefix.PadRight(maxPrefixLength)}  {definition.FileName.PadRight(maxFileNameLength)}  {definition.Context.PadRight(maxContextLength)}");
                }

                Console.WriteLine("Also writing results to: " + resultFilePath);
            }
        }


        private static void OutputResultsToConsole(Dictionary<string, TableDefinition> tableDefinitions, List<string> sortedUnusedTables)
        {
            // Determine the maximum width for each column for alignment
            int maxTableNameLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].TableName.Length).Max();
            int maxPrefixLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].Prefix.Length).Max();
            int maxFileNameLength = sortedUnusedTables.Select(tableName => tableDefinitions[tableName].FileName.Length).Max();

            // Ensure minimum column width to accommodate headers
            maxTableNameLength = Math.Max(maxTableNameLength, "Table".Length);
            maxPrefixLength = Math.Max(maxPrefixLength, "Prefix".Length);
            maxFileNameLength = Math.Max(maxFileNameLength, "File".Length);

            // Header
            Console.WriteLine($"{"Table".PadRight(maxTableNameLength)}  {"Prefix".PadRight(maxPrefixLength)}  {"File".PadRight(maxFileNameLength)}");
            Console.WriteLine($"{new string('-', maxTableNameLength)}  {new string('-', maxPrefixLength)}  {new string('-', maxFileNameLength)}");

            // Rows
            foreach (var tableName in sortedUnusedTables)
            {
                var definition = tableDefinitions[tableName];
                Console.WriteLine($"{definition.TableName.PadRight(maxTableNameLength)}  {definition.Prefix.PadRight(maxPrefixLength)}  {definition.FileName.PadRight(maxFileNameLength)}");
            }
        }


        private static void EndNightRiderBar(CancellationTokenSource cts, Task spinnerTask)
        {
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

        /*
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
*/

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
                    // Skip lines containing EXTERNAL or DLL attributes
                    if (line.Contains("EXTERNAL") || line.Contains("DLL"))
                    {
                        continue;
                    }

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

        //because why not :D
        static void NightRiderBar(CancellationToken token, int barLength = 20)
        {
            // Variables for the current index and direction of movement (1 for right, -1 for left)
            int position = 0;
            int direction = 1;

            while (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                for (int i = 0; i < barLength; i++)
                {
                    // Display a white block at the current position, else a hashed block
                    if (i == position)
                        Console.Write(' '); // 'Light' character
                    else
                        Console.Write('█'); // Background character
                }

                // Update the position for the next frame
                position += direction;

                // Reverse direction if we hit the end or start of the bar
                if (position == 0 || position == barLength - 1)
                    direction *= -1;

                Thread.Sleep(40); // Control the speed of the animation
            }

            // Clear the bar when done
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', barLength));
            Console.Write('\r');
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
