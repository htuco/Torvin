import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

import 'app/router/app_router.dart';

void main() {
  AppConfig.validate();
  runApp(const TorvinAdminApp());
}

class TorvinAdminApp extends StatelessWidget {
  const TorvinAdminApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Torvin Admin',
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.system,
      routerConfig: appRouter,
    );
  }
}
