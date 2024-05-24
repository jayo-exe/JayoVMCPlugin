using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JayoVMCPlugin.VNyanPluginHelper
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private MainThreadDispatcher _instance;
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();

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
            
            lock (_actionQueue)
            {
                while (_actionQueue.Count > 0)
                {
                    _actionQueue.Dequeue().Invoke();
                }
            }
        }

        public void Enqueue(Action action)
        {
            if (_instance == null)
            {
                return;
            }

            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }
    }
}
