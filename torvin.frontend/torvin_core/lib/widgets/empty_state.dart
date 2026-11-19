import 'package:flutter/material.dart';

import '../theme/app_constants.dart';

/// Lista ili ekran bez podataka: ikona, naslov, objašnjenje i (ako ima smisla) akcija koja to mijenja.
class EmptyState extends StatelessWidget {
  const EmptyState({
    super.key,
    required this.icon,
    required this.title,
    required this.message,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final actionLabel = this.actionLabel;
    final onAction = this.onAction;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppConstants.spacingXL),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: AppConstants.emptyIconBox,
              height: AppConstants.emptyIconBox,
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                borderRadius: BorderRadius.circular(AppConstants.radiusCard),
              ),
              child: Icon(icon, size: AppConstants.emptyIconSize, color: theme.colorScheme.primary),
            ),
            const SizedBox(height: AppConstants.spacingRow),
            Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
            const SizedBox(height: AppConstants.spacingRow),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: AppConstants.emptyMessageWidth),
              child: Text(
                message,
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                textAlign: TextAlign.center,
              ),
            ),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: AppConstants.spacingMD),
              FilledButton(onPressed: onAction, child: Text(actionLabel)),
            ],
          ],
        ),
      ),
    );
  }
}
