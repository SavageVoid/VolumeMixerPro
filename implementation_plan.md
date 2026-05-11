# Audited Implementation Plan - Trackpad 4-Finger Volume Control

This plan details the addition of a high-fidelity 4-finger vertical swipe gesture to control the volume of the active application globally.

## User Review Required

> [!IMPORTANT]
> **"Conflict-Free" Hijacking**: To ensure the 4-finger gesture doesn't trigger Windows 11 system actions (like switching desktops or Task View), we will use a **synchronized blocking** approach. While 4 fingers are touching the pad, the utility will temporarily block the `Win`, `Tab`, and `D` keys at the kernel-level hook.
> 
> **Hardware Support**: This feature requires a **Windows Precision Touchpad**. Legacy touchpads that emulate a standard mouse will not be compatible with multi-finger detection.

## Proposed Changes

### [Component] Global State Coordination
#### [MODIFY] [HotkeyService.cs](file:///C:/Users/Vashu/Documents/AutoHotkey/volumeToggle/VolumeMixerPro/HotkeyService.cs)
- Add a static property `public static bool Is4FingerGestureActive { get; set; }`.
- Update `KeyboardHookCallback`:
    - If `Is4FingerGestureActive` is true, intercept and block (`return (IntPtr)1`) the following keys: `VK_LWIN`, `VK_RWIN`, `VK_TAB`, `VK_D`, `VK_LEFT`, `VK_RIGHT`.
    - This "hijacks" the gesture away from the Windows Shell.

### [Component] Trackpad Service (Raw Input)
#### [NEW] [TrackpadService.cs](file:///C:/Users/Vashu/Documents/AutoHotkey/volumeToggle/VolumeMixerPro/TrackpadService.cs)
- **Engine**: Uses `RegisterRawInputDevices` for `UsagePage 0x0D` (Digitizers) / `Usage 0x05` (Touch Pad).
- **HID Parser**: Uses `HidP_GetCaps` to dynamically locate the `ContactCount` and `Y-Position` values in the raw HID report.
- **Gesture Detection**:
    - If `ContactCount == 4`:
        - Set `HotkeyService.Is4FingerGestureActive = true`.
        - Track the average vertical movement (Delta Y).
        - Fire `OnGestureScroll(int direction)` based on a 50-pixel threshold.
    - If `ContactCount < 4`:
        - Reset `HotkeyService.Is4FingerGestureActive = false`.

### [Component] Settings & UI
#### [MODIFY] [SettingsManager.cs](file:///C:/Users/Vashu/Documents/AutoHotkey/volumeToggle/VolumeMixerPro/SettingsManager.cs)
- Add `public bool EnableTrackpadGesture { get; set; } = false;`

#### [MODIFY] [SettingsWindow.xaml](file:///C:/Users/Vashu/Documents/AutoHotkey/volumeToggle/VolumeMixerPro/SettingsWindow.xaml)
- Add a Toggle/CheckBox for "Enable 4-Finger Trackpad Volume Control" in the General tab.

### [Component] Main Orchestration
#### [MODIFY] [MainWindow.xaml.cs](file:///C:/Users/Vashu/Documents/AutoHotkey/volumeToggle/VolumeMixerPro/MainWindow.xaml.cs)
- Instantiate `TrackpadService`.
- Subscribe to `OnGestureScroll` -> Call `HandleVolumeScroll`.
- Ensure the service is only active if `EnableTrackpadGesture` is true.

## Verification Plan

### Automated Tests
- Build verification.
- Mock HID data test to verify that 4-finger packets correctly toggle the `Is4FingerGestureActive` flag.

### Manual Verification
- **Test 1**: With the setting OFF, verify 4-finger swipes still trigger Windows defaults (e.g., Task View).
- **Test 2**: With the setting ON, verify 4-finger swipes ONLY change volume and show our overlay, with NO Windows Shell interference.
- **Test 3**: Verify lifting fingers immediately restores normal keyboard functionality.
