using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace SharpBackup.App.Common;

public class Configuration
{
    private readonly FileInfo _configFilePath = new(Path.Combine(Environment.CurrentDirectory, "config.xml"));

    private void CreateConfigFile()
    {
        try
        {
            XmlDocument xmlDoc = new();
            XmlElement root = xmlDoc.CreateElement("Configuration");
            xmlDoc.AppendChild(root);

            xmlDoc.Save(_configFilePath.FullName);

            Console.WriteLine($"Config file created at {_configFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating config file: {ex.Message}");
        }
    }

    private XmlNode? GetConfigValue(string xmlCategory, string xmlKey)
    {
        try
        {
            if (!File.Exists(_configFilePath.FullName))
            {
                return default;
            }

            XmlDocument xmlDoc = new();
            xmlDoc.Load(_configFilePath.FullName);

            XmlNode? configNode = xmlDoc.SelectSingleNode($"/Configuration/{xmlCategory}/{xmlKey}");
            if (configNode == null)
            {
                return default;
            }

            return configNode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting config value: {ex.Message}");
            return default;
        }
    }

    public string[]? GetConfigValueRange(string xmlCategory, string xmlKey)
    {
        return GetConfigValue(xmlCategory, xmlKey)?.InnerText.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    public string? GetConfigValueString(string xmlCategory, string xmlKey)
    {
        return GetConfigValue(xmlCategory, xmlKey)?.InnerText;
    }

    public void SaveToConfigFile(string xmlCategory, string xmlKey, object value)
    {
        try
        {
            XmlDocument xmlDoc = new();

            if (!File.Exists(_configFilePath.FullName))
            {
                CreateConfigFile();
                xmlDoc.Load(_configFilePath.FullName);
            }
            else
            {
                xmlDoc.Load(_configFilePath.FullName);
            }

            XmlNode? root = xmlDoc.DocumentElement;
            XmlNode? categoryNode = root?.SelectSingleNode(xmlCategory);
            if (categoryNode == null)
            {
                categoryNode = xmlDoc.CreateElement(xmlCategory);
                root?.AppendChild(categoryNode);
            }

            XmlNode? keyNode = categoryNode.SelectSingleNode(xmlKey);
            if (keyNode == null)
            {
                keyNode = xmlDoc.CreateElement(xmlKey);
                categoryNode.AppendChild(keyNode);
            }

            keyNode.InnerText = value switch
            {
                string stringValue => stringValue,
                string[] arrayValue => string.Join(";", arrayValue),
                IEnumerable<DirectoryInfo> directoryArray => string.Join(";",
                    directoryArray.Select(dir => dir.FullName)),
                IEnumerable<FileInfo> fileArray => string.Join(";", fileArray.Select(file => file.FullName)),
                _ => keyNode.InnerText
            };

            xmlDoc.Save(_configFilePath.FullName);

            Console.WriteLine($"Value '{value}' saved to {_configFilePath} under category '{xmlCategory}' with key '{xmlKey}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config file: {ex.Message}");
        }
    }
}
