using System;
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

        public VNyanHelper()
        {
            if ((VNyanInterface.VNyanInterface.VNyanParameter == null))
            {

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
        }

        public void setVNyanParameterFloat(string parameterName, float value)
        {
            Debug.Log($"Setting parameter { parameterName } to {value.ToString()}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat(parameterName, value);
            }
            else
            {
                testHarness.setFloatParameter(parameterName, value);
            }
        }

        public void setVNyanParameterString(string parameterName, string value)
        {
            Debug.Log($"Setting parameter { parameterName } to {value}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(parameterName, value);
            }
            else
            {
                testHarness.setStringParameter(parameterName, value);
            }
        }

        public float getVNyanParameterFloat(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat(parameterName);
            }
            else
            {
                return testHarness.getFloatParameter(parameterName);
            }
        }

        public string getVNyanParameterString(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterString(parameterName);
            }
            else
            {
                return testHarness.getStringParameter(parameterName);
            }
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
            else
            {
                testHarness.registerPluginButton(buttonText, pluginInstance);
                Debug.Log($"(Test Mode) Instantiating Window Prefab");
                GameObject window = GameObject.Instantiate(windowPrefab);
                window.transform.SetParent(testCanvasObject.transform);
                return window;
            }


        }

        public GameObject getAvatarObject()
        {
            if (!(VNyanInterface.VNyanInterface.VNyanAvatar == null))
            {

                return VNyanInterface.VNyanInterface.VNyanAvatar.getAvatarObject() as GameObject;
            }
            else
            {
                //TODO:: Some sort of handling/simulation for the devkit
                return null;
            }
        }

        public void registerTriggerListener(ITriggerHandler triggerHandler)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanTrigger == null))
            {
                VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(triggerHandler);
            }
            else
            {
                //TODO:: Some sort of handling/simulation for the devkit
                return;
            }
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
            else
            {
                return testHarness.loadPluginSettingsData(fileName);
            }
        }

        public void savePluginSettingsData(string fileName, Dictionary<string, string> pluginSettingsData)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanSettings == null))
            {
                VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(fileName, pluginSettingsData);
            }
            else
            {
                testHarness.savePluginSettingsData(fileName, pluginSettingsData);
            }
        }

        private VNyanTestHarness getTestHarness()
        {
            return testHarness;
        }

        private GameObject getTestCanvasObject()
        {
            return testCanvasObject;
        }
    }
}
