# NevPlayer Development Plan

> A step-by-step development roadmap for building NevPlayer.

---

# 1. Project Overview

NevPlayer is a modern, offline-first music and video player designed initially for Windows, with Android support planned for a future phase.

The application is intended to combine:

- Broad media compatibility
- Reliable video playback
- Reliable audio playback
- Hardware acceleration
- Software decoding fallback
- Advanced subtitle support
- Multiple audio tracks
- Modern Cinema Mode
- Music library management
- Video library management
- Playlists
- Playback history
- Favorites
- Advanced playback controls

The development process will prioritize **playback reliability and media compatibility before advanced features and visual polish**.

The core product philosophy is:

> **Open the file. Press Play. Enjoy the media.**

---

# 2. Development Philosophy

NevPlayer will be developed incrementally.

The project will not attempt to build every feature at once.

Development will follow this general progression:

```text
Research
    ↓
Technical Validation
    ↓
Project Foundation
    ↓
Media Engine Prototype
    ↓
Core Playback
    ↓
Cinema Mode
    ↓
Music Player
    ↓
Video Library
    ↓
Advanced Features
    ↓
Performance Optimization
    ↓
Testing
    ↓
Windows Release
    ↓
Android Development

Each phase should be completed and tested before moving to the next major phase.

3. Target Platforms
Phase 1 — Windows

Windows is the first target platform.

The initial goal is to build a powerful and polished Windows desktop media player.

The Windows version will be used to validate:

The media engine
Playback architecture
UI/UX
Media compatibility
Hardware acceleration
Software fallback
Subtitle handling
Music playback
Video playback
Library management
Phase 2 — Android

Android development will begin after the Windows version has reached a stable and mature state.

The Android version will maintain the NevPlayer product philosophy while using appropriate Android technologies and media APIs.

The Windows and Android versions should share common product concepts and business logic where practical, while allowing each platform to use its native media capabilities.

4. Programming Languages
Primary Windows Language

C#

C# will be used for:

Application logic
Playback control
Media library management
Playlist management
Data management
Settings
Services
Windows application functionality
UI Markup

XAML

XAML will be used with WinUI 3 to define and structure the Windows user interface.

It will be used for:

Layouts
Controls
Views
Styles
Templates
Animations
Visual states
Future Android Language

Kotlin

Kotlin will be considered the primary language for the future Android version.

The Android implementation will use Android-native media technologies where appropriate.

5. Application Frameworks
Windows UI Framework

WinUI 3

WinUI 3 will be used to build the Windows user interface.

It will provide the foundation for:

Main application window
Cinema Mode
Music interface
Video interface
Library
Playlist interface
Settings
Custom controls
Modern Windows styling

The UI should be fully custom-designed to match the NevPlayer product identity.

Windows Application Framework

.NET

.NET will provide the application runtime and development foundation for the Windows application.

It will support:

C# application logic
Dependency injection
Async operations
File system operations
Services
Data processing
Application architecture
Development Environment

Visual Studio Code

VS Code will be the primary development environment.

The project should maintain a development workflow that can be managed from VS Code and the command line where possible.

The required Windows SDK, .NET SDK, Windows App SDK, C++ build tools, or other build dependencies should be installed only when required by the selected project architecture.

6. Media Engine

The media engine is the most technically important part of NevPlayer.

The final media engine should not be selected purely based on popularity.

It must be evaluated based on real-world playback performance.

Potential technologies include:

libmpv
FFmpeg
Windows Media Foundation
Native Windows media APIs
Hybrid media architecture

The final choice will be documented in:

docs/media-engine.md
7. Media Engine Evaluation

Before building the complete player, the available media technologies should be evaluated.

The evaluation should consider:

Compatibility
MP4
MKV
AVI
MOV
WebM
MPEG/TS
H.264
H.265 / HEVC
VP8
VP9
AV1
AAC
MP3
FLAC
Opus
AC3
Subtitle Support
SRT
ASS
SSA
Embedded subtitles
External subtitles
Styled subtitles
Advanced Playback
Multiple audio tracks
Multiple subtitle tracks
Subtitle synchronization
Playback speed
Frame stepping
A-B looping
Performance
CPU usage
GPU usage
Memory usage
Startup time
Seeking performance
High-bitrate playback
Hardware Acceleration

Test hardware acceleration where available.

Evaluate:

GPU decoding
Hardware compatibility
Fallback behavior
Stability
Software Decoding

Verify that the application can fall back to software decoding when hardware acceleration is unavailable or unsuitable.

8. Phase 0 — Project Research

Before writing the main application, research and document:

WinUI 3 requirements
.NET version
Windows App SDK requirements
VS Code development workflow
Media engine options
Media engine licensing
Hardware acceleration options
Subtitle capabilities
FFmpeg licensing implications
libmpv integration requirements
Windows packaging options
Future Android architecture considerations

The results should be documented in:

docs/architecture.md
docs/media-engine.md
9. Phase 1 — Project Foundation

Create the initial project structure.

The application should be modular.

The architecture should separate:

UI
Application Core
Media Core
Data Layer
Platform Layer

The project should not be implemented as one monolithic application file.

The initial structure should follow this general organization:

NevPlayer/
│
├── NevPlayer.sln
│
├── src/
│   ├── NevPlayer.App/
│   ├── NevPlayer.Core/
│   ├── NevPlayer.Media/
│   ├── NevPlayer.Data/
│   └── NevPlayer.Platform/
│
├── tests/
│
├── docs/
│   ├── about.md
│   ├── development-plan.md
│   ├── architecture.md
│   ├── media-engine.md
│   └── ui-guidelines.md
│
├── README.md
├── PROJECT_SPEC.md
└── LICENSE

The exact structure may be adjusted based on technical requirements.

10. Phase 2 — Application Shell

Build the initial NevPlayer Windows application shell.

The shell should establish:

Main window
Application navigation
Theme
Global styles
Window behavior
Custom title bar where appropriate
Basic navigation structure

Initial navigation may include:

Home
Music
Videos
Playlists
Favorites
History
Settings

Cinema Mode should be accessible from video playback.

11. Phase 3 — Design System

Implement the NevPlayer visual design system.

The design direction should be:

Dark
Modern
Premium
Minimal
Clean
Immersive

The system should centralize:

Colors
Typography
Spacing
Corner radius
Shadows
Buttons
Icons
Animations
Transitions

The primary visual reference is the approved NevPlayer Cinema Mode Concept #1.

The application should not directly copy PotPlayer.

Instead, NevPlayer should create its own modern visual identity inspired by the best aspects of powerful desktop media players.

12. Phase 4 — Media Engine Prototype

Build the smallest possible functional media player.

The prototype should be able to:

Open a local media file
Play video
Play audio
Pause
Resume
Stop
Seek
Change volume
Enter fullscreen

At this stage, focus on validating the media engine.

Do not build the full library system yet.

13. Phase 5 — Playback System

Create the core playback architecture.

Implement:

Playback controller
Playback state
Queue
Current media
Previous media
Next media
Seek position
Duration
Volume
Playback speed

The playback system should be independent from the UI as much as practical.

14. Phase 6 — Cinema Mode

Implement the NevPlayer Cinema Mode interface.

Cinema Mode should include:

Video display
Playback controls
Timeline
Current time
Duration
Play/pause
Previous/next
Volume
Playback speed
Fullscreen
Subtitle controls
Audio track controls
Playlist access

Controls should be visually subtle.

When the user is watching content, the interface should prioritize the video.

Controls should automatically hide when inactive and appear when the user interacts with the player.

15. Phase 7 — Subtitle System

Implement subtitle functionality.

Support should include, where supported by the selected media engine:

Embedded subtitles
External subtitles
SRT
ASS
SSA

Implement:

Subtitle selection
Subtitle enable/disable
Subtitle synchronization
Subtitle delay
Multiple subtitle tracks

Advanced subtitle styling should be implemented where technically possible.

16. Phase 8 — Audio Track System

Implement support for multiple audio tracks.

Users should be able to:

View available audio tracks
Switch audio tracks
Select preferred audio tracks
Control volume
Adjust playback speed

Future versions may include:

Equalizer
Audio effects
Crossfade
Gapless playback
17. Phase 9 — Playlist System

Build playlist functionality.

Implement:

Add media to queue
Remove media
Reorder media
Play next
Play previous
Clear queue
Save playlists
Load playlists

The playlist system should work for both music and video.

18. Phase 10 — Music Player

Build the dedicated music experience.

Implement:

Music scanning
Artist organization
Album organization
Song listing
Album artwork
Genres
Folders
Favorites
Recently played
Playback history
Playlists
Queue
Shuffle
Repeat

The music interface should remain visually consistent with Cinema Mode and the overall NevPlayer design system.

19. Phase 11 — Video Library

Build the dedicated video experience.

Implement:

Video scanning
Folder browsing
Video thumbnails
Recently watched
Continue watching
Playback history
Favorites
Search

Future versions may include automatic organization of:

Movies
TV shows
Anime
Seasons
Episodes
20. Phase 12 — Media Metadata

Implement media metadata handling.

Potential metadata includes:

Title
Artist
Album
Genre
Duration
Resolution
Codec
Bitrate
File size
Audio tracks
Subtitle tracks
Album artwork
Video thumbnail

Metadata extraction should happen asynchronously without blocking the UI.

21. Phase 13 — Search

Implement global media search.

Search should be able to find:

Songs
Artists
Albums
Videos
Playlists
Folders

Search should be fast and responsive.

22. Phase 14 — Favorites and History

Implement:

Favorite songs
Favorite videos
Recently played
Recently watched
Continue watching
Playback history

The user should be able to manage and clear their history.

23. Phase 15 — Advanced Playback

Implement advanced features gradually.

Potential features:

Playback speed
Frame stepping
A-B looping
Aspect ratio controls
Video scaling
Subtitle delay
Audio delay
Advanced audio controls
Equalizer
Playback statistics

Only implement features that can be supported reliably by the chosen media architecture.

24. Phase 16 — Settings

Create a dedicated Settings system.

Settings may include:

Playback
Default playback speed
Resume playback
Auto-play next
Hardware acceleration
Decoder preferences
Subtitles
Default subtitle language
Subtitle size
Subtitle style
Subtitle delay
Audio
Default audio track
Equalizer
Volume normalization
Interface
Theme
Animation settings
UI preferences
Library
Media folders
Automatic scanning
Thumbnail settings
25. Phase 17 — Performance Optimization

Optimize:

Application startup
Media scanning
Library indexing
Thumbnail generation
Memory usage
CPU usage
GPU usage
Video playback
Seeking
UI responsiveness

The application should remain responsive during:

Media scanning
Metadata extraction
Thumbnail generation
Library indexing
Playback
26. Phase 18 — Compatibility Testing

Test NevPlayer using real-world media files.

Test:

Containers
MP4
MKV
AVI
MOV
WebM
Video
H.264
H.265 / HEVC
VP9
AV1
8-bit
10-bit
Audio
AAC
MP3
FLAC
Opus
AC3
Subtitles
SRT
ASS
SSA
Embedded subtitles
External subtitles
Playback Conditions
Hardware decoding enabled
Hardware decoding disabled
Software decoding
High bitrate video
High-resolution video
Multiple audio tracks
Multiple subtitle tracks

The player should be tested with real-world anime files and other difficult media files.

27. Phase 19 — Testing

Create automated tests for core logic.

Test:

Media models
Playlist logic
Queue logic
Library indexing
Media scanning
History
Favorites
Playback state
Settings

Perform manual playback testing for media compatibility.

28. Phase 20 — Windows Release

Prepare the Windows version for release.

Implement:

Application packaging
Installer
Application icon
File associations
Open-with integration
Drag-and-drop support
Uninstallation
Version management

The application should be tested on supported Windows versions before release.

29. Phase 21 — Android Planning

After the Windows version is stable, begin planning Android.

The Android version should evaluate:

Kotlin
Android native UI
Android Media3 / ExoPlayer
Hardware decoding
Android subtitle capabilities
Shared application logic

The Android version should maintain the NevPlayer identity while adapting the interface to mobile devices.

30. Final Technology Stack
Windows
Layer	Technology
Language	C#
Runtime	.NET
UI	WinUI 3
UI Markup	XAML
Platform	Windows
Development Environment	VS Code
Media Engine	To be selected after evaluation
Android — Future
Layer	Technology
Language	Kotlin
Platform	Android
Media Framework	To be evaluated
UI	Android-native approach to be determined

Potential Android media technology:

Android Media3
ExoPlayer
31. Final Development Order

The complete development sequence is:

01. Research & Technical Evaluation
        ↓
02. Media Engine Selection
        ↓
03. Project Foundation
        ↓
04. Windows Application Shell
        ↓
05. NevPlayer Design System
        ↓
06. Media Engine Prototype
        ↓
07. Core Playback System
        ↓
08. Cinema Mode
        ↓
09. Subtitle System
        ↓
10. Audio Track System
        ↓
11. Playlist System
        ↓
12. Music Player
        ↓
13. Video Library
        ↓
14. Media Metadata
        ↓
15. Search
        ↓
16. Favorites & History
        ↓
17. Advanced Playback
        ↓
18. Settings
        ↓
19. Performance Optimization
        ↓
20. Compatibility Testing
        ↓
21. Automated Testing
        ↓
22. Windows Release
        ↓
23. Android Planning
        ↓
24. Android Development
32. Definition of Completion

The first major Windows release of NevPlayer should provide:

Reliable local audio playback
Reliable local video playback
Broad media compatibility
MKV support
H.264 support
H.265 / HEVC support where supported
Subtitle support
Multiple audio tracks
Hardware acceleration where available
Software decoding fallback
Cinema Mode
Music library
Video library
Playlists
Search
Favorites
Playback history
Resume playback
Modern premium UI
Responsive performance
Offline-first functionality
```
