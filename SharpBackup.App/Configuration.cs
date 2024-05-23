using System;
using System.IO;
using System.Xml;

namespace SharpBackup.App;

public class Configuration
{
    private readonly FileInfo _configFilePath = new(Path.Combine(Environment.CurrentDirectory, "config.xml"));

    private void CreateConfigFile()
    {
        try
        {
            var xmlDoc = new XmlDocument();
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

    public T? GetConfigValue<T>(string xmlCategory, string xmlKey)
    {
        try
        {
            if (!File.Exists(_configFilePath.FullName))
            {
                return default;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(_configFilePath.FullName);

            XmlNode? configNode = xmlDoc.SelectSingleNode($"/Configuration/{xmlCategory}/{xmlKey}");
            if (configNode == null)
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)configNode.InnerText;
            }

            if (typeof(T) == typeof(string[]))
            {
                return (T)(object)configNode.InnerText.Split(';');
            }

            throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting config value: {ex.Message}");
            return default;
        }
    }

    public void SaveToConfigFile(string xmlCategory, string xmlKey, object value)
    {
        try
        {
            var xmlDoc = new XmlDocument();

            if (!File.Exists(_configFilePath.FullName))
            {
                CreateConfigFile();
                xmlDoc.Load(_configFilePath.FullName);
            }
            else
            {
                xmlDoc.Load(_configFilePath.FullName);
            }

            var root = xmlDoc.DocumentElement ?? xmlDoc.AppendChild(xmlDoc.CreateElement("Configuration"));
            var configNode = root?.SelectSingleNode(xmlCategory) ?? root.AppendChild(xmlDoc.CreateElement(xmlCategory));

            var existingKeyNode = configNode?.SelectSingleNode(xmlKey);
            if (existingKeyNode != null)
            {
                configNode?.RemoveChild(existingKeyNode);
            }

            var keyElement = xmlDoc.CreateElement(xmlKey);
            if (value is string stringValue)
            {
                keyElement.InnerText = stringValue;
            }
            else if (value is string[] arrayValue)
            {
                keyElement.InnerText = string.Join(";", arrayValue);
            }

            configNode?.AppendChild(keyElement);
            xmlDoc.Save(_configFilePath.FullName);

            Console.WriteLine($"Value '{value}' saved to {_configFilePath} under category '{xmlCategory}' with key '{xmlKey}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config file: {ex.Message}");
        }
    }
}