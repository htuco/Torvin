import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/amount_field.dart';
import '../../../app/widgets/filter_chips.dart';
import '../../cards/data/card_providers.dart';
import '../../recipients/data/recipient_models.dart';
import '../../recipients/data/recipient_providers.dart';
import '../data/transaction_providers.dart';
import 'confirm_transaction_dialog.dart';
import 'recipient_picker.dart';

/// Nova transakcija: s moje aktivne kartice drugom korisniku, u valuti kartice. Bez aktivne kartice forma se ne
/// otvara (upute §6), nego se nudi dodavanje kartice.
class NewTransactionScreen extends ConsumerWidget {
  const NewTransactionScreen({super.key, this.recipient});

  /// Primalac izabran prije forme (npr. iz omiljenih).
  final Recipient? recipient;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Kartice i omiljeni se traže paralelno. Omiljeni nisu preduslov, pa njihova greška ne ruši formu, nego stoji
    // iznad pretrage
    final cards = ref.watch(activeCardsProvider);
    final favorites = ref.watch(favoritesRowProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Nova transakcija')),
      body: switch (cards) {
        AsyncError(:final error) when !cards.isLoading => ErrorState(
            title: 'Forma se nije učitala',
            message: ApiException.messageOf(error),
            onRetry: () => ref.invalidate(activeCardsProvider),
          ),
        AsyncData(:final value) when value.isEmpty => EmptyState(
            icon: LucideIcons.creditCard,
            title: 'Za slanje je potrebna kartica',
            message: 'Novac se šalje s aktivne virtuelne kartice. Dodajte karticu ili aktivirajte postojeću.',
            actionLabel: 'Dodaj virtuelnu karticu',
            onAction: () => context.push(AppRoutes.newCard),
          ),
        AsyncData(:final value) => _NewTransactionForm(
            cards: value,
            favorites: favorites.value ?? const [],
            favoritesError: favorites.hasError ? ApiException.messageOf(favorites.error!) : null,
            recipient: recipient,
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _NewTransactionForm extends ConsumerStatefulWidget {
  const _NewTransactionForm({
    required this.cards,
    required this.favorites,
    required this.favoritesError,
    required this.recipient,
  });

  final List<CardResponse> cards;
  final List<FavoriteRecipientResponse> favorites;
  final String? favoritesError;
  final Recipient? recipient;

  @override
  ConsumerState<_NewTransactionForm> createState() => _NewTransactionFormState();
}

class _NewTransactionFormState extends ConsumerState<_NewTransactionForm> with ApiFormState<_NewTransactionForm> {
  /// Brze sume iz dizajna (1k), u valuti izabrane kartice.
  static const List<int> _quickAmounts = [20, 50, 100, 200];

  final _cardField = GlobalKey<FormFieldState<int>>();
  final _amount = TextEditingController();
  final _note = TextEditingController();

  late int _cardId;
  Recipient? _recipient;

  /// Greška skeniranog koda (`code`), ispod primaoca dok se primalac ne promijeni.
  String? _codeError;
  bool _scanning = false;

  CardResponse get _card => widget.cards.firstWhere((card) => card.id == _cardId);

  @override
  void initState() {
    super.initState();
    final cards = widget.cards;
    _cardId = (cards.where((card) => card.isDefault).firstOrNull ?? cards.first).id;
    _recipient = widget.recipient;
  }

  @override
  void dispose() {
    _amount.dispose();
    _note.dispose();
    super.dispose();
  }

  void _selectCard(int id) {
    setState(() => _cardId = id);
    _cardField.currentState?.didChange(id);
  }

  void _clearRecipientError() {
    clearServerError('toUserId');
    if (_codeError != null) {
      setState(() => _codeError = null);
    }
  }

  /// Skeniran kod razrješava server. Iz koda se popune primalac, iznos (ako ga kod nosi) i moja kartica u valuti
  /// koda, kad takvu imam.
  Future<void> _scan() async {
    final code = await ref.read(scanCodeProvider)(context);
    if (code == null || !mounted) {
      return;
    }

    _clearRecipientError();
    setState(() => _scanning = true);
    try {
      final resolved = await ref.read(recipientsRepositoryProvider).resolveCode(code);
      if (!mounted) {
        return;
      }
      final amount = resolved.amount;
      if (amount != null) {
        _amount.text = MoneyFormatter.amountText(amount);
      }
      final sameCurrency = widget.cards.where((card) => card.currencyCode == resolved.currencyCode).firstOrNull;
      if (sameCurrency != null && _card.currencyCode != resolved.currencyCode) {
        _selectCard(sameCurrency.id);
      }
      setState(() => _recipient = Recipient.fromCode(resolved));
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _codeError = error.fieldError('code') ?? error.message);
      }
    } finally {
      if (mounted) {
        setState(() => _scanning = false);
      }
    }
  }

  /// Provjera polja, pa potvrda u dijalogu, pa slanje. Poslije uspjeha forma ustupa mjesto potvrdi, pa Back ne
  /// vraća na već poslanu transakciju.
  Future<void> _continue() async {
    if (busy || !formKey.currentState!.validate()) {
      return;
    }

    final card = _card;
    final recipient = _recipient!;
    final cents = MoneyFormatter.parseCents(_amount.text)!;
    final note = _note.text.trim().isEmpty ? null : _note.text.trim();
    final confirmed = await showConfirmTransactionDialog(
      context,
      amount: MoneyFormatter.format(MoneyFormatter.fromCents(cents), currency: card.currencyCode),
      recipient: recipient,
      cardLabel: card.label,
      note: note,
    );
    if (!confirmed || !mounted) {
      return;
    }

    await submit(() async {
      final transaction = await ref.read(transactionsRepositoryProvider).create(
            fromCardId: card.id,
            toUserId: recipient.userId,
            amountCents: cents,
            note: note,
          );
      if (!mounted) {
        return;
      }
      // Balans kartice, njena historija i moja historija su se promijenili
      refreshTransactions(ref, cardId: card.id);
      context.pushReplacement(AppRoutes.transactionSent, extra: transaction);
    });
  }

  @override
  Widget build(BuildContext context) {
    final card = _card;
    final recipient = _recipient;
    final currencyDiffers =
        recipient?.currencyCode != null && recipient?.currencyCode != card.currencyCode;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppConstants.spacingLG),
      child: Form(
        key: formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TorvinDropdownField<int>(
              key: _cardField,
              label: 'Sa kartice',
              initialValue: _cardId,
              helper: 'Dostupno ${MoneyFormatter.format(card.balance, currency: card.currencyCode)}',
              items: [
                for (final card in widget.cards) DropdownMenuItem(value: card.id, child: Text(card.label)),
              ],
              forceErrorText: serverError('fromCardId'),
              validator: (value) => value == null ? 'Kartica je obavezna.' : null,
              onChanged: (value) {
                if (value != null) {
                  setState(() => _cardId = value);
                }
                clearServerError('fromCardId');
              },
            ),
            const SizedBox(height: AppConstants.spacingMD),
            AmountField(
              controller: _amount,
              label: 'Iznos',
              currency: card.currencyCode,
              forceErrorText: serverError('amount'),
              onChanged: (_) {
                clearServerError('amount');
                // Brza suma je izabrana dok iznos u polju odgovara njoj
                setState(() {});
              },
              validator: Validators.combine([
                Validators.required('Iznos je obavezan.'),
                Validators.amount,
                Validators.amountAtMost(
                  MoneyFormatter.roundToCents(card.balance),
                  Validators.insufficientFundsMessage(card.last4Digits),
                ),
              ]),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FilterChips<int?>(
              options: [for (final amount in _quickAmounts) ('$amount', amount * MoneyFormatter.centsPerUnit)],
              selected: MoneyFormatter.parseCents(_amount.text),
              onSelected: (cents) {
                _amount.text = MoneyFormatter.centsText(cents!);
                clearServerError('amount');
                setState(() {});
              },
            ),
            const SizedBox(height: AppConstants.spacingMD),
            RecipientPicker(
              selected: recipient,
              favorites: widget.favorites,
              favoritesError: widget.favoritesError,
              scanning: _scanning,
              error: serverError('toUserId') ?? _codeError,
              onEdited: _clearRecipientError,
              onScan: _scan,
              onSelected: (value) => setState(() => _recipient = value),
            ),
            if (currencyDiffers) ...[
              const SizedBox(height: AppConstants.spacingSM),
              const InfoNote('Novac stiže na primaočevu karticu u valuti vaše kartice.'),
            ],
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _note,
              label: 'Napomena (opciono)',
              hint: 'npr. Kirija za maj',
              textCapitalization: TextCapitalization.sentences,
              onChanged: clearServerError,
              forceErrorText: serverError('note'),
              validator: Validators.note,
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Nastavi', busy: busy, onPressed: _continue),
          ],
        ),
      ),
    );
  }
}
