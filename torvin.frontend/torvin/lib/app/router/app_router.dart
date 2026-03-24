import 'package:flutter/material.dart';
import 'app_routes.dart';

/// Simple router for placeholder pages.
abstract final class AppRouter {
  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    switch (settings.name) {
      case AppRoutes.root:
      case AppRoutes.dashboard:
        return MaterialPageRoute<void>(
          settings: const RouteSettings(name: AppRoutes.dashboard),
          builder: (_) => const _DashboardPlaceholder(),
        );
      case AppRoutes.login:
        return MaterialPageRoute<void>(
          settings: const RouteSettings(name: AppRoutes.login),
          builder: (_) => const _LoginPlaceholder(),
        );
      case AppRoutes.register:
        return MaterialPageRoute<void>(
          settings: const RouteSettings(name: AppRoutes.register),
          builder: (_) => const _RegisterPlaceholder(),
        );
      default:
        return MaterialPageRoute<void>(
          settings: const RouteSettings(name: AppRoutes.dashboard),
          builder: (_) => const _DashboardPlaceholder(),
        );
    }
  }
}

class _DashboardPlaceholder extends StatelessWidget {
  const _DashboardPlaceholder();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: Text('dashboard', style: theme.textTheme.titleLarge),
      ),
    );
  }
}

class _LoginPlaceholder extends StatelessWidget {
  const _LoginPlaceholder();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: Text('login', style: theme.textTheme.titleLarge),
      ),
    );
  }
}

class _RegisterPlaceholder extends StatelessWidget {
  const _RegisterPlaceholder();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: Text('register', style: theme.textTheme.titleLarge),
      ),
    );
  }
}

class _UnknownPlaceholder extends StatelessWidget {
  const _UnknownPlaceholder();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: Text('Unknown route', style: theme.textTheme.titleLarge),
      ),
    );
  }
}
