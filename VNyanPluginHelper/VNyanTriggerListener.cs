using System;
using UnityEngine;
using VNyanInterface;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    class VNyanTriggerListener : MonoBehaviour, ITriggerHandler
    {
        private string triggerPrefix;
        public event Action<string> TriggerFired;

        public void Awake()
        {

        }

        public void triggerCalled(string triggerName)
        {
            //If we're presented with a null org or haven't set a prefix, do nothing
            if (triggerPrefix == null) { return; }
            if (triggerName == null) { return; }
            if (!triggerName.StartsWith(triggerPrefix)) { return; }

            TriggerFired.Invoke(triggerName);
        }
        public void Listen(string prefix)
        {
            triggerPrefix = prefix;
        }

        public void StopListen()
        {
            triggerPrefix = null;
        }


    }
}
