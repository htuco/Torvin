import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

import 'app/router/app_router.dart';

void main() {
  AppConfig.validate();
  runApp(const ProviderScope(child: TorvinAdminApp()));
}

class TorvinAdminApp extends ConsumerWidget {
  const TorvinAdminApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Torvin Admin',
      theme: AppTheme.desktopLightTheme,
      darkTheme: AppTheme.desktopDarkTheme,
      themeMode: ref.watch(themeModeControllerProvider).value ?? ThemeMode.system,
      routerConfig: ref.watch(routerProvider),
    );
  }
}
