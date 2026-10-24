import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import 'app_routes.dart';

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.dashboard,
  routes: [
    GoRoute(
      path: AppRoutes.root,
      redirect: (_, _) => AppRoutes.dashboard,
    ),
    GoRoute(
      path: AppRoutes.dashboard,
      builder: (_, _) => const _PlaceholderPage(title: 'dashboard'),
    ),
    GoRoute(
      path: AppRoutes.login,
      builder: (_, _) => const _PlaceholderPage(title: 'login'),
    ),
    GoRoute(
      path: AppRoutes.register,
      builder: (_, _) => const _PlaceholderPage(title: 'register'),
    ),
  ],
  errorBuilder: (_, state) =>
      _PlaceholderPage(title: 'Unknown route: ${state.uri}'),
);

class _PlaceholderPage extends StatelessWidget {
  const _PlaceholderPage({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Text(
          title,
          style: Theme.of(context).textTheme.titleLarge,
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}
