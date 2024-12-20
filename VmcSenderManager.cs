using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Net;
using uOSC;
using System.Xml;
using UnityEngine.XR;

namespace JayoVMCPlugin
{
    class VmcSenderManager : MonoBehaviour
    {
        public int senderPort;
        public string senderAddress;
        public bool noBundle;
        public bool autoStart;
        public GameObject avatar;
        public Dictionary<string, float> blendshapes;
        public int updateFrameInterval;
        public bool sendBoneScale;
        public bool sendAnimParams;
        public bool sendTriggers;
        public bool sendRateInfo;
        public bool sendRawBones;

        private Dictionary<string, Animator> childAnimators = new Dictionary<string, Animator>();
        private Dictionary<string, Dictionary<int, int>> animatorIntParameters = new Dictionary<string, Dictionary<int, int>>();
        private Dictionary<string, Dictionary<int, float>> animatorFloatParameters = new Dictionary<string, Dictionary<int, float>>();
        private Dictionary<string, Dictionary<int, bool>> animatorBooleanParameters = new Dictionary<string, Dictionary<int, bool>>();
        private Dictionary<string, Dictionary<int, bool>> animatorTriggerParameters = new Dictionary<string, Dictionary<int, bool>>();
        private Dictionary<string, Transform> rawBones = new Dictionary<string, Transform>();

        public event Action VmcConnected;
        public event Action VmcDisconnected;
        public event Action<string> VmcError;

        private uOscClient osc;
        private GameObject lastAvatar;
        private Animator animator;
        private Dictionary<string, Vector3> boneScales;
        private int currentFrameInterval;

        public bool senderReady
        {
            get { return osc.isRunning; }
        }

        // Send Tracking data from Vnyan to a VMC Reciever every frame

        public void Awake()
        {
            Debug.Log($"[VMC Plugin] Manager Awake!");

            senderPort = 39539;
            senderAddress = "localhost";
            noBundle = false;
            autoStart = false;
            updateFrameInterval = 1;
            sendBoneScale = false;
            sendTriggers = false;
            sendAnimParams = false;
            sendRateInfo = false;
            sendRawBones = false;
            currentFrameInterval = 0;
            boneScales = new Dictionary<string, Vector3>();

            gameObject.SetActive(false);
            osc = gameObject.AddComponent<uOscClient>();
            osc.enabled = false;
            osc.maxQueueSize = 300;
            gameObject.SetActive(true);

            osc.onClientStarted.AddListener((string a, int b) => {
                VmcConnected.Invoke();
                Debug.Log($"[VMC Plugin] Client started!");
            });

            osc.onClientStopped.AddListener((string a, int b) => {
                VmcDisconnected.Invoke();
                Debug.Log($"[VMC Plugin] Client stopped");
            });

            Debug.Log($"[VMC Plugin] Manager Initialized!");
        }

        public void OnEnable()
        {

        }

        public void Update()
        {
            currentFrameInterval++;
            if (currentFrameInterval < updateFrameInterval) return; //only actually run on frames needed to meet send rate

            currentFrameInterval = 0;

            Transform rootNode;

            if (avatar != null && avatar != lastAvatar)
            {
                Debug.Log($"avatar object is {avatar.name}");
                animator = avatar.GetComponent<Animator>();
                if(animator == null)
                {
                    Debug.Log($"animator not found for {avatar.name}");
                }
                refetchChildAnimators();
                RetrieveRawBones();
                lastAvatar = avatar;
            }

            if(animator != null)
            {
                rootNode = animator.GetBoneTransform(HumanBodyBones.Hips);

                Vector3 pos = rootNode.position;
                Vector3 rot = rootNode.rotation.eulerAngles;

                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_pos_x", rootNode.position.x);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_pos_y", rootNode.position.y);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_pos_z", rootNode.position.z);

                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_rot_x", rootNode.rotation.eulerAngles.x);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_rot_y", rootNode.rotation.eulerAngles.y);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_av_rot_z", rootNode.rotation.eulerAngles.z);
            }

