# DNExtensions to do

- [ ] **Controller rumble ignores pauses.** `ControllerRumbleListener` advances its effects with `Time.deltaTime`, so
      a rumble that is playing when the game sets `Time.timeScale` to 0 (a pause menu or window) never finishes and
      the pad keeps rumbling. Time the effects with `Time.unscaledDeltaTime`, or give the listener a setting for which
      time to use. ElectroGrid works around it for now: pausing calls `DisableAllRumbleEffects` on every listener and
      no rumble starts while the time scale is 0 (`MobileHaptics.StopRumble`).
