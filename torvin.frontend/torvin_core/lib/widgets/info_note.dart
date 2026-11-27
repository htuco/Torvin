import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';

/// Objašnjenje na sivoj podlozi s ikonom: savjet u dijalogu ili razlog zašto akcija nije dostupna.
class InfoNote extends StatelessWidget {
  const InfoNote(this.message, {super.key});

  final String message;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(AppConstants.spacingRow),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHigh,
        borderRadius: BorderRadius.circular(AppConstants.radiusControl),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(LucideIcons.info, size: AppConstants.iconSize, color: theme.colorScheme.primary),
          const SizedBox(width: AppConstants.spacingSM),
          Expanded(child: Text(message, style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}
