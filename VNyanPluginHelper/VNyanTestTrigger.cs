using System;
using System.Collections.Generic;
using System.Text;
using VNyanInterface;
using UnityEngine;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    
    class VNyanTestTriggerData
    {
        public string triggerName = "";

        public int value1 = 0;
        public int value2 = 0;
        public int value3 = 0;

        public string text1 = "";
        public string text2 = "";
        public string text3 = "";
    }

    class VNyanTestTrigger: MonoBehaviour, ITriggerInterface
    {
        private VNyanTestTrigger _instance;
        private Action<string, int, int, int, string, string, string> triggerFired;
        private Queue<VNyanTestTriggerData> triggerQueue = new Queue<VNyanTestTriggerData>();

        public void registerTriggerListener(ITriggerHandler triggerHandler)
        {
            triggerFired += triggerHandler.triggerCalled;
        }

        public void callTrigger(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            Debug.Log($"Enqueueing trigger {triggerName}");
            if (_instance == null)
            {
                return;
            }

            lock (triggerQueue)
            {
                VNyanTestTriggerData trigger = new VNyanTestTriggerData
                {
                    triggerName = triggerName,
                    value1 = value1,
                    value2 = value2,
                    value3 = value3,
                    text1 = text1,
                    text2 = text2,
                    text3 = text3
                };
                triggerQueue.Enqueue(trigger);
            }
        }

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

        private void Update()
        {
            if (_instance == null)
            {
                return;
            }
            if(triggerFired == null) 
            { 
                return;
            }

            lock (triggerQueue)
            {
                while (triggerQueue.Count > 0)
                {
                    VNyanTestTriggerData trigger = triggerQueue.Dequeue();
                    triggerFired.Invoke(trigger.triggerName, trigger.value1, trigger.value2, trigger.value3, trigger.text1, trigger.text2, trigger.text3);
                }
            }
        }
    }
}
