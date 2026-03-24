import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import 'core/theme/app_theme.dart';

const _designSize = Size(390, 844);
const _pagePadding = EdgeInsets.all(20);

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const TorvinApp());
}

class TorvinApp extends StatelessWidget {
  const TorvinApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ScreenUtilInit(
      designSize: _designSize,
      minTextAdapt: true,
      splitScreenMode: true,
      child: const _ThemePreviewPage(),
      builder: (_, child) {
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          title: 'Torvin',
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: ThemeMode.system,
          home: child,
        );
      },
    );
  }
}

class _ThemePreviewPage extends StatelessWidget {
  const _ThemePreviewPage();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Center(
        child: Padding(
          padding: _pagePadding,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.space_dashboard_rounded,
                size: 56,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(height: 16),
              Text('Torvin', style: theme.textTheme.headlineSmall),
              const SizedBox(height: 8),
              Text(
                'Home screen placeholder',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
