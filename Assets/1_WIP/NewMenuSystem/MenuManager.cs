using System;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private bool autoShowFirstScreen = true;
    [SerializeField, EnableIf("autoShowFirstScreen")] private bool animateFirstScreen = true;
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
    
    
    public void ShowScreenAnimated(Screen screen) => ShowScreen(screen, true,null);
    public void ShowScreenInstant(Screen screen) => ShowScreen(screen, false,null);
    

}