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
        private Dictionary<string, Animator> childAnimators = new Dictionary<string, Animator>();

        private VRMBlendShapeProxy blendShapeProxy = null;
        private Dictionary<BlendShapeKey, float> blends = new Dictionary<BlendShapeKey, float>();
        private Dictionary<BlendShapeKey, float> newBlends = new Dictionary<BlendShapeKey, float>();
        private Dictionary<BlendShapeKey, float> prevBlends = new Dictionary<BlendShapeKey, float>();
        private Dictionary<BlendShapeKey, float> currentBlends = new Dictionary<BlendShapeKey, float>();
        private Dictionary<string, BlendShapeKey> ValidBlendshapes = new Dictionary<string, BlendShapeKey>();

        private Dictionary<string, Vector3> prevBonePositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Vector3> prevBoneScales = new Dictionary<string, Vector3>();
        private Dictionary<string, Quaternion> prevBoneRotations = new Dictionary<string, Quaternion>();
        private Dictionary<string, Vector3> newBonePositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Vector3> newBoneScales = new Dictionary<string, Vector3>();
        private Dictionary<string, Quaternion> newBoneRotations = new Dictionary<string, Quaternion>();
        private int updateFrameInterval = 1;
        private int currentFrameInterval = 0;


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

                if (currentFrameInterval < updateFrameInterval) currentFrameInterval++;
                //Debug.Log($"FrameIndex: {currentFrameInterval} / {updateFrameInterval}");

                float lerpFactor = (float)currentFrameInterval / (float)updateFrameInterval;

                Vector3 pos = Vector3.Lerp(prevBonePositions["root"], newBonePositions["root"], lerpFactor);
                Quaternion rot = Quaternion.Lerp(prevBoneRotations["root"], newBoneRotations["root"], lerpFactor);
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

                foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (bone == HumanBodyBones.LastBone) continue;
                    if (bone == HumanBodyBones.Hips) continue;

                    string boneName = bone.ToString();
                    if (!prevBoneRotations.ContainsKey(boneName)) continue;
                    if (!newBoneRotations.ContainsKey(boneName)) continue;
                    //Debug.Log($"LERP POS {boneName}; {prevBonePositions[boneName].ToString("F6")}; {newBonePositions[boneName].ToString("F6")}; {Vector3.Distance(prevBonePositions[boneName], newBonePositions[boneName])}; {lerpFactor}");
                    //Debug.Log($"LERP ROT {boneName}; {prevBoneRotations[boneName].ToString("F6")}; {newBoneRotations[boneName].ToString("F6")}; {lerpFactor}");
                    pos = Vector3.Lerp(prevBonePositions[boneName], newBonePositions[boneName], lerpFactor);
                    rot = Quaternion.Lerp(prevBoneRotations[boneName], newBoneRotations[boneName], lerpFactor);

                    Transform boneTransform = animator.GetBoneTransform(bone);
                    if (boneTransform != null)
                    {
                        if (!BoneRotationsOnly)
                        {
                            boneTransform.localPosition = pos;
                        }

                        boneTransform.localRotation = rot;

                        if (!prevBoneScales.ContainsKey(boneName)) continue;
                        if (!newBoneScales.ContainsKey(boneName)) continue;

                        Vector3 scale = Vector3.Lerp(prevBoneScales[boneName], newBoneScales[boneName], lerpFactor);
                        boneTransform.localScale = scale;
                        if(lerpFactor == 1.00)
                        {
                            prevBoneScales.Remove(boneName);
                            newBoneScales.Remove(boneName);
                        }
                    }
                }

                if (blendShapeProxy != null)
                {
                    foreach(KeyValuePair<BlendShapeKey, float> newBlendShape in newBlends)
                    {
                        currentBlends[newBlendShape.Key] = Mathf.Lerp(prevBlends[newBlendShape.Key], newBlends[newBlendShape.Key], lerpFactor); 
                    }
                    blendShapeProxy.SetValues(currentBlends);
                }
                currentBlends.Clear();


            }
            catch (Exception e)
            {
                Debug.Log($"Receiver Error: {e.Message}");
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

                refetchChildAnimators();
                
                //disable anim param links on models attached to receivers
                AnimParamLink[] paramLinks = Model.GetComponentsInChildren<AnimParamLink>(true);
                foreach (AnimParamLink paramLink in paramLinks) paramLink.enabled = false;

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

                if (newBonePositions.ContainsKey("root")) prevBonePositions["root"] = new Vector3(newBonePositions["root"].x, newBonePositions["root"].y, newBonePositions["root"].z);
                if (newBoneRotations.ContainsKey("root")) prevBoneRotations["root"] = new Quaternion(newBoneRotations["root"].x, newBoneRotations["root"].y, newBoneRotations["root"].z, newBoneRotations["root"].w);
                newBonePositions["root"] = pos;
                newBoneRotations["root"] = rot;

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
                        string boneName = bone.ToString();
                        var t = animator.GetBoneTransform(bone);
                        if (t != null)
                        {
                            if (newBonePositions.ContainsKey(boneName)) prevBonePositions[boneName] = new Vector3(newBonePositions[boneName].x, newBonePositions[boneName].y, newBonePositions[boneName].z);
                            if (newBoneRotations.ContainsKey(boneName)) prevBoneRotations[boneName] = new Quaternion(newBoneRotations[boneName].x, newBoneRotations[boneName].y, newBoneRotations[boneName].z, newBoneRotations[boneName].w);
                            newBonePositions[boneName] = pos;
                            newBoneRotations[boneName] = rot;
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
                    prevBlends = new Dictionary<BlendShapeKey, float>(newBlends);
                    newBlends = new Dictionary<BlendShapeKey, float>(blends);
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
                            string boneName = bone.ToString();
                            prevBoneScales[boneName] = t.localScale;
                            newBoneScales[boneName] = newScale;
                        }
                    }
                }
            }
            else if (message.address == "/NyaVMC/Ext/Anim/IntParam")
            {
                if (childAnimators.ContainsKey((string)message.values[0])) childAnimators[(string)message.values[0]].SetInteger((int)message.values[1], (int)message.values[2]);
            }
            else if (message.address == "/NyaVMC/Ext/Anim/FloatParam")
            {
                if (childAnimators.ContainsKey((string)message.values[0]))
                {
                    Debug.Log($"Float Parameter Received: (string)message.values[0] {(int)message.values[1]} {(float)message.values[2]}");
                    childAnimators[(string)message.values[0]].SetFloat((int)message.values[1], (float)message.values[2]);
                }
            }
            else if (message.address == "/NyaVMC/Ext/Anim/BoolParam")
            {
                if (childAnimators.ContainsKey((string)message.values[0])) childAnimators[(string)message.values[0]].SetBool((int)message.values[1], (bool)message.values[2]);
            }
            else if (message.address == "/NyaVMC/Ext/Anim/TriggerParam")
            {
                if (childAnimators.ContainsKey((string)message.values[0])) childAnimators[(string)message.values[0]].SetTrigger((int)message.values[1]);
            }
            else if (message.address == "/NyaVMC/F")
            {
                updateFrameInterval = (int)message.values[0];
                currentFrameInterval = 0;
            }
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

            Animator[] animators = Model.GetComponentsInChildren<Animator>(true);
            for(int i = 0; i < animators.Length; i++)
            {
                string relativePath = getObjectPath(animators[i].transform, Model.transform);
                childAnimators[relativePath] = animators[i];
            }
        }
    }
}
