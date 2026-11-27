import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Dno paginirane liste: indikator dok stiže sljedeća stranica, ili poruka i "Pokušaj" kad nije stigla.
/// Već učitani redovi ostaju iznad.
class PagedListFooter extends StatelessWidget {
  const PagedListFooter({super.key, required this.page, required this.onRetry});

  /// Koliko piksela prije kraja liste se traži sljedeća stranica.
  static const double loadMoreThreshold = 200;

  final PagedList<Object?> page;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final error = page.loadMoreError;

    if (page.loadingMore) {
      return const Padding(
        padding: EdgeInsets.all(AppConstants.spacingMD),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (error != null) {
      return Padding(
        padding: const EdgeInsets.only(top: AppConstants.spacingSM),
        child: Row(
          children: [
            Expanded(
              child: Text(error.message, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error)),
            ),
            TextButton(onPressed: onRetry, child: const Text('Pokušaj')),
          ],
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
