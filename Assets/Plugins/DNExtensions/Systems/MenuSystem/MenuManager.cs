
using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;

namespace DNExtensions.Systems.MenuSystem
{

    [DisallowMultipleComponent]
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu Settings")] 
        [SerializeField] private bool autoShowFirstScreen = true;
        [SerializeField, EnableIf("autoShowFirstScreen")]
        private bool animateFirstScreen = true;
        [SerializeField] private bool crossAnimateScreens;


        private readonly List<Screen> _screens = new List<Screen>();
        private Screen _currentScreen;

        private void Awake()
        {
            _screens.AddRange(GetComponentsInChildren<Screen>(true));
        }

        private void Start()
        {
            HideAllScreens(false);
            if (autoShowFirstScreen) ShowScreen(_screens[0], animateFirstScreen);
        }

        private void HideAllScreens(bool animated)
        {
            foreach (var screen in _screens)
            {
                screen.Hide(animated);
            }

            _currentScreen = null;
        }

        public void ShowScreen(Screen screen, bool animated = true, Action onComplete = null)
        {
            if (!screen)
            {
                Debug.LogWarning($"Screen {screen.name} is invalid.");
                return;
            }


            if (crossAnimateScreens)
            {
                if (_currentScreen)
                {
                    _currentScreen.Hide(animated);
                }

                screen.Show(animated, onComplete);
                _currentScreen = screen;

            }
            else
            {
                if (_currentScreen)
                {
                    _currentScreen.Hide(animated, () =>
                    {
                        screen.Show(animated, onComplete);
                        _currentScreen = screen;
                    });
                }
                else
                {
                    screen.Show(animated, onComplete);
                    _currentScreen = screen;
                }
            }


        }


        public void ShowNextScreen(bool animated = true, Action onComplete = null)
        {
            if (!_currentScreen)
            {
                ShowScreen(_screens[0], animated, onComplete);
            }

            var currentIndex = _screens.IndexOf(_currentScreen);
            var nextIndex = currentIndex + 1;

            ShowScreen(nextIndex >= _screens.Count ? _screens[0] : _screens[nextIndex], animated, onComplete);
        }

        public void ShowPreviousScreen(bool animated = true, Action onComplete = null)
        {
            if (!_currentScreen)
            {
                ShowScreen(_screens[0], animated, onComplete);
            }

            var currentIndex = _screens.IndexOf(_currentScreen);
            var previousIndex = currentIndex - 1;

            ShowScreen(previousIndex < 0 ? _screens[^1] : _screens[previousIndex], animated, onComplete);
        }


        public void ShowScreenAnimated(Screen screen) => ShowScreen(screen, true, null);
        public void ShowScreenInstant(Screen screen) => ShowScreen(screen, false, null);

        public void ShowNextScreenAnimated() => ShowNextScreen(true, null);
        public void ShowNextScreenInstant() => ShowNextScreen(false, null);

        public void ShowPreviousScreenAnimated() => ShowPreviousScreen(true, null);
        public void ShowPreviousScreenInstant() => ShowPreviousScreen(false, null);
        
        public void HideCurrentScreen(bool animated = true) => _currentScreen?.Hide(animated);
        




    }
}