# FindUnusedTables

## Overview
`FindUnusedTables` is a console application designed to identify unused tables within Clarion codebases. The development of this tool was inspired by discussions in the Clarion Live Skype Chat and was realized with the assistance of ChatGPT. It aims to assist developers in assessing which tables might be redundant and could potentially be removed from the dictionary. However, users are advised to proceed with caution when deciding to remove tables.

## Usage

To use `FindUnusedTables`, execute the command with the path to your source code containing table definitions, followed by any additional paths where you wish to search for table usages:

    FindUnusedTables.EXE PathToSourceCodeWithTableDefinitions ExtraPathsToSearch

### Example

    FindUnusedTables.exe c:\development\myApplication\src c:\development\SharedLibraries

This command initiates a search for `.clw` files within the specified source code directory to build a unique list of tables. It then searches all `.clw` files in the provided folders, including the initial source code directory, applying various rules to detect table usages. These rules are detailed within the repository's code.


## Detailed Usage Analysis

The `FindUnusedTables` application identifies table usages by searching for specific patterns within `.clw` files. The program looks for instances where tables are interacted with through a variety of standard Clarion operations. Recognized patterns include direct table operations, as well as those accessed via the `Access:` syntax. Below is a breakdown of the patterns and operations the program searches for:

### Direct Table Operations

The application detects the following operations that directly involve table names:

- Standard operations: `ADD`, `PUT`, `DELETE`, `GET`, `NEXT`, `RELATE`, `APPEND`, `ERASE`, `CLEAR`, `OPEN`, `RECORDS`, `CLOSE`
- Conditional operations with prefixes: `TRYINSERT`, `TRYUPDATE`, `TRYFETCH`, `TRYDELETE`

### Access Operations

Additionally, the program identifies usage patterns where tables are accessed through Clarion's `Access:` syntax, followed by an operation:

- Example: `Access:TableName.Fetch()`, `Access:TableName.Update()`, and other operations as listed above.

### Operation Patterns

The regex pattern captures operations followed by parentheses, indicating function calls or method invocations on table objects, which might include, but are not limited to:

- Operations directly invoking a table name, e.g., `OPEN(TableName)`
- `Access:` syntax operations, indicating a more complex interaction with the table, e.g., `Access:TableName.Delete()`

The analysis is case-insensitive and compiled for efficient execution across large codebases, ensuring a comprehensive search through all specified directories for potential table usages.

## Output

Following the analysis, `FindUnusedTables` outputs a list of tables for which it cannot find usages directly in the console. It also generates a file named `UnusedTablesResults.txt` in the execution directory, containing the same list of potentially unused tables.

## Disclaimer

The output should be used as a guideline. Users are advised to perform thorough reviews and testing to validate the results before making changes to their codebase.
