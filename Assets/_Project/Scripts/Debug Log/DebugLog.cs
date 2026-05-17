using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Debug_Log
{
    public class DebugLog : MonoBehaviour
    {
        public static DebugLog instance;

        [SerializeField] TextMeshProUGUI textDebug;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        public void SetDebugLog(string _string = "")
        {
            textDebug.text = _string;
        }
    }
}