import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import 'app/router/app_router.dart';
import 'app/router/app_routes.dart';
import 'core/theme/app_theme.dart';

const _designSize = Size(390, 844);

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
      builder: (_, child) {
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          title: 'Torvin',
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: ThemeMode.system,
          initialRoute: AppRoutes.root,
          onGenerateRoute: AppRouter.onGenerateRoute,
        );
      },
    );
  }
}
