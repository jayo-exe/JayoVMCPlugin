using System.Collections.Generic;
using UnityEngine;
using VNyanInterface;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    class VNyanTestAvatar : MonoBehaviour, IAvatarInterface
    {
        private VNyanTestAvatar _instance;
        public GameObject avatar;
        public Dictionary<string, float> blendshapeOverrides = new Dictionary<string, float>();
        public Dictionary<string, float> blendshapes = new Dictionary<string, float>();
        public Dictionary<string, float> blendshapesLastFrame = new Dictionary<string, float>();
        public List<IPoseLayer> poseLayers = new List<IPoseLayer>();

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

        public void setBlendshapeOverride(string name, float value)
        {
            blendshapeOverrides.Add(name,value);
        }
        
        public void clearBlendshapeOverride(string name)
        {
            blendshapeOverrides.Remove(name);
        }

        public void registerBlendshapeProcessingListener(IBlendshapeProcessingListener listener)
        {
            // For the time being, this will go unimplemented, as I can't come up with a way
            // to approximate the work being done by the real system
        }

        public void registerPoseLayer(IPoseLayer layer)
        {
            poseLayers.Add(layer);
        }

        public object getAvatarObject()
        {
            return avatar;
        }

        public float getBlendshapeInstant(string name)
        {
            float foundValue = 0;
            blendshapes.TryGetValue(name, out foundValue);
            return foundValue;
        }
        
        public float getBlendshapeLastFrame(string name)
        {
            float foundValue = 0;
            blendshapesLastFrame.TryGetValue(name, out foundValue);
            return foundValue;
        }
        
        public Dictionary<string, float> getBlendshapesInstant()
        {
            return blendshapes;
        }
        
        
    }
}
