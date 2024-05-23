using System;
using System.IO;
using System.Linq;

namespace SharpBackup.App.Common;

public static class FileUtils
{
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

    public static void CopyDirectoryContents(DirectoryInfo sourceDir, string targetDir)
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
}