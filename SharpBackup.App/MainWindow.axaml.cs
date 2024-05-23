using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SharpBackup.App.Windows;

namespace SharpBackup.App;

public partial class MainWindow : Window
{
    private readonly List<DirectoryInfo> _folderList = [];
    private readonly List<FileInfo> _fileList = [];

    private DirectoryInfo? _selectedFolderPath;
    private FileInfo? _selectedFilePath;

    private DirectoryInfo? _selectedBackupPath;
    private readonly TextBox? _backupPathBox;

    private readonly ListBox? _folderListBox, _fileListBox;

    private readonly Configuration _config = new();

    public MainWindow()
    {
        InitializeComponent();
        _folderListBox = this.FindControl<ListBox>("FolderListBox");
        _fileListBox = this.FindControl<ListBox>("FileListBox");
        _backupPathBox = this.FindControl<TextBox>("BackupLocationOption");

        if (_folderListBox != null) ScrollViewer.SetHorizontalScrollBarVisibility(_folderListBox, ScrollBarVisibility.Auto);
        if (_fileListBox != null) ScrollViewer.SetHorizontalScrollBarVisibility(_fileListBox, ScrollBarVisibility.Auto);

        ApplyConfigurations();
    }

    private void ApplyConfigurations()
    {
        _backupPathBox!.Text = _config.GetConfigValue<string>("backup", "backupFolder") ?? @"C:\Backup";

        var folderPaths = _config.GetConfigValue<string[]>("paths", "folderList") ?? [];
        foreach (var path in folderPaths)
        {
            _folderList.Add(new DirectoryInfo(path));
        }
        UpdateListBox(_folderListBox, _folderList);

        var filePaths = _config.GetConfigValue<string[]>("paths", "fileList") ?? [];
        foreach (var path in filePaths)
        {
            _fileList.Add(new FileInfo(path));
        }
        UpdateListBox(_fileListBox, _fileList);
    }

    private async void OnAddFolderClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folderResult = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder to Backup"
            });

        if (folderResult.Count > 0)
        {
            _selectedFolderPath = new DirectoryInfo(folderResult[0].Path.LocalPath);
            AddPathToList(_folderList, _selectedFolderPath);
            UpdateListBox(_folderListBox, _folderList);
            _config.SaveToConfigFile("paths", "folderList", _folderList.ToArray());
        }
        else
        {
            _selectedFolderPath = null;
        }
    }

    private async void OnAddFileClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> fileResult = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "Select File to Backup"
            });

        if (fileResult.Count > 0)
        {
            _selectedFilePath = new FileInfo(fileResult[0].Path.LocalPath);
            AddPathToList(_fileList, _selectedFilePath);
            UpdateListBox(_fileListBox, _fileList);
            _config.SaveToConfigFile("paths", "fileList", _fileList.ToArray());
        }
        else
        {
            _selectedFilePath = null;
        }
    }

    private async void SelectBackupLocation(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> backupFolder = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Backup Folder"
            });

        if (backupFolder.Count > 0)
        {
            _selectedBackupPath = new DirectoryInfo(backupFolder[0].Path.LocalPath);
            WindowHelpers.UpdateTextBox(_backupPathBox, _selectedBackupPath.FullName);
            _config.SaveToConfigFile("backup", "backupFolder", _selectedBackupPath.FullName);
        }
        else
        {
            _selectedFolderPath = null;
        }
    }

    private void OnBackupClick(object sender, RoutedEventArgs e)
    {
        var backupDirectory = new DirectoryInfo(_config.GetConfigValue<string>("backup", "backupFolder") ?? @"C:\Backup");

        if (!backupDirectory.Exists)
        {
            backupDirectory.Create();
        }

        try
        {
            foreach (var folder in _folderList)
            {
                var destinationFolderPath = Path.Combine(backupDirectory.FullName, folder.Name);
                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }
                CopyDirectoryContents(folder, destinationFolderPath);
            }

            foreach (var file in _fileList)
            {
                var destinationFilePath = Path.Combine(backupDirectory.FullName, file.Name);
                file.CopyTo(destinationFilePath, true);
            }

            var infoWindow = new InfoWindow
            {
                InfoText = "Backup completed with no errors."
            };
            infoWindow.Show();
        }
        catch (Exception exc)
        {
            Console.WriteLine("Error during backup: " + exc.Message);
        }
    }

    private static void CopyDirectoryContents(DirectoryInfo sourceDir, string targetDir)
    {
        if (!sourceDir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
        }

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in sourceDir.GetFiles())
        {
            var tempPath = Path.Combine(targetDir, file.Name);
            if (!File.Exists(tempPath))
            {
                file.CopyTo(tempPath, false);
                Console.WriteLine($"File '{file.Name}' copied to '{tempPath}'.");
            }
            else
            {
                var sourceBytes = File.ReadAllBytes(file.FullName);
                var destinationBytes = File.ReadAllBytes(tempPath);

                if (ByteArraysAreEqual(sourceBytes, destinationBytes))
                {
                    Console.WriteLine($"File '{file.Name}' already exists in destination and is identical. Skipping.");
                }
                else
                {
                    RenameFile(tempPath);
                    file.CopyTo(tempPath, false);
                    Console.WriteLine($"File '{file.Name}' copied to '{tempPath}' after renaming existing file.");
                }

            }
        }

        foreach (var subDir in sourceDir.GetDirectories())
        {
            var tempPath = Path.Combine(targetDir, subDir.Name);
            CopyDirectoryContents(subDir, tempPath);
        }
    }

    private static bool ByteArraysAreEqual(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length)
            return false;

        return !array1.Where((t, i) => t != array2[i]).Any();
    }

    private static void RenameFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var fileExtension = Path.GetExtension(filePath);
        var directory = Path.GetDirectoryName(filePath)!;

        var newFileName = $"{fileName}_{DateTime.Now:yyyy-MM-dd_HH-mm}{fileExtension}";
        var newFilePath = Path.Combine(directory, newFileName);

        File.Move(filePath, newFilePath);
        Console.WriteLine($"File '{Path.GetFileName(filePath)}' renamed to '{Path.GetFileName(newFilePath)}'.");
    }

    private static void UpdateListBox<T>(ListBox? listBox, List<T> list)
    {
        if (listBox == null) return;
        listBox.Items.Clear();
        foreach (var item in list)
        {
            listBox.Items.Add(item);
        }
    }

    private static void AddPathToList<T>(List<T> list, T path)
    {
        if (path != null && !list.Contains(path))
        {
            list.Add(path);
        }
    }
}