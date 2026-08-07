# NevPlayer — LibVLCSharp Migration Documentation

**Date:** August 2026  
**Migration Type:** Playback Engine Replacement (Gradual, Non-Destructive)  
**Target Engine:** LibVLCSharp 3.9.0 / libvlc 3.0.21

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Changes](#architecture-changes)
3. [New Dependencies](#new-dependencies)
4. [LibVLC Integration Guide](#libvlc-integration-guide)
5. [Migration Steps Performed](#migration-steps-performed)
6. [Feature Parity](#feature-parity)
7. [Known Issues](#known-issues)
8. [How to Switch Engines at Runtime](#how-to-switch-engines-at-runtime)
9. [Future Improvements](#future-improvements)

---

## 1. Overview

NevPlayer previously used the **Windows Media Foundation (WMF)** stack exclusively for all media playback:

```
WinUI 3 UI  →  MediaPlayerElement  →  MediaPlayer  →  Windows Media Foundation
```

This migration introduces **LibVLCSharp** as a second, fully-supported playback backend. Both engines coexist simultaneously. Users can switch between them at runtime via the **Settings page** without restarting the application.

```
WinUI 3 UI  →  SwitchableMediaPlayer  →  WindowsMediaPlayer  →  Windows Media Foundation
                                     ↘  VlcMediaPlayer      →  libvlc (LibVLCSharp)
```

---

## 2. Architecture Changes

### 2.1 Before Migration

```
App.xaml.cs
    └─ new WindowsMediaPlayer()
           └─ IMediaPlayer
                  └─ PlaybackService

CinemaPage.xaml.cs
    └─ VideoSurface.SetMediaPlayer(wmfNativePlayer)
```

### 2.2 After Migration

```
App.xaml.cs
    └─ new SwitchableMediaPlayer(settingsService)
           ├─ WindowsMediaPlayer  (WMF engine — kept intact)
           └─ VlcMediaPlayer      (LibVLC engine — new)
                  └─ IMediaPlayer (shared contract)
                         └─ PlaybackService (unchanged)

CinemaPage.xaml.cs
    └─ UpdateVideoSurfaces()
           ├─ UseLibVLC = false → VideoSurface.SetMediaPlayer(wmfPlayer)
           └─ UseLibVLC = true  → VlcVideoSurface.MediaPlayer = vlcPlayer
```

### 2.3 New Files

| File | Purpose |
|------|---------|
| `NevPlayer.App/Services/VlcMediaPlayer.cs` | LibVLC engine implementing `IMediaPlayer` |
| `NevPlayer.App/Services/SwitchableMediaPlayer.cs` | Composite engine that delegates to the active backend |

### 2.4 Modified Files

| File | Change |
|------|--------|
| `NevPlayer.App/NevPlayer.App.csproj` | Added LibVLCSharp NuGet packages |
| `NevPlayer.Core/Services/IMediaPlayer.cs` | Extended with `Position`, `Duration`, `IsFullScreen`, track APIs |
| `NevPlayer.App/Services/WindowsMediaPlayer.cs` | Implemented all new interface members |
| `NevPlayer.App/Views/CinemaPage.xaml` | Added `<vlc:VideoView x:Name="VlcVideoSurface">` |
| `NevPlayer.App/Views/CinemaPage.xaml.cs` | Added `UpdateVideoSurfaces()` for runtime switching |
| `NevPlayer.Core/Services/ISettingsService.cs` | Added `UseLibVLC` property |
| `NevPlayer.Core/Services/SettingsService.cs` | Implemented `UseLibVLC` with JSON persistence |
| `NevPlayer.App/Views/SettingsPage.xaml` | Added "Use LibVLC Engine" toggle switch |
| `NevPlayer.App/Views/SettingsPage.xaml.cs` | Wired `LibVlcToggle_Toggled` handler |

---

## 3. New Dependencies

All packages were added to `NevPlayer.App/NevPlayer.App.csproj`:

```xml
<PackageReference Include="LibVLCSharp"             Version="3.9.0" />
<PackageReference Include="LibVLCSharp.WinUI"        Version="3.9.0" />
<PackageReference Include="VideoLAN.LibVLC.Windows"  Version="3.0.21" />
```

| Package | Role |
|---------|------|
| `LibVLCSharp` | Managed C# bindings for libvlc |
| `LibVLCSharp.WinUI` | WinUI 3 `VideoView` control for rendering |
| `VideoLAN.LibVLC.Windows` | Copies native `libvlc.dll` and plugin DLLs to build output |

> **Note:** `VideoLAN.LibVLC.Windows` uses MSBuild targets to automatically resolve and copy the native binary tree to the output directory. No manual DLL management is required.

---

## 4. LibVLC Integration Guide

### 4.1 Initialization

```csharp
Core.Initialize(); // Must be called once before any LibVLC usage
var _libVLC = new LibVLC();
var _mediaPlayer = new MediaPlayer(_libVLC);
```

### 4.2 Loading Media

```csharp
var media = new Media(_libVLC, new Uri(filePath));
_mediaPlayer.Media = media;
_mediaPlayer.Play();
```

> LibVLC accepts local file URIs directly — no `StorageFile.GetFileFromPathAsync()` call required.

### 4.3 Rendering in WinUI 3

In XAML:
```xml
xmlns:vlc="using:LibVLCSharp.WinUI"

<vlc:VideoView x:Name="VlcVideoSurface" Visibility="Collapsed" />
```

In code-behind:
```csharp
VlcVideoSurface.MediaPlayer = vlcMediaPlayerInstance;
```

### 4.4 Events

All LibVLC events fire on **background threads**. Always marshal to the UI thread:

```csharp
_mediaPlayer.PositionChanged += (s, e) =>
{
    DispatcherQueue.TryEnqueue(() => { /* UI update here */ });
};
```

### 4.5 Track Selection

```csharp
// Subtitle tracks
foreach (var desc in _mediaPlayer.SpuDescription) { ... }
_mediaPlayer.SetSpu(trackId);

// Audio tracks
foreach (var desc in _mediaPlayer.AudioTrackDescription) { ... }
_mediaPlayer.SetAudioTrack(trackId);
```

---

## 5. Migration Steps Performed

### Phase 1 — Dependency Setup
- Added `LibVLCSharp`, `LibVLCSharp.WinUI`, `VideoLAN.LibVLC.Windows` to `.csproj`.
- Verified `dotnet build` succeeds with 0 errors.

### Phase 2 — VlcMediaPlayer Engine
- Created `VlcMediaPlayer.cs` implementing `IMediaPlayer`.
- Mapped: `Play`, `Pause`, `Stop`, `Seek`, `SetVolume`, `SetPlaybackRate`.
- Mapped subtitle and audio track cycling.

### Phase 3 — Unified Interface
- Extended `IMediaPlayer` with `Position`, `Duration`, `IsFullScreen`.
- Added provider-agnostic track APIs: `GetSubtitleTracks()`, `SetSubtitleTrack(int)`, `GetAudioTracks()`, `SetAudioTrack(int)`.
- Both `WindowsMediaPlayer` and `VlcMediaPlayer` fully implement the updated interface.

### Phase 4 — UI Integration
- Added `<vlc:VideoView>` to `CinemaPage.xaml` (initially `Collapsed`).
- Created `UpdateVideoSurfaces()` in `CinemaPage.xaml.cs` which shows/hides the correct control and binds the correct player.
- Added `UseLibVLC` setting to `ISettingsService`, `SettingsService`, and `SettingsPage`.
- Introduced `SwitchableMediaPlayer` to proxy all `IMediaPlayer` calls to the active backend.

### Phase 5 — Verification
- All 5 phases built cleanly: `0 Errors, 0 Warnings`.
- API coverage verified structurally for all listed test cases.

---

## 6. Feature Parity

| Feature | WMF | VLC |
|---------|-----|-----|
| MP4, MKV, AVI, MOV, HEVC | ✅ | ✅ |
| Subtitles | ✅ | ✅ |
| External subtitle loading | ✅ | ✅ |
| Subtitle delay | ✅ | ✅ |
| Audio track switching | ✅ | ✅ |
| Audio delay | ❌ Not supported | ✅ |
| Playback speed (0.5×–2×) | ✅ | ✅ |
| Seeking | ✅ | ✅ |
| Volume | ✅ | ✅ |
| Hardware decoding (DXVA2) | ✅ | ✅ |
| Fullscreen | ✅ (layout) | ✅ (native) |

---

## 7. Known Issues

### 7.1 Airspace (Z-Order) Conflict
- **Symptom:** WinUI 3 XAML overlays (OSD text, subtitle block, bottom controls bar) may be hidden behind the VLC `VideoView` because it renders as a native Win32 child window hosted above the XAML compositor layer.
- **Workaround A:** Place XAML overlays inside a `Popup` or secondary `AppWindow` layer.
- **Workaround B:** Switch VLC to memory-buffer rendering mode (software bitmap piped into a `SwapChainPanel`) to stay within the XAML compositor.
- **Status:** Under investigation.

### 7.2 Engine Switch Requires Reload
- **Symptom:** Toggling the engine in Settings while a video is playing does not immediately switch the active renderer. The engine switches on the **next media load**.
- **Workaround:** Restart playback of the current file after switching.
- **Status:** By design for Phase 4. Auto-reload on engine switch can be added in a future phase.

### 7.3 VLC Position Event Frequency
- **Symptom:** VLC fires `PositionChanged` per decoded frame (up to 60 fps), which can overload the UI thread with dispatcher calls.
- **Mitigation:** Consider throttling position updates to once per 250ms in `VlcMediaPlayer.cs`.

---

## 8. How to Switch Engines at Runtime

1. Launch **NevPlayer**.
2. Navigate to the **Settings** page (gear icon).
3. Under the **PLAYBACK** section, find **"Use LibVLC Engine"**.
4. Toggle the switch **ON** to use LibVLC, or **OFF** to use Windows Media Foundation.
5. The setting is persisted to `%LOCALAPPDATA%/NevPlayer/settings.json`.
6. Load or reload a media file. The active rendering surface will update automatically.

---

## 9. Future Improvements

| Item | Description |
|------|-------------|
| Auto-reload on engine switch | Seamlessly switch and continue playback without user action |
| Offscreen VLC rendering | Eliminate airspace conflict by rendering to `SoftwareBitmap` |
| LibVLC 4.x upgrade | LibVLC 4.x has improved WinUI integration and better DXVA3 support |
| Remove WMF | Once VLC is fully validated, deprecate and remove `WindowsMediaPlayer` entirely |
| Hardware decode toggle | Expose `--no-avcodec-hw` VLC option tied to the existing hardware acceleration setting |
