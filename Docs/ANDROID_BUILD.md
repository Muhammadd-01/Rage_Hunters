# RAGE HUNTERS — Android Build Guide

## Prerequisites

### Required Unity Modules
Install via **Unity Hub → Installs → 6000.6.5f1 → Add Modules**:
- ✅ Android Build Support
- ✅ Android SDK & NDK Tools
- ✅ OpenJDK

### Verify Installation
After installing modules, check in Unity Editor:
- **Edit → Preferences → External Tools** — Android SDK, NDK, JDK paths should be auto-populated
- **File → Build Settings** — Android platform should be available

## Project Configuration

| Setting | Value |
|---------|-------|
| Package Name | `com.ragehunters.game` |
| Minimum API Level | Android 8.0 (API 26) |
| Target API Level | Automatic (highest installed) |
| Scripting Backend | IL2CPP (recommended for release) |
| Target Architectures | ARMv7 + ARM64 |
| Internet Access | Required |
| Screen Orientation | Landscape Left |
| Render Pipeline | URP (Mobile preset) |

## Building a Development APK

### From Unity Editor
1. **File → Build Settings**
2. Switch platform to **Android** (if not already)
3. Check **Development Build** and **Script Debugging**
4. Click **Build**
5. Save as `Build/Android/RageHunters-dev.apk`

### From Command Line
```bash
# macOS — adjust Unity path for your installation
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod BuildScripts.BuildAndroid.BuildDevelopmentAPK \
  -logFile Build/build.log
```

## Installing on Device

### Via USB (ADB)
```bash
# Ensure USB debugging is enabled on the device
# Connect device via USB cable

# Check device is connected
adb devices

# Install APK
adb install -r Build/Android/RageHunters-dev.apk

# Launch the app
adb shell am start -n com.ragehunters.game/com.unity3d.player.UnityPlayerActivity
```

### Via File Transfer
1. Copy APK to device storage
2. Open file manager on device
3. Tap APK to install
4. May need to enable "Install from unknown sources"

## Building a Release AAB

1. Create a keystore: **Edit → Project Settings → Player → Android → Publishing Settings**
2. Configure signing with your keystore
3. **File → Build Settings → Build App Bundle (AAB)**
4. Upload to Google Play Console

## Build Script

See `BuildScripts/BuildAndroid.cs` for automated build methods.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Android module not installed" | Install via Unity Hub → Add Modules |
| Gradle build fails | Check JDK version, update Gradle template |
| APK won't install | Check minimum SDK vs device Android version |
| Black screen on launch | Check URP asset is assigned in Graphics settings |
| Touch not working | Verify Input System is set to "Both" in Player Settings |

## Tested Devices

| Device | Android Version | Result | FPS | Notes |
|--------|----------------|--------|-----|-------|
| *(not yet tested)* | | | | |

> **Note**: Android Build Support module must be installed before any Android builds can be generated. This is currently a blocker.
