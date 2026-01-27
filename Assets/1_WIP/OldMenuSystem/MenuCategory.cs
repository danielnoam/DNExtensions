using UnityEngine;
using System.Collections.Generic;
using DNExtensions.Utilities;
using PrimeTween;
using UnityEngine.InputSystem;

namespace  DNExtensions.Utilities.MenuSystemOld
{
    
    [DisallowMultipleComponent]
    public class MenuCategory : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private bool alwaysSelectFirstPageOnSelect = true;
        [SerializeField] private List<MenuPage> pages = new List<MenuPage>();
        [SerializeField, HideInInspector] private MenuController controller;

        [Space(10)] 
        [ReadOnly, SerializeField] private MenuPage currentPage;


        private Sequence _pageChangeSequence;
        public MenuPage CurrentPage => currentPage;
        public bool IsAtFirstPage => pages[0].gameObject.activeSelf;


        private void OnValidate()
        {

            if (!controller) controller = GetComponentInParent<MenuController>();


            // Find and add all pages in the category
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out MenuPage category)) continue;
                if (!pages.Contains(category))
                {
                    pages.Add(category);
                }
            }

            pages.RemoveAll(page => page == null);
        }



        public void OnNavigate(InputAction.CallbackContext context)
        {
            if (currentPage != null)
            {
                currentPage.OnNavigate(context);
            }
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            if (!context.started || currentPage == null) return;

            if (currentPage != pages[0])
            {
                SelectFirstPage();
            }

        }

        public void OnCategorySelected()
        {
            if (alwaysSelectFirstPageOnSelect)
            {
                SelectFirstPage();

            }
            else if (currentPage)
            {
                SelectPage(currentPage);
            }
        }

        public float OnCategoryDeselected()
        {
            var delayBeforeDisable = 0f;

            if (currentPage != null)
            {
                delayBeforeDisable = currentPage.OnPageDeselected();
            }

            if (!controller.KeepAllPagesActive)
            {
                if (delayBeforeDisable > 0)
                {
                    if (_pageChangeSequence.isAlive) _pageChangeSequence.Stop();

                    _pageChangeSequence = Sequence.Create()
                        .ChainDelay(delayBeforeDisable)
                        .ChainCallback(() => { DisableAllPagesInCategory(); });
                }
                else
                {
                    DisableAllPagesInCategory();
                }
            }

            return delayBeforeDisable;
        }



        public void SelectPage(MenuPage newPage)
        {
            if (!newPage || !pages.Contains(newPage)) return;

            var delayBeforeNextPage = 0f;

            if (currentPage)
            {
                delayBeforeNextPage = currentPage.OnPageDeselected();
            }

            if (delayBeforeNextPage > 0)
            {
                if (_pageChangeSequence.isAlive) _pageChangeSequence.Stop();

                _pageChangeSequence = Sequence.Create()
                    .ChainDelay(delayBeforeNextPage)
                    .ChainCallback(() =>
                    {
                        if (!controller.KeepAllPagesActive)
                        {
                            if (currentPage)
                            {
                                currentPage.gameObject.SetActive(false);
                            }

                            newPage.gameObject.SetActive(true);
                        }

                        currentPage = newPage;
                        newPage.OnPageSelected();
                    });
            }
            else
            {
                if (!controller.KeepAllPagesActive)
                {
                    if (currentPage)
                    {
                        currentPage.gameObject.SetActive(false);
                    }

                    newPage.gameObject.SetActive(true);
                }

                currentPage = newPage;
                newPage.OnPageSelected();
            }
        }

        private void SelectFirstPage()
        {
            if (pages.Count > 0)
            {
                SelectPage(pages[0]);
            }
        }

        private void DisableAllPagesInCategory()
        {
            foreach (MenuPage page in pages)
            {
                page.gameObject.SetActive(false);
            }

            currentPage = null;
        }

        private void EnableAllPagesInCategory()
        {
            foreach (MenuPage page in pages)
            {
                page.gameObject.SetActive(true);
            }
        }


    }
}