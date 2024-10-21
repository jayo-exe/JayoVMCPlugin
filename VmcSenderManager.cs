using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Net;
using uOSC;

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

            if (avatar != null && avatar != lastAvatar)
            {
                Debug.Log($"avatar object is {avatar.name}");
                animator = avatar.GetComponent<Animator>();
                if(animator == null)
                {
                    Debug.Log($"animator not found for {avatar.name}");
                }
                lastAvatar = avatar;
            }

            if (animator == null || avatar == null || !senderReady)
            {
                sendVMC("/VMC/Ext/OK", 0);
                sendVMC("/VMC/Ext/T", Time.time);
                return;
            }

            Bundle boneBundle = new Bundle(Timestamp.Now);
            Bundle blendshapeBundle = new Bundle(Timestamp.Now);
            var rootNode = animator.GetBoneTransform(HumanBodyBones.Hips); //probably this

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

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                if (bone == HumanBodyBones.Hips) continue;

                ///VMC/Ext/Bone/Pos (string){name} (float){p.x} (float){p.y} (float){p.z} (float){q.x} (float){q.y} (float){q.z} (float){q.w}

                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    if(boneScales.ContainsKey(bone.ToString()) && Vector3.Distance(boneScales[bone.ToString()], boneTransform.localScale) > 0)
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
                    
                    if(noBundle)
                    {
                        sendVMC(boneMessage);
                    } else
                    {
                        boneBundle.Add(boneMessage);
                    }
                }
            }
            if (!noBundle)
            {
                sendVMC(boneBundle);
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
            sendVMC("/NyaVMC/Trigger", triggerName, value1, value2, value3, text1, text2, text3);
        }

    }
}
