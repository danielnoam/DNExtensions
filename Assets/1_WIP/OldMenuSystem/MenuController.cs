

using System.Collections.Generic;
using DNExtensions.Utilities;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

namespace  DNExtensions.MenuSystemOld
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MenuInput))]
    public class MenuController : MonoBehaviour
    {

        [Header("Menu Settings")] 
        [SerializeField] private bool keepAllCategoriesActive;
        [SerializeField] private bool keepAllPagesActive;
        [SerializeField] private List<MenuCategory> menuCategories = new List<MenuCategory>();


        [Space(10)] 
        [SerializeField, ReadOnly] private MenuCategory currentCategory;
        [SerializeField, ReadOnly] private MenuInput inputReader;


        private bool _receiveInput = true;
        private Sequence _categoryChangeSequence;
        
        
        public MenuCategory CurrentCategory => currentCategory;
        public bool KeepAllPagesActive => keepAllPagesActive;
        
        

        private void OnValidate()
        {

            if (!inputReader) inputReader = GetComponent<MenuInput>();

            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out MenuCategory category)) continue;
                if (!menuCategories.Contains(category))
                {
                    menuCategories.Add(category);
                }
            }

            menuCategories.RemoveAll(menu => !menu);
        }

        private void OnEnable()
        {
            if (!keepAllCategoriesActive)
            {
                DisableAllCategoriesInMenu();
            }
            else
            {
                EnableAllCategoriesInMenu();
            }

            SelectFirstCategory();

            inputReader.OnNavigateAction += OnNavigate;
            inputReader.OnCancelAction += OnCancel;
        }

        private void OnDisable()
        {
            inputReader.OnNavigateAction -= OnNavigate;
            inputReader.OnCancelAction -= OnCancel;
        }

        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (!_receiveInput) return;

            if (currentCategory)
            {
                currentCategory.OnNavigate(context);
            }
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            if (!_receiveInput || !currentCategory) return;


            if (!currentCategory.IsAtFirstPage)
            {
                currentCategory.OnCancel(context);

            }
            else if (currentCategory != menuCategories[0])
            {

                SelectFirstCategory();
            }

        }


        public void SelectCategory(MenuCategory newCategory)
        {
            if (newCategory == null || !menuCategories.Contains(newCategory)) return;

            var delayBeforeCategoryChange = 0f;

            if (currentCategory)
            {
                if (currentCategory.CurrentPage)
                {
                    delayBeforeCategoryChange = currentCategory.CurrentPage.OnPageDeselected();
                }

                currentCategory.OnCategoryDeselected();
            }

            if (delayBeforeCategoryChange > 0)
            {
                if (_categoryChangeSequence.isAlive) _categoryChangeSequence.Stop();

                _categoryChangeSequence = Sequence.Create()
                    .ChainDelay(delayBeforeCategoryChange)
                    .ChainCallback(() => { CompleteCategoryChange(newCategory); });
            }
            else
            {
                CompleteCategoryChange(newCategory);
            }
        }

        private void CompleteCategoryChange(MenuCategory newCategory)
        {
            if (!keepAllCategoriesActive)
            {
                if (currentCategory)
                {
                    currentCategory.gameObject.SetActive(false);
                }

                newCategory.gameObject.SetActive(true);
            }

            currentCategory = newCategory;
            newCategory.OnCategorySelected();
        }

        private void SelectFirstCategory()
        {
            if (menuCategories.Count > 0 && menuCategories[0])
            {
                SelectCategory(menuCategories[0]);
            }
        }

        private void DisableAllCategoriesInMenu()
        {
            foreach (MenuCategory category in menuCategories)
            {
                category.gameObject.SetActive(false);
            }

            currentCategory = null;
        }

        private void EnableAllCategoriesInMenu()
        {
            foreach (MenuCategory category in menuCategories)
            {
                category.gameObject.SetActive(true);
            }
        }

        private void ToggleInput(bool state)
        {
            _receiveInput = state;
        }

    }
}