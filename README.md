# Torvin

Torvin is a modern Flutter-based digital wallet application for securely storing payment cards and tracking transactions in a clean, intuitive interface.

## Development Setup

### Tool Versions (Locked)
- Flutter: 3.41.4 (stable)
- Dart: 3.11.1
- Xcode: 26.3 (minimum for this setup)

### Project Structure
- Frontend (Flutter): torvin.frontend/torvin
- Backend: torvin.backend
- Documentation: docs

## Local Run (Frontend)

Run the app from the Flutter project folder:

```bash
cd torvin.frontend/torvin
flutter pub get
flutter devices
flutter run
```

For iOS simulator specifically:

```bash
open -a Simulator
cd torvin.frontend/torvin
flutter run
```

## iOS Prerequisites

1. Install and open Xcode at least once.
2. Accept license and install required components.
3. Set active developer directory:

```bash
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -runFirstLaunch
```

## iOS Simulator Quick Troubleshooting

- Simulator not listed in Flutter:

```bash
open -a Simulator
flutter devices
```

- Xcode tools not configured correctly:

```bash
xcode-select -p
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
```

- CocoaPods issue for iOS build:

```bash
cd torvin.frontend/torvin/ios
pod install
cd ..
cd ..
flutter clean
flutter pub get
flutter run
```
