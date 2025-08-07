using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages loading and validation of XML configuration files
    /// </summary>
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;
        public static ConfigurationManager Instance => _instance ?? throw new InvalidOperationException("ConfigurationManager not initialized");

        private readonly Dictionary<string, XDocument> _loadedConfigurations = new();

        /// <summary>
        /// Initializes the singleton instance
        /// </summary>
        public static void Initialize()
        {
            _instance ??= new ConfigurationManager();
        }

        /// <summary>
        /// Loads and caches an XML configuration file
        /// </summary>
        public XDocument LoadConfiguration(string path)
        {
            if (_loadedConfigurations.TryGetValue(path, out var cached))
                return cached;

            try
            {
                using var stream = TitleContainer.OpenStream(path);
                var doc = XDocument.Load(stream);
                _loadedConfigurations[path] = doc;
                return doc;
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException($"Failed to load configuration from {path}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that a configuration file exists and has required structure
        /// </summary>
        public void ValidateConfiguration(string path, string requiredRootElement)
        {
            var doc = LoadConfiguration(path);
            if (doc.Root?.Element(requiredRootElement) == null)
            {
                throw new InvalidDataException($"Configuration {path} missing required element: {requiredRootElement}");
            }
        }
    }
}