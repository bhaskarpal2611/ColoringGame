using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorSwipeGame
{
    public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance { get { return _instance; } }


        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this as T;
                //DontDestroyOnLoad(Instance);
            }
        }
    }
}