            if (animator == null || avatar == null || !senderReady)
            {
                sendVMC("/VMC/Ext/OK", 0);
                sendVMC("/VMC/Ext/T", Time.time);
                return;
            }

            if (sendAnimParams) checkAllParameters();

            Bundle boneBundle = new Bundle(Timestamp.Now);
            Bundle blendshapeBundle = new Bundle(Timestamp.Now);

            rootNode = animator.GetBoneTransform(HumanBodyBones.Hips);

            sendVMC(new Message("/VMC/Ext/Root/Pos",
               "root",

                rootNode.position.x,
                rootNode.position.y,
                rootNode.position.z,

                rootNode.rotation.x,
                rootNode.rotation.y,
                rootNode.rotation.z,
                rootNode.rotation.w
            ));

            if (!sendRawBones)
            {
                foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (bone == HumanBodyBones.LastBone) continue;
                    if (bone == HumanBodyBones.Hips) continue;

                    ///VMC/Ext/Bone/Pos (string){name} (float){p.x} (float){p.y} (float){p.z} (float){q.x} (float){q.y} (float){q.z} (float){q.w}

                    Transform boneTransform = animator.GetBoneTransform(bone);
                    if (boneTransform != null)
                    {
                        if (sendBoneScale)
                        {
                            if (boneScales.ContainsKey(bone.ToString()) && Vector3.Distance(boneScales[bone.ToString()], boneTransform.localScale) > 0)
                            {
                                var scaleMessage = new Message("/NyaVMC/Ext/Bone/Scale",
                                    bone.ToString(),

                                    boneTransform.localScale.x,
                                    boneTransform.localScale.y,
                                    boneTransform.localScale.z
                                );

                                if (noBundle)
                                {
                                    sendVMC(scaleMessage);
                                }
                                else
                                {
                                    boneBundle.Add(scaleMessage);
                                }
                            }
                            boneScales[bone.ToString()] = boneTransform.localScale;
                        }

                        var boneMessage = new Message("/VMC/Ext/Bone/Pos",
                            bone.ToString(),

                            boneTransform.localPosition.x,
                            boneTransform.localPosition.y,
                            boneTransform.localPosition.z,

                            boneTransform.localRotation.x,
                            boneTransform.localRotation.y,
                            boneTransform.localRotation.z,
                            boneTransform.localRotation.w
                        );

                        if (noBundle)
                        {
                            sendVMC(boneMessage);
                        }
                        else
                        {
                            boneBundle.Add(boneMessage);
                        }
                    }
                }
                if (!noBundle)
                {
                    sendVMC(boneBundle);
                }
            }
            else
            {
                foreach(KeyValuePair<string,Transform> boneEntry in rawBones)
                {
                    var boneMessage = new Message("/NyaVMC/Ext/RawBone/Pos",
                        boneEntry.Key,

                        boneEntry.Value.localPosition.x,
                        boneEntry.Value.localPosition.y,
                        boneEntry.Value.localPosition.z,

                        boneEntry.Value.localRotation.x,
                        boneEntry.Value.localRotation.y,
                        boneEntry.Value.localRotation.z,
                        boneEntry.Value.localRotation.w
                    );

                    if (noBundle)
                    {
                        sendVMC(boneMessage);
                    }
                    else
                    {
                        boneBundle.Add(boneMessage);
                    }
                }
                if (!noBundle)
                {
                    sendVMC(boneBundle);
                }
            }

            foreach (KeyValuePair<string, float> blendshape in blendshapes)
            {
                var blendMessage = new Message("/VMC/Ext/Blend/Val", blendshape.Key, blendshape.Value);
                if(noBundle)
                {
                    sendVMC(blendMessage);
                } else
                {
                    blendshapeBundle.Add(blendMessage);
                }
                
            }
            if (noBundle)
            {
                sendVMC(new Message("/VMC/Ext/Blend/Apply"));
            }
            else
            {
                blendshapeBundle.Add(new Message("/VMC/Ext/Blend/Apply"));
            }

            if (!noBundle)
            {
                sendVMC(blendshapeBundle);
            }

            sendVMC("/VMC/Ext/OK", 1);
            sendVMC("/VMC/Ext/T", Time.time);
            if(sendRateInfo)
            {
                sendVMC("/NyaVMC/F", updateFrameInterval);
            }
            
        }

        public void initVmc()
        {
            if (osc == null) return;

            if (osc.isRunning) 
            {
                VmcConnected.Invoke();
                Debug.Log($"[VMC Plugin] OSC already running!");
                return; 
            }

            Debug.Log($"[VMC Plugin] Client initializing!");
            osc.port = senderPort;
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(senderAddress);

                if (addresses.Length > 0)
                {
                    osc.address = addresses[0].ToString();
                }
                else
                {
                    VmcError.Invoke("No IP addresses found for hostname");
                    return;
                }
            }
            catch (Exception ex)
            {
                VmcError.Invoke(ex.Message);
                return;
            }
            osc.enabled = true;
        }

        public void deInitVmc()
        {
            if (osc == null) return;

            if (!osc.isRunning)
            {
                VmcDisconnected.Invoke();
                Debug.Log($"[VMC Plugin] OSC already stopped!");
                return;
            }

            Debug.Log($"[VMC Plugin] Client de-initializing!");
            osc.enabled = false;
        }

        private void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("Exiting Play Mode");
                osc.enabled = false;
            }
        }

        private void OnDisable()
        {
            deInitVmc();
        }

        private void OnDestroy()
        {
            deInitVmc();
        }

        private void OnApplicationQuit()
        {
            deInitVmc();
        }

        public void sendVMC(string address, params object[] values)
        {
            if (osc == null) return;

            osc.Send(address, values);
        }

        public void sendVMC(Message message)
        {
            if (osc == null) return;

            osc.Send(message);
        }

        public void sendVMC(Bundle bundle)
        {
            if (osc == null) return;

            osc.Send(bundle);
        }

        public void SendTriggerOverVMC(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            if (!sendTriggers) return;
            sendVMC("/NyaVMC/Trigger", triggerName, value1, value2, value3, text1, text2, text3);
        }

        private string getObjectPath(Transform child, Transform parent)
        {
            string path = child.name;
            Transform currentParent = child.parent;

            while (currentParent != parent && currentParent != null)
            {
                path = currentParent.name + "/" + path;
                currentParent = currentParent.parent;
            }

            if (currentParent == parent)
            {
                return path;
            }

            return "";
        }

        private void refetchChildAnimators()
        {
            childAnimators = new Dictionary<string, Animator>();
            animatorIntParameters = new Dictionary<string, Dictionary<int, int>>();
            animatorFloatParameters = new Dictionary<string, Dictionary<int, float>>();
            animatorBooleanParameters = new Dictionary<string, Dictionary<int, bool>>();
            animatorTriggerParameters = new Dictionary<string, Dictionary<int, bool>>();

            Animator[] animators = avatar.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                string relativePath = getObjectPath(animators[i].transform, avatar.transform);
                childAnimators[relativePath] = animators[i];
                animatorIntParameters[relativePath] = new Dictionary<int, int>();
                animatorFloatParameters[relativePath] = new Dictionary<int, float>();
                animatorBooleanParameters[relativePath] = new Dictionary<int, bool>();
                animatorTriggerParameters[relativePath] = new Dictionary<int, bool>();
            }
        }

        private void checkAllParameters()
        {
            if (childAnimators.Count == 0) return;

            foreach (KeyValuePair<string, Animator> item in childAnimators)
            {
                RetrieveAnimatorParameters(item.Key, item.Value);
            }
        }

        private void RetrieveRawBones()
        {
            childAnimators = new Dictionary<string, Animator>();
            var rootNode = animator.GetBoneTransform(HumanBodyBones.Hips);
            string rootPath = getObjectPath(rootNode, avatar.transform);
            TraverseArmatureNode(rootNode.gameObject, rootPath, true);

        }

        private void TraverseArmatureNode(GameObject currentNode, string relativePath, bool noSave = false)
        {
            if (!noSave) rawBones[relativePath] = currentNode.transform;

            foreach (Transform child in currentNode.transform)
            {
                string childPath = relativePath + "/" + child.name;
                TraverseArmatureNode(child.gameObject, childPath);
            }
        }

        private void RetrieveAnimatorParameters(string path, Animator target)
        {
            bool paramsChanged = false;
            Bundle parameterBundle = new Bundle(Timestamp.Now);
            AnimatorControllerParameter[] parameters = target.parameters;

            // Iterate over all the parameters
            foreach (AnimatorControllerParameter parameter in parameters)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        int intValue = target.GetInteger(parameter.nameHash);
                        if (!animatorIntParameters[path].ContainsKey(parameter.nameHash) || animatorIntParameters[path][parameter.nameHash] != intValue)
                        {
                            //send/batch change packet for this difference
                            //NyaVMC/Ext/Anim/IntParam animatorPath paramhash value
                            var paramMessage = new Message("/NyaVMC/Ext/Anim/IntParam", path, parameter.nameHash, intValue);
                            if (noBundle) sendVMC(paramMessage);
                            else parameterBundle.Add(paramMessage);
                            paramsChanged = true;
                            Debug.Log($"Int Parameter Changed: {path} {parameter.name} {intValue}");
                        }
                        animatorIntParameters[path][parameter.nameHash] = intValue;
                        break;
                    case AnimatorControllerParameterType.Float:
                        float floatValue = target.GetFloat(parameter.nameHash);
                        if (!animatorFloatParameters[path].ContainsKey(parameter.nameHash) || animatorFloatParameters[path][parameter.nameHash] != floatValue)
                        {
                            //send/batch change packet for this difference
                            //NyaVMC/Ext/Anim/FloatParam animatorPath paramhash value
                            var paramMessage = new Message("/NyaVMC/Ext/Anim/FloatParam", path, parameter.nameHash, floatValue);
                            if (noBundle) sendVMC(paramMessage);
                            else parameterBundle.Add(paramMessage);
                            paramsChanged = true;
                            Debug.Log($"Float Parameter Changed: {path} {parameter.name} ({parameter.nameHash}) {floatValue}");
                        }
                        animatorFloatParameters[path][parameter.nameHash] = floatValue;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        bool boolValue = target.GetBool(parameter.nameHash);
                        if (!animatorBooleanParameters[path].ContainsKey(parameter.nameHash) || animatorBooleanParameters[path][parameter.nameHash] != boolValue)
                        {
                            //send/batch change packet for this difference
                            //NyaVMC/Ext/Anim/BoolParam animatorPath paramhash value
                            var paramMessage = new Message("/NyaVMC/Ext/Anim/BoolParam", path, parameter.nameHash, boolValue);
                            if (noBundle) sendVMC(paramMessage);
                            else parameterBundle.Add(paramMessage);
                            paramsChanged = true;
                            Debug.Log($"Bool Parameter Changed: {path} {parameter.name} {boolValue}");
                        }
                        animatorBooleanParameters[path][parameter.nameHash] = boolValue;
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        bool triggerValue = target.GetBool(parameter.nameHash);
                        if (!animatorTriggerParameters[path].ContainsKey(parameter.nameHash) || (animatorTriggerParameters[path][parameter.nameHash] != triggerValue && triggerValue))
                        {
                            //send/batch change packet for this difference
                            //NyaVMC/Ext/Anim/BoolParam animatorPath paramhash value
                            var paramMessage = new Message("/NyaVMC/Ext/Anim/TriggerParam", path, parameter.nameHash);
                            if (noBundle) sendVMC(paramMessage);
                            else parameterBundle.Add(paramMessage);
                            paramsChanged = true;
                            Debug.Log($"Trigger Parameter Set: {path} {parameter.name}");
                        }
                        animatorTriggerParameters[path][parameter.nameHash] = triggerValue;
                        break;
                    default:
                        Debug.LogWarning($"Unhandled parameter type: {parameter.type}");
                        break;
                }
            }

            if(paramsChanged && !noBundle)
            {
                sendVMC(parameterBundle);
            }
        }

    }
}
