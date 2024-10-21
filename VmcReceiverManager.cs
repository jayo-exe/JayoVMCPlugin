using System;
using System.Collections.Generic;
using UnityEngine;
using uOSC;
using VRM;

namespace JayoVMCPlugin
{
    class VmcReceiverManager : MonoBehaviour
    {
        public GameObject Model;
        private GameObject RootNode;
        private GameObject OldModel = null;

        public int receiverPort;

        public bool AnchorToStage;
        public bool BoneRotationsOnly;

        private Animator animator = null;
        private VRMBlendShapeProxy blendShapeProxy = null;
        private Dictionary<BlendShapeKey, float> blends = new Dictionary<BlendShapeKey, float>();
        private Dictionary<string, BlendShapeKey> ValidBlendshapes = new Dictionary<string, BlendShapeKey>();

        private uOSC.uOscServer osc;


        void Start()
        {
            if (receiverPort < 1)
            {
                receiverPort = 39539;
            }

            osc = gameObject.AddComponent<uOscServer>();
            osc.port = receiverPort;
            osc.onDataReceived.AddListener(OnDataReceived);
        }

        void Update()
        {
            try
            {
                if (blendShapeProxy == null)
                {
                    blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                    if (blendShapeProxy != null)
                    {
                        ValidBlendshapes.Clear();
                        var bsv = blendShapeProxy.GetValues();
                        foreach (var b in bsv)
                        {
                            ValidBlendshapes.Add(b.Key.Name.ToLower(), b.Key);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Debug.Log("O!");
            }


        }

        void OnDataReceived(uOSC.Message message)
        {
            //Model updated
            if (Model != null && OldModel != Model)
            {
                animator = Model.GetComponent<Animator>();
                RootNode = animator.GetBoneTransform(HumanBodyBones.Hips).gameObject;
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                if (blendShapeProxy != null)
                {
                    ValidBlendshapes.Clear();
                    var bsv = blendShapeProxy.GetValues();
                    foreach (var b in bsv)
                    {
                        ValidBlendshapes.Add(b.Key.Name.ToLower(), b.Key);
                    }
                }
                OldModel = Model;
            }

            if (message.address.StartsWith("/VMC/"))
            {
                handleVMCMessage(message);
            }
            else if (message.address.StartsWith("/NyaVMC/"))
            {
                handleNyaVMCMessage(message);
            }
        }

        void handleVMCMessage(uOSC.Message message)
        {
            if (message.address == "/VMC/Ext/Root/Pos")
            {
                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);
                if (AnchorToStage)
                {
                    RootNode.transform.localPosition = pos;
                    RootNode.transform.localRotation = rot;
                }
                else
                {
                    RootNode.transform.position = pos;
                    RootNode.transform.rotation = rot;
                }

            }

            else if (message.address == "/VMC/Ext/Bone/Pos")
            {


                HumanBodyBones bone;
                if (Enum.TryParse<HumanBodyBones>((string)message.values[0], out bone))
                {
                    if ((animator != null) && (bone != HumanBodyBones.LastBone) && (bone != HumanBodyBones.Hips))
                    {
                        Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                        Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                        var t = animator.GetBoneTransform(bone);
                        if (t != null)
                        {
                            if (!BoneRotationsOnly)
                            {
                                t.localPosition = pos;
                            }

                            t.localRotation = rot;
                        }
                    }
                }
            }
            else if (message.address == "/VMC/Ext/Blend/Val")
            {
                string BlendName = ((string)message.values[0]).ToLower();
                float BlendValue = (float)message.values[1];

                if (ValidBlendshapes.ContainsKey(BlendName) && !blends.ContainsKey(ValidBlendshapes[BlendName]))
                {
                    blends.Add(ValidBlendshapes[BlendName], BlendValue);
                }
            }
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                if (blendShapeProxy != null)
                {
                    blendShapeProxy.SetValues(blends);
                }
                blends.Clear();

            }
        }

        void handleNyaVMCMessage(uOSC.Message message)
        {
            if (message.address == "/NyaVMC/Trigger")
            {
                string triggerName = (string)message.values[0];
                int value1 = (int)message.values[1];
                int value2 = (int)message.values[2];
                int value3 = (int)message.values[3];
                string text1 = (string)message.values[4];
                string text2 = (string)message.values[5];
                string text3 = (string)message.values[6];

                //Debug.Log($"Receiving Trigger Details. name: {triggerName} | v1: {value1}, v2: {value2}, v3: {value3} | t1: {text1}, t2: {text2}, t3: {text3}");

                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger(triggerName, value1, value2, value3, text1, text2, text3);
            }
            else if (message.address == "/NyaVMC/Ext/Bone/Scale")
            {
                HumanBodyBones bone;
                if (Enum.TryParse<HumanBodyBones>((string)message.values[0], out bone))
                {
                    if ((animator != null) && (bone != HumanBodyBones.LastBone) && (bone != HumanBodyBones.Hips))
                    {
                        Vector3 newScale = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);

                        var t = animator.GetBoneTransform(bone);
                        if (t != null)
                        {
                            t.localScale = newScale;
                        }
                    }
                }
            }
        }
    }
}
