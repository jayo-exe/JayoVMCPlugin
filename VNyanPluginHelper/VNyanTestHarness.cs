using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VNyanInterface;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    internal class VNyanTestHarness : MonoBehaviour
    {
        private Dictionary<string, Dictionary<string, string>> pluginSettingsData = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> VNyanStringParameters = new Dictionary<string, string>();
        private Dictionary<string, float> VNyanFloatParameters = new Dictionary<string, float>();
        private Dictionary<string, IButtonClickedHandler> registeredPlugins = new Dictionary<string, IButtonClickedHandler>();

        private GameObject canvasObject;
        private GameObject buttonHolderObject;
        private GameObject stringParameterBox;
        private GameObject floatParameterBox;
        private MainThreadDispatcher mainThread;

        private DefaultControls.Resources uiResources;

        private void Awake()
        {
            mainThread = gameObject.AddComponent<MainThreadDispatcher>();
            canvasObject = GameObject.Find("__VNyanTestHarness/__VNyanTestCanvas");
            buttonHolderObject = GameObject.Find("__VNyanTestHarness/__VNyanTestCanvas/__VNyanPluginButtonHolder");
            stringParameterBox = GameObject.Find("__VNyanTestHarness/__VNyanTestCanvas/__VNyanParameterHolder/__VNyanStringParameterScroll/Viewport/Content");
            floatParameterBox = GameObject.Find("__VNyanTestHarness/__VNyanTestCanvas/__VNyanParameterHolder/__VNyanFloatParameterScroll/Viewport/Content");

            uiResources = new DefaultControls.Resources();
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name == "UISprite")
                {
                    uiResources.standard = sprite;
                    break;
                }
            }
        }

        public void registerPluginButton(string buttonText, IButtonClickedHandler pluginInstance)
        {
            registeredPlugins[buttonText] = pluginInstance;
            refreshButtonDisplay();
        }

        public void refreshButtonDisplay()
        {

            int buttonWidth = 160;
            int buttonHeight = 24;
            int gap = 12;
            int gapOffset = 0;

            buttonHolderObject.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, gap, (gap + gap + ((gap + buttonHeight) * registeredPlugins.Keys.Count)));
            buttonHolderObject.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, gap, gap + gap + buttonWidth);

            foreach (string key in registeredPlugins.Keys)
            {
                GameObject pluginButton = DefaultControls.CreateButton(uiResources);
                pluginButton.name = $"__VNyanPluginButton {key}";
                pluginButton.transform.SetParent(buttonHolderObject.transform);
                pluginButton.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, gap, buttonWidth);
                pluginButton.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, gap + gapOffset, buttonHeight);
                pluginButton.GetComponentInChildren<Text>().text = key;
                pluginButton.GetComponent<Button>().onClick.AddListener(registeredPlugins[key].pluginButtonClicked);
                gapOffset += (gap + buttonHeight);
                Debug.Log($"gap offset set to {gapOffset}");
            }
        }

        public void refreshParameterDisplay()
        {

            mainThread.Enqueue(() => {
                int itemWidth = 160;
                int itemHeight = 14;
                int gap = 6;
                int gapOffset = 0;
                GameObject parameterItem;

                
                gapOffset = ((gap + (itemHeight * 2)) * (stringParameterBox.transform.childCount));
                foreach (string key in VNyanStringParameters.Keys)
                {
                
                    parameterItem = GameObject.Find($"__VNyanStringParameterText-{key}");
                    if(!parameterItem)
                    {
                        parameterItem = DefaultControls.CreateText(uiResources);
                        parameterItem.name = $"__VNyanStringParameterText-{key}";
                        parameterItem.transform.SetParent(stringParameterBox.transform);
                        parameterItem.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, gap, itemWidth);
                        parameterItem.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, gap + gapOffset, (itemHeight * 2));
                        parameterItem.GetComponentInChildren<Text>().fontSize = 12;
                        gapOffset += (gap + (itemHeight * 2));
                        stringParameterBox.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, (gap + gapOffset));
                    }

                    parameterItem.GetComponentInChildren<Text>().text = $"{key}:\n{VNyanStringParameters[key]}";
                }

                
                gapOffset = ((gap + itemHeight) * (floatParameterBox.transform.childCount));
                foreach (string key in VNyanFloatParameters.Keys)
                {
                    parameterItem = GameObject.Find($"__VNyanFloatParameterText-{key}");
                    if (!parameterItem)
                    {
                        parameterItem = DefaultControls.CreateText(uiResources);
                        parameterItem.name = $"__VNyanFloatParameterText-{key}";
                        parameterItem.transform.SetParent(floatParameterBox.transform);
                        parameterItem.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, gap, itemWidth);
                        parameterItem.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, gap + gapOffset, itemHeight);
                        parameterItem.GetComponentInChildren<Text>().fontSize = 12;
                        gapOffset += (gap + itemHeight);
                        floatParameterBox.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, (gap + gapOffset));
                    }

                    parameterItem.GetComponentInChildren<Text>().text = $"{key}: {VNyanFloatParameters[key]}";
                }
            });

        }


        public void savePluginSettingsData(string fileName, Dictionary<string, string> newPluginSettingsData)
        {
            pluginSettingsData[fileName] = newPluginSettingsData;
        }

        public Dictionary<string, string> loadPluginSettingsData(string fileName)
        {
            Dictionary<string, string> loadedPluginSettingsData;
            if (pluginSettingsData.TryGetValue(fileName, out loadedPluginSettingsData))
            {
                return loadedPluginSettingsData;
            }
            return new Dictionary<string, string>();
        }

        public void setStringParameter(string parameterName, string value)
        {
            VNyanStringParameters[parameterName] = value;
            refreshParameterDisplay();
        }

        public string getStringParameter(string parameterName)
        {
            string loadedParameter;
            if(VNyanStringParameters.TryGetValue(parameterName, out loadedParameter))
            { 
                return loadedParameter;
            }
            return "";
        }

        public void setFloatParameter(string parameterName, float value)
        {
            VNyanFloatParameters[parameterName] = value;
            refreshParameterDisplay();
        }

        public float getFloatParameter(string parameterName)
        {
            float loadedParameter;
            if(VNyanFloatParameters.TryGetValue(parameterName, out loadedParameter))
            { 
                return loadedParameter;
            }
            return 0.0f;
        }
    }
}
