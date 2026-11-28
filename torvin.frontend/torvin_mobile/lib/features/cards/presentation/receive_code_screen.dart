import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/widgets/amount_field.dart';
import '../data/card_providers.dart';
import '../data/cards_repository.dart';

/// QR kod kojim drugi korisnik šalje novac na ovu karticu. Kod potpisuje server i važi 15 minuta, a može nositi
/// iznos koji pošiljalac dobija popunjen.
class ReceiveCodeScreen extends ConsumerStatefulWidget {
  const ReceiveCodeScreen({super.key, required this.cardId});

  final int cardId;

  @override
  ConsumerState<ReceiveCodeScreen> createState() => _ReceiveCodeScreenState();
}

class _ReceiveCodeScreenState extends ConsumerState<ReceiveCodeScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amount = TextEditingController();

  /// Iznos u kodu koji je na ekranu. `null` je kod bez iznosa.
  int? _amountCents;

  /// Greška iznosa sa servera se sakrije pri prvoj izmjeni polja, jer bi inače blokirala novi pokušaj.
  bool _amountEdited = false;

  ReceiveCodeRequest get _request => (cardId: widget.cardId, amountCents: _amountCents);

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  /// Novi kod s iznosom iz polja. Isti iznos traži novi kod od servera, jer stari ističe.
  void _newCode() {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final cents = MoneyFormatter.parseCents(_amount.text);
    if (cents == _amountCents) {
      ref.invalidate(receiveCodeProvider(_request));
    }
    setState(() {
      _amountCents = cents;
      _amountEdited = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final code = ref.watch(receiveCodeProvider(_request));
    final card = ref.watch(cardProvider(widget.cardId)).value;
    final error = code.error;
    final amountError = error is ApiException && !_amountEdited ? error.fieldError('amount') : null;

    return Scaffold(
      appBar: AppBar(title: const Text('QR za primanje')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppConstants.spacingLG),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (card != null) _CardHeader(card: card),
            switch (code) {
              AsyncData(:final value) => _QrCode(code: value, currency: card?.currencyCode),
              AsyncError(:final error) when error is! ApiException || error.fieldError('amount') == null => ErrorState(
                  title: 'Kod se nije napravio',
                  message: ApiException.messageOf(error),
                  onRetry: () => ref.invalidate(receiveCodeProvider(_request)),
                ),
              AsyncError() => const SizedBox.shrink(),
              _ => const SizedBox(
                  height: AppConstants.qrSize,
                  child: Center(child: CircularProgressIndicator()),
                ),
            },
            const SizedBox(height: AppConstants.spacingLG),
            Form(
              key: _formKey,
              child: AmountField(
                controller: _amount,
                label: 'Iznos (opciono)',
                hint: 'npr. 50,00',
                helper: 'Pošiljalac dobija popunjen iznos.',
                currency: card?.currencyCode,
                forceErrorText: amountError,
                onChanged: (_) {
                  if (amountError != null) {
                    setState(() => _amountEdited = true);
                  }
                },
                validator: Validators.amount,
              ),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            OutlinedButton.icon(
              onPressed: code.isLoading ? null : _newCode,
              icon: const Icon(LucideIcons.rotateCcw, size: AppConstants.iconSize),
              label: const Text('Novi kod'),
            ),
          ],
        ),
      ),
    );
  }
}

class _CardHeader extends StatelessWidget {
  const _CardHeader({required this.card});

  final CardResponse card;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppConstants.spacingLG),
      child: Column(
        children: [
          Text(card.label, style: theme.textTheme.titleLarge),
          const SizedBox(height: AppConstants.spacingXS),
          Text(
            'Uplate stižu u valuti kartice (${card.currencyCode}).',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ],
      ),
    );
  }
}

class _QrCode extends StatelessWidget {
  const _QrCode({required this.code, required this.currency});

  final ReceiveCode code;

  /// Valuta kartice. Dok kartica nije učitana, iznos se ne prikazuje, da ne stoji pogrešna valuta.
  final String? currency;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // Skener čita tamne module na svijetloj podlozi, pa je QR u svijetlim bojama i u tamnoj temi
    final qrColors = AppTheme.lightScheme();
    final amount = code.amount;
    final currency = this.currency;
    final secondary = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    return Column(
      children: [
        Container(
          padding: const EdgeInsets.all(AppConstants.spacingMD),
          decoration: BoxDecoration(
            color: qrColors.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(AppConstants.radiusCard),
            border: Border.all(color: qrColors.outline),
          ),
          child: QrImageView(
            data: code.code,
            size: AppConstants.qrSize,
            padding: EdgeInsets.zero,
            backgroundColor: qrColors.surfaceContainerLowest,
            eyeStyle: QrEyeStyle(eyeShape: QrEyeShape.square, color: qrColors.onSurface),
            dataModuleStyle: QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: qrColors.onSurface),
            semanticsLabel: 'QR kod za primanje novca',
          ),
        ),
        if (amount != null && currency != null) ...[
          const SizedBox(height: AppConstants.spacingMD),
          Text(
            'Kod traži ${MoneyFormatter.format(amount, currency: currency)}.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
          ),
        ],
        const SizedBox(height: AppConstants.spacingSM),
        Text(
          'Pokažite kod osobi koja vam šalje novac. Kod važi do ${DateFormatter.time(code.expiresAt)}.',
          textAlign: TextAlign.center,
          style: secondary,
        ),
      ],
    );
  }
}
