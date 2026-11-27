import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:torvin_core/torvin_core.dart';

import '../data/card_providers.dart';

/// QR kod kojim drugi korisnik šalje novac na ovu karticu. Kod potpisuje server i važi 15 minuta.
class ReceiveCodeScreen extends ConsumerWidget {
  const ReceiveCodeScreen({super.key, required this.cardId});

  final int cardId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final code = ref.watch(receiveCodeProvider(cardId));
    final card = ref.watch(cardProvider(cardId)).value;
    // Skener čita tamne module na svijetloj podlozi, pa je QR u svijetlim bojama i u tamnoj temi
    final qrColors = AppTheme.lightScheme();

    return Scaffold(
      appBar: AppBar(title: const Text('QR za primanje')),
      body: code.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorState(
          title: 'Kod se nije napravio',
          message: ApiException.messageOf(error),
          onRetry: () => ref.invalidate(receiveCodeProvider(cardId)),
        ),
        data: (receive) => SingleChildScrollView(
          padding: const EdgeInsets.all(AppConstants.spacingLG),
          child: Column(
            children: [
              if (card != null) ...[
                Text('${card.displayName} ${card.maskedNumber}', style: theme.textTheme.titleLarge),
                const SizedBox(height: AppConstants.spacingXS),
                Text(
                  'Uplate stižu u valuti kartice (${card.currencyCode}).',
                  style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                ),
                const SizedBox(height: AppConstants.spacingLG),
              ],
              Container(
                padding: const EdgeInsets.all(AppConstants.spacingMD),
                decoration: BoxDecoration(
                  color: qrColors.surfaceContainerLowest,
                  borderRadius: BorderRadius.circular(AppConstants.radiusCard),
                  border: Border.all(color: qrColors.outline),
                ),
                child: QrImageView(
                  data: receive.code,
                  size: AppConstants.qrSize,
                  padding: EdgeInsets.zero,
                  backgroundColor: qrColors.surfaceContainerLowest,
                  eyeStyle: QrEyeStyle(eyeShape: QrEyeShape.square, color: qrColors.onSurface),
                  dataModuleStyle: QrDataModuleStyle(
                    dataModuleShape: QrDataModuleShape.square,
                    color: qrColors.onSurface,
                  ),
                  semanticsLabel: 'QR kod za primanje novca',
                ),
              ),
              const SizedBox(height: AppConstants.spacingMD),
              Text(
                'Pokažite kod osobi koja vam šalje novac. Kod važi do ${DateFormatter.time(receive.expiresAt)}.',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: AppConstants.spacingLG),
              OutlinedButton.icon(
                onPressed: () => ref.invalidate(receiveCodeProvider(cardId)),
                icon: const Icon(LucideIcons.rotateCcw, size: AppConstants.iconSize),
                label: const Text('Novi kod'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
