# NevPlayer Architecture & Environment Reference

This document outlines the environmental requirements, development workflows, packaging, and cross-platform considerations for NevPlayer.

---

## 1. WinUI 3 & .NET Requirements

### Target OS
- **Minimum**: Windows 10, version 1809 (Build 17763) or newer.
- **Recommended**: Windows 10/11 with the latest feature updates.

### Runtime and SDKs
- **.NET SDK**: .NET 8.0 LTS (or .NET 9.0). 
  *Note: Our environmental check shows no active .NET SDK is currently installed or configured in the system PATH. This is a prerequisite before building starts.*
- **Windows App SDK**: Version 1.5 or 1.6. Provides the WinUI 3 controls, styling, and modern Windows integration.

---

## 2. VS Code Development Workflow

To build and maintain NevPlayer from VS Code, the following workflow and tools are recommended:

### Recommended Extensions
1. **C# Dev Kit**: Official Microsoft extension offering solution management, build/run targets, and test explorer integration.
2. **C#**: Roslyn-powered language services.
3. **XAML Styler / XML Tools**: Format and maintain clean WinUI 3 XAML code.

### Command-Line Compilation
The project can be restored, built, and run entirely using the `dotnet` CLI:
- **Restore Dependencies**: `dotnet restore`
- **Build Solution**: `dotnet build`
- **Run App (Unpackaged)**: `dotnet run --project src/NevPlayer.App`
- **Run Tests**: `dotnet test`

---

## 3. Windows Packaging Options

NevPlayer will support two packaging and distribution models:

### A. Unpackaged (Portable Win32)
- **Concept**: The application runs as a standard Win32 executable.
- **Pros**: Zero installation required (xcopy deployable, ZIP extract-and-run), simple testing, no developer certificate signing required for local development.
- **Cons**: Requires manual registration for file associations or deep shell integration.

### B. Packaged (MSIX)
- **Concept**: The application is wrapped in a modern Windows App Package.
- **Pros**: Clean install/uninstall, automatic updates, deep integration with Windows Shell (protocol association, file handler registration), sandboxed execution.
- **Cons**: Requires signing certificate (development/self-signed or commercial) to install.

*Recommendation*: Support **Unpackaged** for portable use-cases and local development speed, and compile to **MSIX** for formal releases.

---

## 4. Future Android Architecture Considerations

The development plan specifies Kotlin and Android Media3/ExoPlayer for the future Android version. Because of the technology difference (.NET/C# on Windows vs. Kotlin on Android), we should adopt the following code-splitting philosophy:

```text
NevPlayer Project
 ├── Windows Codebase (C# / .NET)
 │    ├── NevPlayer.App (WinUI 3 / XAML Views)
 │    ├── NevPlayer.Core (Shared ViewModels, Playback logic, DB Interfaces)
 │    └── NevPlayer.Media (libmpv C# Wrapper & playback controller)
 │
 └── Android Codebase (Kotlin / Gradle)
      ├── UI Layer (Jetpack Compose)
      ├── Core Logic (Kotlin ViewModels & local DB)
      └── Media Engine (Android Media3 / ExoPlayer integration)
```

### Shared Logic & Concept Portability
- **Concept Sharing**: ViewModels and Service layers in `NevPlayer.Core` (C#) should be designed with minimal Windows-specific APIs so they can act as a direct blueprint for the Kotlin rewrite.
- **Data Schemas**: Database schemas, playlist JSON formats, and configuration settings should be identical across platforms to support future backup and sync.
