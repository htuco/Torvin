import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';

/// Naslov sekcije ("Moje kartice") s linkom "Sve ›" kad sekcija ima svoj ekran.
class SectionHeader extends StatelessWidget {
  const SectionHeader({super.key, required this.title, this.onSeeAll});

  final String title;
  final VoidCallback? onSeeAll;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final onSeeAll = this.onSeeAll;

    return Row(
      children: [
        Expanded(child: Text(title, style: theme.textTheme.titleLarge)),
        if (onSeeAll != null)
          InkWell(
            onTap: onSeeAll,
            borderRadius: BorderRadius.circular(AppConstants.radiusGrid),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('Sve', style: theme.textTheme.labelLarge?.copyWith(color: theme.colorScheme.primary)),
                Icon(LucideIcons.chevronRight, size: AppConstants.iconSize, color: theme.colorScheme.primary),
              ],
            ),
          ),
      ],
    );
  }
}
