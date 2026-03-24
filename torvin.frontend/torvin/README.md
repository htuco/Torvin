# Torvin Frontend

Bootstrap stage is complete.

Next step is implementing feature-based architecture.

## Feature-Based Structure

### App Shell
- `lib/app`: Global app-level setup.
- `lib/app/router`: Route definitions and navigation flow.
- `lib/app/di`: Dependency injection registration and wiring.

### Core
- `lib/core`: Shared foundation not tied to one feature.
- `lib/core/constants`: App constants.
- `lib/core/errors`: Error/failure models and mapping.
- `lib/core/network`: API client and network configuration.
- `lib/core/theme`: Global theming.
- `lib/core/utils`: Generic helpers and utilities.

### Shared
- `lib/shared`: Reusable cross-feature code.
- `lib/shared/extensions`: Dart extensions.
- `lib/shared/widgets`: Reusable UI components.
- `lib/shared/models`: Cross-feature models.
- `lib/shared/services`: Shared services.

### Features
- `lib/features`: Root for feature modules.
- `lib/features/_template`: Template for creating a new feature.

Each feature follows this internal layering:
- `data`: Data sources, DTO/models, repository implementations.
- `domain`: Entities, repository contracts, use cases.
- `presentation`: UI pages/widgets and state controllers.

Template subfolders:
- `lib/features/_template/data/datasources`
- `lib/features/_template/data/models`
- `lib/features/_template/data/repositories`
- `lib/features/_template/domain/entities`
- `lib/features/_template/domain/repositories`
- `lib/features/_template/domain/usecases`
- `lib/features/_template/presentation/pages`
- `lib/features/_template/presentation/widgets`
- `lib/features/_template/presentation/controllers`

## Run

```bash
flutter pub get
flutter run
```
