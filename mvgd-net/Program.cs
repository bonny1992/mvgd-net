using System;
using System.IO;
using System.Linq;
using Spectre.Console;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string[] files = Directory.GetFiles(currentDirectory);

        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No files found in the current directory.[/]");
            return;
        }

        string[] fileNames = Array.ConvertAll(files, Path.GetFileName);

        var fileSelectionPrompt = new MultiSelectionPrompt<string>()
            .Title("Select files:")
            .PageSize(30)
            .MoreChoicesText("[grey](Move up and down to reveal more files)[/]")
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a file, " + 
                "[green]<enter>[/] to accept)[/]")
            .AddChoices(fileNames);

        var selectedFiles = AnsiConsole.Prompt(fileSelectionPrompt);
        var fileToFolderAssociations = new List<(string file, string folder)>();

        foreach (string selectedFile in selectedFiles)
        {
            if (!string.IsNullOrEmpty(selectedFile))
            {
                char firstLetter = char.ToUpper(selectedFile[0]);
                string destinationFolder = Path.Combine(@"/mnt/addons/merged-remotes/td-personal-bonny-home-union-crypt/Home/Other games", $"{firstLetter}");
                
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }
                
                var subDirectories = Directory.GetDirectories(destinationFolder);

                var folderChoices = subDirectories.Select(subDir => Path.GetFileName(subDir)).ToList();
                folderChoices.Add("[green]Create New Folder[/]");

                var folderSelectionPrompt = new SelectionPrompt<string>()
                    .Title($"Select a subdirectory for '{selectedFile}' or create a new one:")
                    .PageSize(30)
                    .MoreChoicesText("[grey](Move up and down to reveal more subdirectories)[/]")
                    .AddChoices(folderChoices);

                var selectedFolder = AnsiConsole.Prompt(folderSelectionPrompt);

                if (selectedFolder == "[green]Create New Folder[/]")
                {
                    string newFolderName = AnsiConsole.Prompt(new TextPrompt<string>("Enter a new folder name:"));

                    // Check if the folder name is valid for Windows, Linux, and macOS
                    if (!IsValidFolderName(newFolderName))
                    {
                        AnsiConsole.MarkupLine("[red]Invalid folder name.[/]");
                    }
                    else
                    {
                        if (AnsiConsole.Confirm($"Create new folder '{newFolderName}'?"))
                        {
                            string newFolderPath = Path.Combine(destinationFolder, newFolderName);
                            Directory.CreateDirectory(newFolderPath);
                            AnsiConsole.WriteLine($"Created new folder: {newFolderPath}");

                            fileToFolderAssociations.Add((selectedFile, newFolderPath));
                        }
                    }
                }
                else
                {
                    if (AnsiConsole.Confirm($"Selected subdirectory '{selectedFolder}', are you sure?"))
                    {
                        AnsiConsole.WriteLine($"Proceeding with selected subdirectory: {selectedFolder}");

                        string selectedFolderPath = Path.Combine(destinationFolder, selectedFolder);
                        fileToFolderAssociations.Add((selectedFile, selectedFolderPath));
                    }
                }
            }
        }
        AnsiConsole.Status()
            .Start("Moving...", ctx =>
            {
                foreach (var association in fileToFolderAssociations)
                {
                    string sourceFilePath = association.file;
                    string destinationFolderPath = association.folder;
                    string destinationFilePath = Path.Combine(destinationFolderPath, Path.GetFileName(sourceFilePath));

                    if (!Directory.Exists(destinationFolderPath))
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }

                    if (File.Exists(destinationFilePath))
                    {
                        if (AnsiConsole.Confirm($"The file '{Path.GetFileName(destinationFilePath)}' already exists. Do you want to overwrite it?"))
                        {
                            File.Delete(destinationFilePath);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"Skipped '{Path.GetFileName(sourceFilePath)}'");
                            continue; // Skip this file and move to the next iteration
                        }
                    }

                    AnsiConsole.MarkupLine($"Moving '{Path.GetFileName(sourceFilePath)}' to '{destinationFolderPath}'");
                    File.Move(sourceFilePath, destinationFilePath);
                }

                AnsiConsole.MarkupLine("Ended moving the files.");
            });
        // After selecting all folders, move the files based on the associations
        /*foreach (var association in fileToFolderAssociations)
        {
            MoveFile(association.file, association.folder);
        }*/
    }

    static bool IsValidFolderName(string folderName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return !folderName.Any(c => invalidChars.Contains(c));
    }

    //static void MoveFile(string sourceFilePath, string destinationFolderPath)
    //{
     //   AnsiConsole.Progress()
       //     .AutoRefresh(false)
         //   .AutoClear(false)
           // .HideCompleted(false)
            //.Columns(new ProgressColumn[] 
            //{
              //  new TaskDescriptionColumn(),
               // new ProgressBarColumn(),
                //new PercentageColumn(),
                //new RemainingTimeColumn(),
                //new SpinnerColumn(),
            //})
            //.Start(ctx =>
            //{
              //  var task = ctx.AddTask($"Moving '{Path.GetFileName(sourceFilePath)}' to '{destinationFolderPath}'");

                // Perform the file move and update the progress
                //File.Move(sourceFilePath, Path.Combine(destinationFolderPath, Path.GetFileName(sourceFilePath)));
                //task.Increment(100);
            //});
    //}*/
}
