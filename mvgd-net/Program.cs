using System;
using System.IO;
using System.Linq;
using Spectre.Console;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

class Program
{
    static void Main(string[] args)
    {
        string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/mvgd/config.toml");

        if (!File.Exists(configFilePath))
        {
            AnsiConsole.MarkupLine("[yellow]Configuration file not found. Creating a default one...[/]");

            string configDirectory = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            var defaultConfig = new TomlTable
            {
                ["destinationDirectory"] = "/mnt/addons/merged-remotes/td-personal-bonny-home-union-crypt/Home/Other games"
            };

            File.WriteAllText(configFilePath, Toml.FromModel(defaultConfig));
        }

        string tomlContent = File.ReadAllText(configFilePath);
        var config = Toml.ToModel(tomlContent) as TomlTable;
        string destinationDirectory = config["destinationDirectory"] as string;

        if (string.IsNullOrEmpty(destinationDirectory))
        {
            AnsiConsole.MarkupLine("[red]Invalid destination directory in configuration file.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"Destination directory: [green]{destinationDirectory}[/]");

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
                string folderPath = Path.Combine(destinationDirectory, $"{firstLetter}");
                AnsiConsole.MarkupLine($"[green] Found folder: '{folderPath}'.[/]");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var subDirectories = Directory.GetDirectories(folderPath);
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

                    if (!IsValidFolderName(newFolderName))
                    {
                        AnsiConsole.MarkupLine("[red]Invalid folder name.[/]");
                    }
                    else
                    {
                        if (AnsiConsole.Confirm($"Create new folder '{newFolderName}'?"))
                        {
                            string newFolderPath = Path.Combine(folderPath, newFolderName);
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
                        string selectedFolderPath = Path.Combine(folderPath, selectedFolder);
                        fileToFolderAssociations.Add((selectedFile, selectedFolderPath));
                    }
                }
            }
        }

        // Sposta i file al di fuori del contesto di visualizzazione dello stato.
        AnsiConsole.MarkupLine("[yellow]Starting the file move operation...[/]");
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
                    continue;
                }
            }

            AnsiConsole.MarkupLine($"Moving '{Path.GetFileName(sourceFilePath)}' to '{destinationFolderPath}'");
            File.Move(sourceFilePath, destinationFilePath);
        }

        AnsiConsole.MarkupLine("[green]File move operation completed.[/]");
    }

    static bool IsValidFolderName(string folderName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return !folderName.Any(c => invalidChars.Contains(c));
    }
}