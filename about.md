# NevPlayer

> **Play everything. Your way.**

NevPlayer is a modern, powerful, and offline-first media player designed for Windows, with Android support planned for the future.

The application combines broad media compatibility, smooth playback, advanced playback controls, and a clean, premium user interface into one unified media experience.

NevPlayer is designed to handle both **video and music**, with the goal of making media playback simple for everyday users while providing the advanced capabilities expected by power users.

---

## Vision

The vision behind NevPlayer is simple:

> **Open your media. Press Play. It just works.**

Users should not have to understand codecs, containers, decoders, rendering technologies, or hardware acceleration to enjoy their media.

Whether a user opens a movie, anime episode, music file, or another supported media format, NevPlayer should automatically determine the best available playback method and provide a smooth and reliable experience.

NevPlayer aims to deliver:

- Broad media compatibility
- Smooth and reliable playback
- Hardware acceleration where available
- Intelligent software fallback
- Excellent subtitle support
- Multiple audio and subtitle tracks
- Fast startup
- Efficient resource usage
- A modern and intuitive interface
- Powerful features without unnecessary complexity
- A fully functional offline experience

---

# Product Philosophy

## Compatibility First

NevPlayer is being designed to support a wide range of media formats and codecs.

The application should aim to handle the types of files users commonly encounter, including media downloaded from the internet, transferred from other devices, or stored locally.

The goal is not to make users worry about what format their file uses.

The goal is:

> **If the file is valid and the system is capable of decoding it, NevPlayer should make every reasonable effort to play it.**

---

## Intelligent Playback

NevPlayer should intelligently select the best available playback path.

The application should prioritize hardware acceleration when it is available and appropriate.

If hardware decoding is unavailable, incompatible, or fails, the player should use an appropriate software decoding path where possible.

Conceptually:

```text
Open Media
    │
    ▼
Analyze Media
    │
    ├── Detect Container
    ├── Detect Video Codec
    ├── Detect Audio Codec
    ├── Detect Subtitle Tracks
    └── Detect Available Hardware
    │
    ▼
Select Best Playback Path
    │
    ├── Hardware Decoding
    │
    └── Software Decoding Fallback
    │
    ▼
Process Audio / Video / Subtitles
    │
    ▼
Render
    │
    ▼
Playback
```
