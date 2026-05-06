# AndroidControlKit .NET

<div align="center">

### Professional Android SDK + Scrcpy Automation Toolkit for .NET Developers

A unified open-source library for Android SDK discovery, ADB/Fastboot execution, Scrcpy mirroring, emulator tooling, and desktop-grade Android device automation.

![GitHub stars](https://img.shields.io/github/stars/yourname/AndroidControlKit?style=for-the-badge)
![GitHub forks](https://img.shields.io/github/forks/yourname/AndroidControlKit?style=for-the-badge)
![License](https://img.shields.io/github/license/yourname/AndroidControlKit?style=for-the-badge)
![NuGet](https://img.shields.io/nuget/v/AndroidControlKit?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-6%2B-blue?style=for-the-badge)

</div>

---

## Why AndroidControlKit?

Building Android desktop utilities in .NET often means manually handling:

* Android SDK path detection
* `adb` process wrappers
* `fastboot` command execution
* emulator binary resolution
* Scrcpy process launch scripts
* device mirroring arguments
* environment validation logic
* repeated shell boilerplate

That usually leads to fragmented codebases and fragile utility scripts.

**AndroidControlKit .NET** solves this by providing a single, clean, developer-friendly API layer that combines:

✔ Android SDK Management
✔ ADB/Fastboot Integration
✔ Scrcpy Device Mirroring
✔ Emulator Tool Resolution
✔ Android Device Automation

inside one reusable .NET toolkit.

---

## Core Features

### Android SDK Discovery Engine

Automatically locate and validate Android SDK installations from:

* `ANDROID_HOME`
* `ANDROID_SDK_ROOT`
* Android Studio standard directories
* Windows Local AppData paths
* custom developer-defined locations

### ADB + Fastboot Tooling

Programmatic access to:

* adb executable
* fastboot executable
* shell command helpers
* device listing
* package install/uninstall
* reboot/recovery helpers
* command output capture

### Scrcpy Mirroring Engine

Integrated desktop Android screen control with:

* automatic scrcpy binary discovery
* process launch wrappers
* serial-targeted execution
* USB or TCP/IP sessions
* custom resolution / bitrate / fullscreen flags
* hidden/background runtime support
* multi-device launch compatibility

### Emulator Resolver

Quick access to installed:

* emulator binary
* AVD directories
* SDK tools
* runtime launch parameters

### Automation Friendly Architecture

Designed for:

* WinForms applications
* WPF dashboards
* background Windows services
* enterprise device management tools
* QA device farms
* internal support utilities

---

## Installation

```bash
dotnet add package AndroidControlKit
```

or

```powershell
Install-Package AndroidControlKit
```

---

## Quick Start Example

```csharp
using AndroidControlKit;

var android = new AndroidManager();

Console.WriteLine($"SDK Root: {android.GetSdkRoot()}");
Console.WriteLine($"ADB Path: {android.GetAdbPath()}");
Console.WriteLine($"Fastboot Path: {android.GetFastbootPath()}");
Console.WriteLine($"Scrcpy Path: {android.GetScrcpyPath()}");

var devices = android.GetConnectedDevices();

foreach (var device in devices)
{
    Console.WriteLine(device.Serial);
}

android.StartScrcpy("DEVICE_SERIAL");
```

---

## Real World Use Cases

AndroidControlKit was built for developers creating:

* Android Device Inventory Systems
* Internal MDM/Desktop Control Utilities
* QA Automated Device Labs
* Device Mirroring Dashboards
* Android Deployment/Flashing Tools
* Remote Support Applications
* Enterprise Mobile Monitoring Solutions
* Multi-device Scrcpy Launchers

---

## Developer API Highlights

### SDK Detection

```csharp
string sdkRoot = android.GetSdkRoot();
bool valid = android.ValidateSdk();
```

### ADB Command Execution

```csharp
var result = android.Adb("devices");
Console.WriteLine(result.Output);
```

### Device Enumeration

```csharp
var devices = android.GetConnectedDevices();
```

### Launch Scrcpy

```csharp
android.StartScrcpy("DEVICE_SERIAL", new ScrcpyOptions
{
    MaxSize = 1024,
    BitRate = "8M",
    Fullscreen = false,
    TcpIp = false
});
```

---

## Designed For Scalability

Unlike basic adb wrappers, AndroidControlKit is structured as a modular Android desktop integration framework.

This means future support can extend into:

* APK deployment managers
* screen recording automation
* file transfer wrappers
* wireless debugging helpers
* device performance monitoring
* batch command orchestration
* Android farm administration

without changing the project foundation.

---

## Platform Support

Currently Supported:

* Windows 10
* Windows 11
* .NET Framework
* .NET 6+
* .NET 7+
* .NET 8+

Roadmap:

* Linux runtime support
* macOS runtime support
* bundled Scrcpy dependency manager
* NuGet plugin modules

---

## Contributing

Contributions are welcome.

Whether it's:

* new Android SDK resolvers
* better Scrcpy wrappers
* additional ADB helpers
* bug fixes
* performance improvements

feel free to submit pull requests or open feature discussions.

---

## License

MIT License

---

## GitHub Topics

`android-sdk` `scrcpy` `adb` `fastboot` `dotnet` `csharp` `android-automation` `android-device-control` `android-toolkit` `device-mirroring`

---

<div align="center">
Built for developers who need professional Android desktop tooling in .NET.
</div>
