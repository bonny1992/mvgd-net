using System;
using System.IO;
using System.Linq;
using Spectre.Console;
using System.Collections.Generic;
using Tomlyn; // Libreria per gestire i file TOML
using Tomlyn.Model; // Modello di dati per la lettura e scrittura TOML


class Program
{
    static void Main(string[] args)
    {
        
        // Path del file di configurazione
        string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/mvgd/config.toml");

        // Se il file di configurazione non esiste, crealo con valori di default
        if (!File.Exists(configFilePath))
        {
            AnsiConsole.MarkupLine("[yellow]Configuration file not found. Creating a default one...[/]");

            // Creazione della directory se non esiste
            string configDirectory = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // Scrittura di un file TOML di default
            var defaultConfig = new TomlTable
            {
                ["destinationDirectory"] = "/mnt/addons/merged-remotes/td-personal-bonny-home-union-crypt/Home/Other games"
            };

            File.WriteAllText(configFilePath, Toml.FromModel(defaultConfig));
        }

        // Lettura del file di configurazione
        string tomlContent = File.ReadAllText(configFilePath);
        var config = Toml.ToModel(tomlContent) as TomlTable;
        
        // Estrai il valore della chiave "destinationDirectory"
        string destinationDirectory = config["destinationDirectory"] as string;

        if (string.IsNullOrEmpty(destinationDirectory))
        {
            AnsiConsole.MarkupLine("[red]Invalid destination directory in configuration file.[/]");
            return;
        }

        // Mostra la directory di destinazione
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
                string destinationFolder = Path.Combine(destinationDirectory, $"{firstLetter}");
                
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
