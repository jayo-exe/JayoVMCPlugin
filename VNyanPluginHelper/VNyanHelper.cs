using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VNyanInterface;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    public class VNyanHelper
    {
        private VNyanTestHarness testHarness;
        private GameObject testCanvasObject;
        private VNyanTestParameter parameterSystem;
        MainThreadDispatcher mainThread;
        public bool inVNyan { get; }

        public VNyanHelper()
        {
            inVNyan = true;
            if (VNyanInterface.VNyanInterface.VNyanParameter != null) return;

            // VNyan's parameter interface isn't initialized, which means we aren't in VNyan (we're in the Unity editor)
            // Initialize the emulated VNyanIntreface systems so we can test stuff in-editor
            inVNyan = false;
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name == "UISprite")
                {
                    uiResources.standard = sprite;
                    break;
                }
            }

            var harnessObject = GameObject.Find("__VNyanTestHarness");
            if (harnessObject == null)
            {
                Debug.Log($"Instantiating Test Harness");
                harnessObject = new GameObject("__VNyanTestHarness");

            }

            if (GameObject.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            if (GameObject.FindObjectOfType<VNyanTestTrigger>() == null)
            {
                VNyanTestTrigger triggerSystem = harnessObject.AddComponent<VNyanTestTrigger>();
                VNyanInterface.VNyanInterface.VNyanTrigger = triggerSystem;
            }

            if (GameObject.FindObjectOfType<VNyanTestParameter>() == null)
            {
                parameterSystem = harnessObject.AddComponent<VNyanTestParameter>();
                VNyanInterface.VNyanInterface.VNyanParameter = parameterSystem;
            }

            if (GameObject.FindObjectOfType<VNyanTestSettings>() == null)
            {
                VNyanTestSettings settingsSystem = harnessObject.AddComponent<VNyanTestSettings>();
                VNyanInterface.VNyanInterface.VNyanSettings = settingsSystem;
            }

            if (GameObject.FindObjectOfType<VNyanTestUI>() == null)
            {
                VNyanTestUI uiSystem = harnessObject.AddComponent<VNyanTestUI>();
                VNyanInterface.VNyanInterface.VNyanUI = uiSystem;
            }

            VNyanTestAvatar avatarSystem = GameObject.FindObjectOfType<VNyanTestAvatar>();
            if (avatarSystem == null)
            {
                avatarSystem = harnessObject.AddComponent<VNyanTestAvatar>();      
            }

            VNyanInterface.VNyanInterface.VNyanAvatar = avatarSystem;

            var canvasObject = GameObject.Find("__VNyanTestHarness/__VNyanTestCanvas");
            if (canvasObject == null)
            {
                Debug.Log($"Instantiating Test Canvas");
                canvasObject = new GameObject("__VNyanTestCanvas");
                canvasObject.AddComponent<Canvas>();
                Canvas myCanvas = canvasObject.GetComponent<Canvas>();
                myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                canvasObject.transform.SetParent(harnessObject.transform);

                GameObject canvasButtonHolder = DefaultControls.CreatePanel(uiResources);
                canvasButtonHolder.name = "__VNyanPluginButtonHolder";
                canvasButtonHolder.transform.SetParent(canvasObject.transform);

                GameObject canvasParameterHolder = DefaultControls.CreatePanel(uiResources);
                canvasParameterHolder.name = "__VNyanParameterHolder";
                canvasParameterHolder.transform.SetParent(canvasObject.transform);
                canvasParameterHolder.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 12, 468);
                canvasParameterHolder.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 12, 240);

                GameObject stringParameterScroll = DefaultControls.CreateScrollView(uiResources);
                stringParameterScroll.name = "__VNyanStringParameterScroll";
                stringParameterScroll.transform.SetParent(canvasParameterHolder.transform);
                stringParameterScroll.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 12, 216);
                stringParameterScroll.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 12, 216);

                GameObject floatParameterScroll = DefaultControls.CreateScrollView(uiResources);
                floatParameterScroll.name = "__VNyanFloatParameterScroll";
                floatParameterScroll.transform.SetParent(canvasParameterHolder.transform);
                floatParameterScroll.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 240, 216);
                floatParameterScroll.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 12, 216);

            }
            testCanvasObject = canvasObject;

            harnessObject.AddComponent<VNyanTestHarness>();
            testHarness = harnessObject.GetComponent<VNyanTestHarness>();
            
        }

        public void setVNyanParameterFloat(string parameterName, float value)
        {
            //Debug.Log($"Setting parameter { parameterName } to {value.ToString()}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat(parameterName, value);
            }
        }

        public void setVNyanParameterString(string parameterName, string value)
        {
            //Debug.Log($"Setting parameter { parameterName } to {value}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(parameterName, value);
            }
        }

        public float getVNyanParameterFloat(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat(parameterName);
            }
            return 0.0f;
        }

        public string getVNyanParameterString(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterString(parameterName);
            }
            return "";
        }

        public GameObject pluginSetup(IButtonClickedHandler pluginInstance, string buttonText, GameObject windowPrefab)
        {
            // Register button to plugins window
            if (!(VNyanInterface.VNyanInterface.VNyanUI == null))
            {
                VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton(buttonText, pluginInstance);
                Debug.Log($"Instantiating Window Prefab");
                // Create a window that will show when the button in plugins window is clicked
                return (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
            }
            return null;


        }

        public string getVNyanDictionaryValue(string dictionaryName, string keyName)
        {
            //Debug.Log($"Getting Value { keyName } from Dictionary { dictionaryName }");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanDictionaryValue(dictionaryName, keyName);
            }
            return "";

        }

        public void setVNyanDictionaryValue(string dictionaryName, string keyName, string value)
        {
            //Debug.Log($"Setting Value { keyName } from Dictionary { dictionaryName } to { value }");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanDictionaryValue(dictionaryName, keyName, value);
            }
        }

        public void clearVNyanDictionary(string dictionaryName)
        {
            //Debug.Log($"Clearing Dictionary { dictionaryName }");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.clearVNyanDictionary(dictionaryName);
            }
        }

        public GameObject getAvatarObject()
        {
            if (!(VNyanInterface.VNyanInterface.VNyanAvatar == null))
            {
                return VNyanInterface.VNyanInterface.VNyanAvatar.getAvatarObject() as GameObject;
            }
            return null;

            GameObject avatar = VNyanInterface.VNyanInterface.VNyanAvatar.getAvatarObject() as GameObject;
            SkinnedMeshRenderer[] smrs = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void registerTriggerListener(ITriggerHandler triggerHandler)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanTrigger == null))
            {
                VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(triggerHandler);
            }
        }

        public void callTrigger(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            //Debug.Log($"Trigger called: {triggerName}");
            if (!(VNyanInterface.VNyanInterface.VNyanTrigger == null))
            {
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger(triggerName, value1, value2, value3, text1, text2, text3);
            }
        }

        public string parseStringArgument(string arg)
        {
            if(arg.StartsWith("<") && arg.EndsWith(">"))
            {
                return getVNyanParameterString(arg.Substring(1, arg.Length - 2));
            }
            return arg;
        }

        public float parseFloatArgument(string arg)
        {
            if (arg.StartsWith("[") && arg.EndsWith("]"))
            {
                //float param, just get and return the value directly
                return getVNyanParameterFloat(arg.Substring(1, arg.Length - 2));
            }

            if (arg.StartsWith("<") && arg.EndsWith(">"))
            {
                //string param, set arg to the value from the parameters list before parsing
                arg = getVNyanParameterString(arg.Substring(1, arg.Length - 2));
            }

            //parse the value of arg into a float
            float returnVal = 0f;
            float.TryParse(arg, NumberStyles.Any, CultureInfo.InvariantCulture, out returnVal);
            return returnVal;
        }

        public Dictionary<string, float> getAvatarBlendshapes()
        {
            if (!(VNyanInterface.VNyanInterface.VNyanAvatar == null))
            {

                return VNyanInterface.VNyanInterface.VNyanAvatar.getBlendshapesInstant();
            }
            else
            {
                //TODO:: Some sort of handling/simulation for the devkit
                return null;
            }
        }

        public void setAvatarBlendshape(string key, float value)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanAvatar == null))
            {
                VNyanInterface.VNyanInterface.VNyanAvatar.setBlendshapeOverride(key, value);
            }
            else
            {
                //TODO:: Some sort of handling/simulation for the devkit
                return;
            }
        }

        public void setAvatarBlendshapes(Dictionary<string, float> blendshapes)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanAvatar == null))
            {

                foreach (KeyValuePair<string, float> blendshape in blendshapes)
                {
                    VNyanInterface.VNyanInterface.VNyanAvatar.setBlendshapeOverride(blendshape.Key, blendshape.Value);
                }

            }
            else
            {
                //TODO:: Some sort of handling/simulation for the devkit
                return;
            }
        }

        public Dictionary<string, string> loadPluginSettingsData(string fileName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanSettings == null))
            {
                return VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(fileName);
            }
            return new Dictionary<string, string>();
        }

        public void savePluginSettingsData(string fileName, Dictionary<string, string> pluginSettingsData)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanSettings == null))
            {
                VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(fileName, pluginSettingsData);
            }
        }

        private VNyanTestHarness getTestHarness()
        {
            return testHarness;
        }

        public GameObject getTestCanvasObject()
        {
            return testCanvasObject;
        }
    }
}
