using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MenuSystem.Pages
{
    public class Page : MonoBehaviour
    {
        public string pageName;
        public bool open;

        public void Open()
        {
            open = true;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            open = false;
            gameObject.SetActive(false);
        }
    }
}
