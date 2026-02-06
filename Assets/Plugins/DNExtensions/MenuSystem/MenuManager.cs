
using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;

namespace DNExtensions.MenuSystem
{

    [DisallowMultipleComponent]
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu Settings")] 
        [SerializeField] private bool autoShowFirstScreen = true;
        [SerializeField, EnableIf("autoShowFirstScreen")]
        private bool animateFirstScreen = true;
        [SerializeField] private bool crossAnimateScreens;


        private readonly List<Screen> screens = new List<Screen>();
        private Screen currentScreen;

        private void Awake()
        {
            screens.AddRange(GetComponentsInChildren<Screen>(true));
        }

        private void Start()
        {
            HideAllScreens(false);
            if (autoShowFirstScreen) ShowScreen(screens[0], animateFirstScreen);
        }

        private void HideAllScreens(bool animated)
        {
            foreach (var screen in screens)
            {
                screen.Hide(animated);
            }

            currentScreen = null;
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
                if (currentScreen)
                {
                    currentScreen.Hide(animated);
                }

                screen.Show(animated, onComplete);
                currentScreen = screen;

            }
            else
            {
                if (currentScreen)
                {
                    currentScreen.Hide(animated, () =>
                    {
                        screen.Show(animated, onComplete);
                        currentScreen = screen;
                    });
                }
                else
                {
                    screen.Show(animated, onComplete);
                    currentScreen = screen;
                }
            }


        }


        public void ShowNextScreen(bool animated = true, Action onComplete = null)
        {
            if (!currentScreen)
            {
                ShowScreen(screens[0], animated, onComplete);
            }

            var currentIndex = screens.IndexOf(currentScreen);
            var nextIndex = currentIndex + 1;

            ShowScreen(nextIndex >= screens.Count ? screens[0] : screens[nextIndex], animated, onComplete);
        }

        public void ShowPreviousScreen(bool animated = true, Action onComplete = null)
        {
            if (!currentScreen)
            {
                ShowScreen(screens[0], animated, onComplete);
            }

            var currentIndex = screens.IndexOf(currentScreen);
            var previousIndex = currentIndex - 1;

            ShowScreen(previousIndex < 0 ? screens[^1] : screens[previousIndex], animated, onComplete);
        }


        public void ShowScreenAnimated(Screen screen) => ShowScreen(screen, true, null);
        public void ShowScreenInstant(Screen screen) => ShowScreen(screen, false, null);

        public void ShowNextScreenAnimated() => ShowNextScreen(true, null);
        public void ShowNextScreenInstant() => ShowNextScreen(false, null);

        public void ShowPreviousScreenAnimated() => ShowPreviousScreen(true, null);
        public void ShowPreviousScreenInstant() => ShowPreviousScreen(false, null);
        
        public void HideCurrentScreen(bool animated = true) => currentScreen?.Hide(animated);
        




    }
}