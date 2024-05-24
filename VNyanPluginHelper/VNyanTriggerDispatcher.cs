using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    class VNyanTriggerDispatcher : MonoBehaviour
    {
        private Queue<string> triggerQueue;
        private GameObject triggerSender;
        private CallVNyanTrigger triggerSenderComponent;

        private void ensureInit()
        {
            if (triggerQueue == null)
            {
                triggerQueue = new Queue<string>();
            }
            if (triggerSender == null)
            {
                triggerSender = new GameObject("TriggerSender");
                triggerSender.transform.SetParent(gameObject.transform);
                triggerSenderComponent = triggerSender.AddComponent<CallVNyanTrigger>();
            }
        }

        public void callVNyanTrigger(string triggerName)
        {
            if (triggerName == null || triggerName == "")
            {
                return;
            }

            ensureInit();

            lock (triggerQueue)
            {
                triggerQueue.Enqueue(triggerName);
            }
        }

        public void Awake()
        {
            ensureInit();
            Debug.Log("VNyan Trigger Dispatcher Awake!");
        }

        private void Update()
        {
            lock (triggerQueue)
            {
                while (triggerQueue.Count > 0)
                {
                    triggerSenderComponent.TriggerName = triggerQueue.Dequeue();
                    triggerSender.SetActive(true);
                    triggerSender.SetActive(false);
                }
                triggerSenderComponent.TriggerName = "";
            }
        }
    }
}
