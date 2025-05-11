using System;
using System.Collections;
using UnityEngine;

namespace SmartAction.Utils
{
    public class CoroutineRunner : MonoBehaviour
    {
        public static event Action OnUpdate;
        private static CoroutineRunner _instance;

        public static Coroutine Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                var go = new GameObject("TimeStretchCoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }
            return _instance.StartCoroutine(routine);
        }  
        public static void Stop(Coroutine routine)
        {
            if (_instance != null && routine != null)
            {
                _instance.StopCoroutine(routine);
            }
        }
        public static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("TimeStretchCoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }
        }
        public static void RegisterUpdate(Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject("TimeStretchCoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }

            OnUpdate += action;
        }

        public static void UnregisterUpdate(Action action)
        {
            if (_instance != null)
            {
                OnUpdate -= action;
            }
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}