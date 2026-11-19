import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../features/auth/presentation/login_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../shell/admin_shell.dart';
import 'app_routes.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    initialLocation: AppRoutes.root,
    redirect: (_, state) => authRedirect(
      auth: ref.read(authControllerProvider),
      location: state.matchedLocation,
      publicRoutes: AppRoutes.publicRoutes,
      splash: AppRoutes.root,
      login: AppRoutes.login,
      home: AppRoutes.dashboard,
    ),
    routes: [
      GoRoute(path: AppRoutes.root, builder: (_, _) => const SplashPage()),
      GoRoute(path: AppRoutes.login, builder: (_, _) => const LoginScreen()),
      ShellRoute(
        builder: (_, state, child) => AdminShell(location: state.matchedLocation, child: child),
        routes: [
          GoRoute(path: AppRoutes.dashboard, builder: (_, _) => const DashboardScreen()),
        ],
      ),
    ],
    errorBuilder: (context, _) => NotFoundPage(onBack: () => context.go(AppRoutes.root)),
  );

  // Prijava, odjava i istekla sesija mijenjaju auth stanje, pa router ponovo provjeri redirect
  ref.listen(authControllerProvider, (_, _) => router.refresh());
  ref.onDispose(router.dispose);
  return router;
});
