using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JayoVMCPlugin.VNyanPluginHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;

namespace JayoVMCPlugin
{
    public class JayoVmcPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler, VNyanInterface.ITriggerHandler
    {
        public GameObject windowPrefab;
        public GameObject window;

        public MainThreadDispatcher mainThread;

        private VNyanHelper _VNyanHelper;
        private VNyanPluginUpdater updater;
        private VmcSenderManager vmcManager;

        private GameObject connectButton;
        private GameObject disconnectButton;
        private GameObject autoStartToggle;
        private GameObject noBundleToggle;
        private GameObject sendRateDropdown;
        private GameObject sendBoneScaleToggle;
        private GameObject sendTriggersToggle;
        private GameObject sendRateInfoToggle;
        private GameObject sendRawBonesToggle;
        private GameObject sendAnimParamsToggle;
        private InputField PortInput;
        private InputField AddressInput;

        private string currentVersion = "v0.4.0";
        private string repoName = "jayo-exe/JayoVMCPlugin";
        private string updateLink = "https://jayo-exe.itch.io/vmc-plugin-for-vnyan";

        public void Awake()
        {

            Debug.Log($"VMC is Awake!");
            _VNyanHelper = new VNyanHelper();

            updater = new VNyanPluginUpdater(repoName, currentVersion, updateLink);
            updater.OpenUrlRequested += (url) => mainThread.Enqueue(() => { Application.OpenURL(url); });

            vmcManager = gameObject.AddComponent<VmcSenderManager>();
            vmcManager.VmcConnected += OnVmcConnected;
            vmcManager.VmcDisconnected += OnVmcDisconnected;
            vmcManager.VmcError += OnVmcError;
            Debug.Log($"Loading Settings");
            // Load settings
            loadPluginSettings();
            updater.CheckForUpdates();

            Debug.Log($"Beginning Plugin Setup");

            mainThread = gameObject.AddComponent<MainThreadDispatcher>();

            _VNyanHelper.registerTriggerListener(this);

            try
            {
                window = _VNyanHelper.pluginSetup(this, "Jayo's VMC Plugin", windowPrefab);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }



            // Hide the window by default
            if (window != null)
            {

                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                PortInput = window.transform.Find("Panel/VMCSenderInfo/PortField").GetComponent<InputField>();
                PortInput?.onValueChanged.AddListener((v) => { vmcManager.senderPort = Int32.Parse(v); });
                PortInput?.SetTextWithoutNotify(vmcManager.senderPort.ToString());

                AddressInput = window.transform.Find("Panel/VMCSenderInfo/AddressField").GetComponent<InputField>();
                AddressInput?.onValueChanged.AddListener((v) => { vmcManager.senderAddress = v; });
                AddressInput?.SetTextWithoutNotify(vmcManager.senderAddress);



                setStatusTitle("Idle");

                try
                {
                    Debug.Log($"Preparing Plugin Window beep");

                    updater.PrepareUpdateUI(
                        window.transform.Find("Panel/VersionText").gameObject,
                        window.transform.Find("Panel/UpdateText").gameObject,
                        window.transform.Find("Panel/UpdateButton").gameObject
                    );

                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });

                    connectButton = window.transform.Find("Panel/VMCSenderInfo/ConnectButton").gameObject;
                    connectButton.GetComponent<Button>().onClick.AddListener(() => { initVmc(); });

                    disconnectButton = window.transform.Find("Panel/VMCSenderInfo/DisconnectButton").gameObject;
                    disconnectButton.GetComponent<Button>().onClick.AddListener(() => { deInitVmc(); });

                    autoStartToggle = window.transform.Find("Panel/VMCSenderInfo/AutoStartToggle").gameObject;
                    autoStartToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.autoStart = v; });
                    autoStartToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.autoStart);

                    noBundleToggle = window.transform.Find("Panel/VMCSenderInfo/NoBundleToggle").gameObject;
                    noBundleToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.noBundle = v; });
                    noBundleToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.noBundle);

                    sendRateDropdown = window.transform.Find("Panel/VMCSenderInfo/SendRateDropdown").gameObject;
                    sendRateDropdown.GetComponent<Dropdown>().onValueChanged.AddListener((v) => { vmcManager.updateFrameInterval = v + 1; });
                    sendRateDropdown.GetComponent<Dropdown>().SetValueWithoutNotify(vmcManager.updateFrameInterval - 1);

                    sendBoneScaleToggle = window.transform.Find("Panel/ExtendedFeatures/BoneScaleToggle").gameObject;
                    sendBoneScaleToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.sendBoneScale = v; });
                    sendBoneScaleToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.sendBoneScale);

                    sendTriggersToggle = window.transform.Find("Panel/ExtendedFeatures/SendTriggerToggle").gameObject;
                    sendTriggersToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.sendTriggers = v; });
                    sendTriggersToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.sendTriggers);

                    sendAnimParamsToggle = window.transform.Find("Panel/ExtendedFeatures/SendAnimParamToggle").gameObject;
                    sendAnimParamsToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.sendAnimParams = v; });
                    sendAnimParamsToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.sendAnimParams);

                    sendRateInfoToggle = window.transform.Find("Panel/ExtendedFeatures/SendTimingToggle").gameObject;
                    sendRateInfoToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.sendRateInfo = v; });
                    sendRateInfoToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.sendRateInfo);

                    sendRawBonesToggle = window.transform.Find("Panel/ExtendedFeatures/SendRawBonesToggle").gameObject;
                    sendRawBonesToggle.GetComponent<Toggle>().onValueChanged.AddListener((v) => { vmcManager.sendRawBones = v; });
                    sendRawBonesToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(vmcManager.sendRawBones);
                }
                catch (Exception e)
                {
                    Debug.Log($"Couldn't prepare Plugin Window: {e.Message}");
                }

                try
                {
                    if (vmcManager.autoStart == true)
                    {
                        initVmc();
                    }
                }
                catch (Exception e)
                {
                    setStatusTitle($"Couldn't auto-initialize VMC Client: {e.Message}");
                }
            }

        }

        private void OnVmcConnected()
        {
            setStatusTitle("VMC Connected");
        }

        private void OnVmcDisconnected()
        {
            setStatusTitle("VMC Disconnected");
        }
        private void OnVmcError(string message)
        {
            setStatusTitle($"Error: {message}");
        }

        public void Update()
        {

            //Get the Avatar gameobject
            vmcManager.avatar = _VNyanHelper.getAvatarObject();

            //update the blendshape values
            vmcManager.blendshapes = _VNyanHelper.getAvatarBlendshapes();
        }

        public void initVmc()
        {
            if (vmcManager.senderPort <= 0)
            {
                setStatusTitle("VMC Receiver Port required");
                return;
            }
            mainThread.Enqueue(() =>
            {
                setStatusTitle("Starting VMC");
                vmcManager.initVmc();
                connectButton.SetActive(false);
                disconnectButton.SetActive(true);
                AddressInput.DeactivateInputField();
                PortInput.DeactivateInputField();
            });
        }

        public void deInitVmc()
        {
            mainThread.Enqueue(() =>
            {
                setStatusTitle("Stopping VMC");
                vmcManager.deInitVmc();
                connectButton.SetActive(true);
                disconnectButton.SetActive(false);
                AddressInput.ActivateInputField();
                PortInput.ActivateInputField();
            });
        }

        private void OnApplicationQuit()
        {
            // Save settings
            savePluginSettings();
        }

        public void loadPluginSettings()
        {
            // Get settings in dictionary
            Dictionary<string, string> settings = _VNyanHelper.loadPluginSettingsData("JayoVMCPlugin.cfg");
            if (settings != null)
            {
                string portValue;
                settings.TryGetValue("VMCSenderPort", out portValue);
                if (portValue != null) vmcManager.senderPort = Int32.Parse(portValue);

                string addressValue;
                settings.TryGetValue("VMCSenderAddress", out addressValue);
                if (addressValue != null) vmcManager.senderAddress = addressValue;

                string startValue;
                settings.TryGetValue("VMCAutoStart", out startValue);
                if (startValue != null) vmcManager.autoStart = Boolean.Parse(startValue);

                string bundleValue;
                settings.TryGetValue("VMCNoBundle", out bundleValue);
                if (bundleValue != null) vmcManager.noBundle = Boolean.Parse(bundleValue);

                string sendRate;
                settings.TryGetValue("VMCSendRate", out sendRate);
                if (sendRate != null) vmcManager.updateFrameInterval = Int32.Parse(sendRate);

                string sendBoneScale;
                settings.TryGetValue("VMCSendBoneScale", out sendBoneScale);
                if (sendBoneScale != null) vmcManager.sendBoneScale = Boolean.Parse(sendBoneScale);

                string sendTriggers;
                settings.TryGetValue("VMCSendTriggers", out sendTriggers);
                if (sendTriggers != null) vmcManager.sendTriggers = Boolean.Parse(sendTriggers);

                string sendAnimParams;
                settings.TryGetValue("VMCSendAnimParams", out sendAnimParams);
                if (sendTriggers != null) vmcManager.sendAnimParams = Boolean.Parse(sendAnimParams);

                string sendRateInfo;
                settings.TryGetValue("VMCSendRateInfo", out sendRateInfo);
                if (sendRateInfo != null) vmcManager.sendRateInfo = Boolean.Parse(sendRateInfo);

                string sendRawBones;
                settings.TryGetValue("VMCSendRawBones", out sendRawBones);
                if (sendRawBones != null) vmcManager.sendRawBones = Boolean.Parse(sendRawBones);
            }
        }

        public void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["VMCSenderPort"] = vmcManager.senderPort.ToString();
            settings["VMCSenderAddress"] = vmcManager.senderAddress.ToString();
            settings["VMCAutoStart"] = vmcManager.autoStart.ToString();
            settings["VMCNoBundle"] = vmcManager.noBundle.ToString();
            settings["VMCSendRate"] = vmcManager.updateFrameInterval.ToString();
            settings["VMCSendBoneScale"] = vmcManager.sendBoneScale.ToString();
            settings["VMCSendTriggers"] = vmcManager.sendTriggers.ToString();
            settings["VMCSendAnimParams"] = vmcManager.sendAnimParams.ToString();
            settings["VMCSendRateInfo"] = vmcManager.sendRateInfo.ToString();
            settings["VMCSendRawBones"] = vmcManager.sendRawBones.ToString();


            _VNyanHelper.savePluginSettingsData("JayoVMCPlugin.cfg", settings);
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                if (window.activeSelf) window.transform.SetAsLastSibling();
            }

        }

        public void closePluginWindow()
        {
            window.SetActive(false);
        }

        public void setStatusTitle(string titleText)
        {
            try
            {
                Text StatusTitle = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<Text>();
                StatusTitle.text = titleText;
            }
            catch (Exception e)
            {
                Debug.Log("title window doesnt exist yet");
            }
        }

        public void triggerCalled(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            if (!triggerName.StartsWith("_xjv_") && !triggerName.StartsWith("_xjvt:")) return;

            //Debug.Log($"Trigger Details. name: {triggerName} | v1: {value1}, v2: {value2}, v3: {value3} | t1: {text1}, t2: {text2}, t3: {text3}");
            if(triggerName.StartsWith("_xjvt:"))
            {
                string newTriggerName = triggerName.Substring(6);
                //Debug.Log($"Transmitting Trigger Details. name: {newTriggerName} | v1: {value1}, v2: {value2}, v3: {value3} | t1: {text1}, t2: {text2}, t3: {text3}");
                vmcManager.SendTriggerOverVMC(newTriggerName, value1, value2, value3, text1, text2, text3);
                return;
            }
            
            switch (triggerName)
            {
                case "_xjv_init":
                    initVmc();
                    break;
                case "_xjv_deinit":
                    deInitVmc();
                    break;
                default:
                    break;
            }
        }
    }
}
