using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SharpBackup.App.Common;
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
        WindowHelpers.UpdateListBox(_folderListBox, _folderList);

        var filePaths = _config.GetConfigValue<string[]>("paths", "fileList") ?? [];
        foreach (var path in filePaths)
        {
            _fileList.Add(new FileInfo(path));
        }
        WindowHelpers.UpdateListBox(_fileListBox, _fileList);
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
            WindowHelpers.UpdateListBox(_folderListBox, _folderList);
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
            WindowHelpers.UpdateListBox(_fileListBox, _fileList);
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
                FileUtils.CopyDirectoryContents(folder, destinationFolderPath);
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

    private static void AddPathToList<T>(List<T> list, T path)
    {
        if (path != null && !list.Contains(path))
        {
            list.Add(path);
        }
    }
}