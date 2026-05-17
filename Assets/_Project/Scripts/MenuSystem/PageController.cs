using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MenuSystem.Pages
{
    public class PageController : MonoBehaviour
    {
        public static PageController instance;

        [SerializeField] Page initialPage;
        [SerializeField] List<Page> pages;

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

            //pages = new List<Page>();
            //pages = GetComponentsInChildren<Page>().ToList();

            DisableAllPagesButInitial();
        }

        private void Start()
        {
            if (initialPage != null)
            {
                OpenPage(initialPage);
            }
        }

        public void OpenPage(string pageName)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].pageName == pageName)
                {
                    pages[i].Open();
                }
                else if (pages[i].open)
                {
                    ClosePage(pages[i]);
                }
            }
        }

        public void OpenPage(Page page)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].open)
                {
                    ClosePage(pages[i]);
                }
            }
            page.Open();
        }

        public void ClosePage(Page page)
        {
            page.Close();
        }

        private void DisableAllPagesButInitial()
        {
            foreach (var page in pages)
            {
                if(page != initialPage) 
                {
                    page.gameObject.SetActive(false);
                    page.Close();
                }
            }
        }
    }
} 
