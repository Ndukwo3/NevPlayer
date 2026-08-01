# NevPlayer Media Engine Selection & Evaluation

This document outlines the media playback engine options, subtitle handling, hardware acceleration paths, and licensing implications for NevPlayer.

---

## 1. Media Engine Comparison

| Feature | libmpv (Recommended) | FFmpeg (Custom Wrapper) | Windows Media Foundation (WMF) |
| :--- | :--- | :--- | :--- |
| **Codec Compatibility** | Outstanding (plays almost everything out of the box) | Outstanding (requires manual demuxing & rendering) | Moderate (limited container support like MKV/WebM defaults) |
| **Subtitle Quality** | Best (native libass for SSA/ASS styled subtitles) | High (requires manual rendering/libass integration) | Poor (basic SRT support, styled/anime subtitles fail) |
| **Implementation Effort** | Low-Medium (leverages mature player API) | Extremely High (requires writing a custom player engine) | Low (native API, built-in WinUI MediaPlayerElement) |
| **Hardware Acceleration** | Built-in (D3D11VA, DXVA2, NVDEC) | Low-level (must be configured manually) | Built-in (DXVA) |
| **Binary Size Overhead** | Medium (~20-30MB for mpv library dll) | High (~30-50MB for FFmpeg shared dlls) | Zero (uses system-native libraries) |

### Verdict
We recommend **libmpv** as the core media engine for NevPlayer. It achieves the core product philosophy ("Open the file. Press Play. It just works.") with the highest degree of reliability, particularly for media containers like MKV and styled anime subtitle rendering (SSA/ASS) which are notorious for failing on native Windows Media Foundation.

---

## 2. Subtitle Capabilities

Subtitles are a core feature of the NevPlayer experience, especially for anime-friendly playback:
- **SRT (SubRip Text)**: Standard plain text subtitles. Supported by all engines.
- **ASS / SSA (Advanced SubStation Alpha)**: Dynamic styled subtitles containing custom positioning, fonts, colors, and overlapping animations.
  - **Handling**: libmpv utilizes `libass` natively to render these with exact pixel styling, preserving formatting without CPU overhead. WMF lacks native support for ASS/SSA fonts and styling, rendering them either poorly or not at all.
- **Embedded & External**: The media engine wrapper must allow switching between embedded tracks (common in MKV) and loading external `.srt`/`.ass` files.

---

## 3. Hardware Acceleration & Software Fallback

Efficient rendering is critical for high-resolution (4K) and high-depth (10-bit) files:

```text
               [ Playback Initiated ]
                         │
                         ▼
        [ Attempt Hardware Acceleration ]
       (D3D11VA / DXVA2 / NVDEC / Intel QSV)
                         │
            ┌────────────┴────────────┐
            ▼ Success                 ▼ Fails / Unsupported
   [ Smooth GPU Decoding ]    [ Software Decoders (FFmpeg) ]
   (Minimal CPU/Battery load) (Decodes via CPU cores)
```

- **Hardware Acceleration**: libmpv auto-detects and uses Windows graphics hardware (Direct3D 11 Video Acceleration) to decode demanding formats like H.265 (HEVC) and AV1.
- **Intelligent Fallback**: If the user's graphics processor does not support a specific codec profile (e.g. older hardware playing AV1 or 10-bit HEVC), the player seamlessly falls back to CPU decoding via software decoders (bundled in the engine) without crashing or interrupting playback.

---

## 4. Licensing Implications

### FFmpeg Licensing
- FFmpeg is licensed under the LGPL v2.1.
- If compiled with GPL-only external libraries (like `libx264`), the license upgrades to GPL.
- **NevPlayer's Stance**: By using FFmpeg dynamically linked (no modifications to FFmpeg's source code), we comply with LGPL requirements and can keep our application code closed/proprietary if desired.

### libmpv Licensing
- libmpv is similarly licensed under LGPL v2.1 (though some build profiles include GPL components).
- **NevPlayer's Stance**: We will utilize precompiled LGPL-licensed dynamic link libraries (`mpv-2.dll` / `mpv-1.dll`) and dynamically bind to them in C#. This allows us to distribute the app without copyright licensing issues.
