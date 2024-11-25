using Microsoft.VisualBasic.FileIO;
using System.CommandLine;
using System.Globalization;


var bundleOption = new Option<FileInfo>("--output", "File path and name for the bundled file") { IsRequired = true };
bundleOption.AddAlias("-o");

var languageOption = new Option<string>("--languages", "File languages to bundle (e.g., cs,py,js or all)") { IsRequired = true };
languageOption.AddAlias("-l");


var includeSourcePathOption = new Option<bool>(  "--include-source-path","Include the source file path as a comment in the bundled file"){ IsRequired = false };
includeSourcePathOption.AddAlias ("-i");

var sortByOption = new Option<string>( "--sort-by",    "Sort files by 'name' (default) or 'type'. (e.g., 'name' or 'type')"){   IsRequired = false,};
sortByOption.AddAlias("-s");

var emptyLineOption = new Option<bool>( "--erase-empty-lines", "Erase the empty line from the file"){ IsRequired = false };
emptyLineOption.AddAlias("-e");

var authorOption = new Option<string>("--author", "Enter your name if you want the file to be in your name. ") { IsRequired = false, };
authorOption.AddAlias("-a");

var bundleCommand = new Command("bundle", "Bundle code files to a single file");


bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(includeSourcePathOption);
bundleCommand.AddOption(sortByOption);
bundleCommand.AddOption(emptyLineOption);
bundleCommand.AddOption(authorOption);
bundleCommand.SetHandler((output,  languages,  includeSourcePath,  SortBy,emptyLine , author) =>
{
    try
    {
        if (output == null || string.IsNullOrWhiteSpace(languages))
        {
            Console.WriteLine("Output file path and languages must be specified.");
            return;
        }

        // Define supported languages
        var supportedLanguages = new[] { "cs", "c", "cpp", "js", "jsx", "py", "java" };

        // Parse the input languages
        var selectedLanguages = languages.ToUpper() == "ALL"
       ? supportedLanguages // Select all supported languages
            : languages.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(lang => lang.Trim().ToLower())
                       .Where(lang => supportedLanguages.Contains(lang))
                       .ToArray();

        if (!selectedLanguages.Any())
        {
            Console.WriteLine("No valid languages selected.");
            return;
        }

        // Define the directory to search for files (current directory only)
        var directory = Directory.GetCurrentDirectory();

        // Collect files matching the selected languages in the current directory only
        var files = selectedLanguages.SelectMany(lang =>
            Directory.GetFiles(directory, $"*.{lang}", System.IO.SearchOption.TopDirectoryOnly)) // Exclude subdirectories
            .Distinct()
            .ToArray();

        if (!files.Any())
        {
            Console.WriteLine($"No files found for the specified languages: {string.Join(", ", selectedLanguages)}");
            return;
        }
         SortBy = string.IsNullOrWhiteSpace(SortBy) ? "name" : SortBy.ToLower();

        if (SortBy.ToLower() == "type")
        {
            // Sort by file type (extension)
            files = files.OrderBy(file => Path.GetExtension(file)).ThenBy(file => file).ToArray();
        }
        else
        {
            // Sort by file name (alphabetically)
            files = files.OrderBy(file => file).ToArray();
        }

        // Bundle files into the specified output file
        using (var outputStream = File.Create(output.FullName))
        using (var writer = new StreamWriter(outputStream))
        {
            if(author!=null)
            {
                writer.WriteLine($" {author}");
            }
            foreach (var file in files)
            {
                if(includeSourcePath)
                {
                    writer.WriteLine($"// Source of file: {file}");
                }
                if(emptyLine)
                {
                    foreach (var line in File.ReadLines(file))
                    {
                        //The line is not empty
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
                else
                {
                    writer.Write(File.ReadAllText(file));
                }
                writer.WriteLine();
            }
        }

        // Get the languages that actually had files
        var languagesWithFiles = selectedLanguages.Where(lang =>
            files.Any(file => file.EndsWith($".{lang}"))).ToArray();

        Console.WriteLine($"Bundled {files.Length} files ({string.Join(", ", languagesWithFiles)}) into {output.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, bundleOption, languageOption, includeSourcePathOption, sortByOption,emptyLineOption,authorOption);
var createRspCommand = new Command("create-rsp", "Create a response file with the full command");

createRspCommand.SetHandler(async () =>
{
    try
    {
        // בקשה מהמשתמש להזין ערכים עבור כל אפשרות
        Console.Write("Enter output file path: ");
        string output = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("Output file path is required.");

        Console.Write("Enter languages (e.g., cs, py, js): ");
        string languages = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(languages))
            throw new ArgumentException("Languages are required.");

        Console.Write("Include source path as comment? (y/n): ");
        string includeSourceInput = Console.ReadLine();
        bool includeSourcePath = includeSourceInput.ToLower() == "y";

        Console.Write("Sort by (name/type): ");
        string sortBy = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(sortBy))
            sortBy = "name"; // ברירת מחדל אם המשתמש לא בחר

        Console.Write("Erase empty lines? (y/n): ");
        string eraseEmptyLinesInput = Console.ReadLine();
        bool eraseEmptyLines = eraseEmptyLinesInput.ToLower() == "y";

        Console.Write("Enter your name for the author (optional): ");
        string author = Console.ReadLine();

        // בניית הפקודה המלאה
        string command = $" --output {output} --languages {languages}";

        if (includeSourcePath)
            command += " --include-source-path";

        if (!string.IsNullOrEmpty(sortBy))
            command += $" --sort-by {sortBy}";

        if (eraseEmptyLines)
            command += " --erase-empty-lines";

        if (!string.IsNullOrEmpty(author))
            command += $" --author {author}";

        // יצירת קובץ תגובה
        string responseFileName = "response.rsp";
        File.WriteAllText(responseFileName, command);

        Console.WriteLine($"Response file created: {responseFileName}");
    }
    catch (ArgumentException ex)
    {
        // טיפול בשגיאות כמו שדה חסר
        Console.WriteLine($"Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        // טיפול בשגיאות כלליות
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
});
var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
await rootCommand.InvokeAsync(args);
