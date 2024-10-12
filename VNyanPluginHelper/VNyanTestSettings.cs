using System;
using System.Collections.Generic;
using System.Text;
using VNyanInterface;
using UnityEngine;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    
    class VNyanTestSettings: MonoBehaviour, ISettingsInterface
    {
        private VNyanTestSettings _instance;
        private Dictionary<string, Dictionary<string, string>> pluginSettingsData = new Dictionary<string, Dictionary<string, string>>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void saveSettings(string fileName, Dictionary<string, string> newPluginSettingsData)
        {
            pluginSettingsData[fileName] = newPluginSettingsData;
        }

        public Dictionary<string, string> loadSettings(string fileName)
        {
            Dictionary<string, string> loadedPluginSettingsData;
            if (pluginSettingsData.TryGetValue(fileName, out loadedPluginSettingsData))
            {
                return loadedPluginSettingsData;
            }
            return new Dictionary<string, string>();
        }

    }
}
