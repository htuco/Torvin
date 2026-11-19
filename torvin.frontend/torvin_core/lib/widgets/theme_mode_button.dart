import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/theme_mode_controller.dart';

/// Prekidač svijetle i tamne teme: u svijetloj pokazuje mjesec, u tamnoj sunce.
class ThemeModeButton extends ConsumerWidget {
  const ThemeModeButton({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final brightness = Theme.of(context).brightness;
    final isDark = brightness == Brightness.dark;

    return IconButton(
      tooltip: isDark ? 'Svijetla tema' : 'Tamna tema',
      icon: Icon(isDark ? LucideIcons.sun : LucideIcons.moon),
      onPressed: () => ref.read(themeModeControllerProvider.notifier).toggle(brightness),
    );
  }
}
