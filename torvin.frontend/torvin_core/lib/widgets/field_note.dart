import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';

/// Red ispod polja forme: greška s ikonom, a bez greške objašnjenje formata (ako postoji).
class FieldNote extends StatelessWidget {
  const FieldNote({super.key, this.error, this.helper});

  final String? error;
  final String? helper;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final style = theme.textTheme.labelMedium;
    final error = this.error;
    final helper = this.helper;

    if (error == null && helper == null) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.only(top: AppConstants.spacingSM),
      child: error != null
          ? Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(LucideIcons.circleAlert, size: AppConstants.fieldNoteIconSize, color: scheme.error),
                const SizedBox(width: AppConstants.spacingXS),
                Expanded(child: Text(error, style: style?.copyWith(color: scheme.error))),
              ],
            )
          : Text(helper!, style: style?.copyWith(color: scheme.onSurfaceVariant)),
    );
  }

  /// Labela polja: 13/16, srednja debljina, sekundarna boja (DS v2).
  static TextStyle? labelStyle(ThemeData theme) => theme.textTheme.bodySmall?.copyWith(
        height: 16 / 13,
        fontWeight: FontWeight.w500,
        color: theme.colorScheme.onSurfaceVariant,
      );
}
