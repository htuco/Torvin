import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';
import '../theme/torvin_colors.dart';

/// Podaci se nisu učitali: poruka (najčešće ona s backenda) i dugme za ponovni pokušaj.
class ErrorState extends StatelessWidget {
  const ErrorState({super.key, this.title = 'Podaci se nisu učitali', required this.message, required this.onRetry});

  final String title;
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final primary = theme.colorScheme.primary;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppConstants.spacingXL),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: AppConstants.errorIconBox,
              height: AppConstants.errorIconBox,
              decoration: BoxDecoration(
                color: colors.errorTint,
                borderRadius: BorderRadius.circular(AppConstants.radiusCard),
              ),
              child: Icon(LucideIcons.circleAlert, size: AppConstants.errorIconSize, color: colors.errorText),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
            const SizedBox(height: AppConstants.spacingSM),
            Text(
              message,
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppConstants.spacingSM),
            OutlinedButton.icon(
              onPressed: onRetry,
              icon: const Icon(LucideIcons.rotateCcw, size: AppConstants.iconSize),
              label: const Text('Pokušaj ponovo'),
              // DS: dugme greške je obrubljeno primarnom, ne neutralnom ivicom
              style: OutlinedButton.styleFrom(
                foregroundColor: primary,
                side: BorderSide(color: primary),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
