using System;
using System.Collections.Generic;
using System.Text;
using VNyanInterface;
using UnityEngine;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    
    class VNyanTestUI: MonoBehaviour, IUIInterface
    {
        private VNyanTestUI _instance;
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

        public object instantiateUIPrefab(object gameObject)
        {
            if (!(gameObject is GameObject)) return null;
            Debug.Log($"(Test Mode) Instantiating Window Prefab");
            GameObject window = GameObject.Instantiate((GameObject)gameObject);
            window.transform.SetParent(GameObject.FindObjectOfType<VNyanTestHarness>().canvasObject.transform);
            return window;
        }

        public void registerPluginButton(string buttonText, IButtonClickedHandler clickCallback)
        {
            VNyanTestHarness harness = GameObject.FindObjectOfType<VNyanTestHarness>();
            GameObject.FindObjectOfType<VNyanTestHarness>().registeredPlugins[buttonText] = clickCallback;
            GameObject.FindObjectOfType<VNyanTestHarness>().refreshButtonDisplay();
        }

        public string openLoadFileDialog(string header, string[] extensions)
        {
            return "";
        }

        public string openSaveFileDialog(string header, string[] extensions)
        {
            return "";
        }

    }
}
