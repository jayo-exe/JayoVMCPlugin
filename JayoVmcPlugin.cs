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
    public class JayoVmcPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;
        public GameObject window;

        public MainThreadDispatcher mainThread;

        private VNyanHelper _VNyanHelper;
        private VNyanTriggerDispatcher triggerDispatcher;
        private VmcSenderManager vmcManager;

        private GameObject connectButton;
        private GameObject disconnectButton;
        private GameObject autoStartToggle;
        private GameObject noBundleToggle;
        private InputField PortInput;
        private InputField AddressInput;

        private string currentVersion = "v0.1.0";
        private string latestVersion = "";
        private bool updateAvailable = false;
        private string repoName = "jayo-exe/JayoVMCPlugin";
        private GameObject versionText;
        private GameObject updateText;
        private GameObject updateButton;

        private void CheckForUpdates()
        {
            try
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create($"https://api.github.com/repos/{repoName}/releases");
                Request.UserAgent = "request";
                HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                StreamReader Reader = new StreamReader(response.GetResponseStream());
                string JsonResponse = Reader.ReadToEnd();
                JArray Releases = JArray.Parse(JsonResponse);
                latestVersion = Releases[0]["tag_name"].ToString();
                updateAvailable = currentVersion != latestVersion;
            }
            catch (Exception e)
            {
                Debug.Log($"Couldn't check for updates: {e.Message}");
            }
        }

        private void OpenUpdatePage()
        {
            mainThread.Enqueue(() => {
                Application.OpenURL($"https://github.com/{repoName}/releases/latest");
            });
        }

        private void PrepareUpdateUI()
        {

            versionText = window.transform.Find("Panel/VersionText").gameObject;
            versionText.GetComponent<Text>().text = currentVersion;

            updateText = window.transform.Find("Panel/UpdateText").gameObject;
            updateText.GetComponent<Text>().text = $"New Update Available: {latestVersion}";

            updateButton = window.transform.Find("Panel/UpdateButton").gameObject;
            updateButton.GetComponent<Button>().onClick.AddListener(() => { OpenUpdatePage(); });

            if (!updateAvailable)
            {
                updateText.SetActive(false);
                updateButton.SetActive(false);
            }
        }

        public void Awake()
        {

            Debug.Log($"VMC is Awake!");
            _VNyanHelper = new VNyanHelper();

            vmcManager = gameObject.AddComponent<VmcSenderManager>();
            vmcManager.VmcConnected += OnVmcConnected;
            vmcManager.VmcDisconnected += OnVmcDisconnected;
            vmcManager.VmcError += OnVmcError;
            Debug.Log($"Loading Settings");
            // Load settings
            loadPluginSettings();
            CheckForUpdates();

            Debug.Log($"Beginning Plugin Setup");

            mainThread = gameObject.AddComponent<MainThreadDispatcher>();
            triggerDispatcher = gameObject.AddComponent<VNyanTriggerDispatcher>();

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
                    Debug.Log($"Preparing Plugin Window");

                    PrepareUpdateUI();

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
                }
                catch (Exception e)
                {
                    Debug.Log($"Couldn't prepare Plugin Window: {e.Message}");
                }

                try
                {
                    if(vmcManager.autoStart == true)
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
            if  (vmcManager.senderPort <= 0)
            {
                setStatusTitle("VMC Receiver Port required");
                return;
            }
            mainThread.Enqueue(() => {
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
            mainThread.Enqueue(() => {
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
                if(portValue != null) vmcManager.senderPort = Int32.Parse(portValue);

                string addressValue;
                settings.TryGetValue("VMCSenderAddress", out addressValue);
                if (addressValue != null) vmcManager.senderAddress = addressValue;

                string startValue;
                settings.TryGetValue("VMCAutoStart", out startValue);
                if (startValue != null) vmcManager.autoStart = Boolean.Parse(startValue);

                string bundleValue;
                settings.TryGetValue("VMCNoBundle", out bundleValue);
                if (bundleValue != null) vmcManager.noBundle = Boolean.Parse(bundleValue);
            }
        }

        public void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["VMCSenderPort"] = vmcManager.senderPort.ToString();
            settings["VMCSenderAddress"] = vmcManager.senderAddress.ToString();
            settings["VMCAutoStart"] = vmcManager.autoStart.ToString();
            settings["VMCNoBundle"] = vmcManager.noBundle.ToString();

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
            } catch(Exception e)
            {
                Debug.Log("title window doesnt exist yet");
            }
        }
    }
}
