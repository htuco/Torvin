import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../data/card_providers.dart';

/// Nova kartica: tip i valuta iz baze, opcioni naziv, broj i istek. Poslije snimanja ide unos koda iz emaila.
class AddCardScreen extends ConsumerWidget {
  const AddCardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final types = ref.watch(cardTypesProvider);
    final currencies = ref.watch(currenciesProvider);
    final error = types.error ?? currencies.error;
    // Poslije "Pokušaj ponovo" se čeka novi odgovor, a ne prikazuje stara greška
    final loading = types.isLoading || currencies.isLoading;

    return Scaffold(
      appBar: AppBar(title: const Text('Nova kartica')),
      body: switch ((types.value, currencies.value)) {
        _ when error != null && !loading => ErrorState(
            title: 'Forma se nije učitala',
            message: ApiException.messageOf(error),
            onRetry: () {
              ref.invalidate(cardTypesProvider);
              ref.invalidate(currenciesProvider);
            },
          ),
        (final types?, final currencies?) when types.isEmpty || currencies.isEmpty => const EmptyState(
            icon: LucideIcons.creditCard,
            title: 'Dodavanje trenutno nije moguće',
            message: 'Nema tipova kartica ili valuta. Obratite se podršci.',
          ),
        (final types?, final currencies?) => _AddCardForm(types: types, currencies: currencies),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _AddCardForm extends ConsumerStatefulWidget {
  const _AddCardForm({required this.types, required this.currencies});

  final List<CardTypeResponse> types;
  final List<CurrencyResponse> currencies;

  @override
  ConsumerState<_AddCardForm> createState() => _AddCardFormState();
}

class _AddCardFormState extends ConsumerState<_AddCardForm> with ApiFormState<_AddCardForm> {
  static const int _nameMaxLength = 50;
  static const int _cardNumberLength = 16;

  /// Kartica može važiti najviše 10 godina unaprijed, isto kao `CardService.MaxExpiryYears` na backendu.
  static const int _maxExpiryYears = 10;

  final _name = TextEditingController();
  // Pun broj živi samo u ovom polju i ide samo u tijelo zahtjeva. Ne čuva se u stanju ni u ruti
  final _number = TextEditingController();
  int? _cardTypeId;
  int? _currencyId;

  // Mjesec i godina isteka kao na kartici ("08", "29"), a server dobija "08/29"
  String? _expiryMonth;
  String? _expiryYear;

  @override
  void dispose() {
    _name.dispose();
    _number.dispose();
    super.dispose();
  }

  Future<void> _submit() => submit(() async {
        final name = _name.text.trim();
        final card = await ref.read(cardsRepositoryProvider).create(
              cardTypeId: _cardTypeId!,
              currencyId: _currencyId!,
              name: name.isEmpty ? null : name,
              cardNumber: _number.text,
              expiry: '$_expiryMonth/$_expiryYear',
            );
        if (!mounted) {
          return;
        }
        refreshCards(ref);
        // Forma s brojem kartice se zamjenjuje kodom, pa Back s koda vodi na listu, a ne nazad na broj
        context.pushReplacement(AppRoutes.activateCard(card.id));
      });

  static String _twoDigits(int value) => value.toString().padLeft(2, '0');

  /// Od tekuće godine do najviše 10 godina unaprijed.
  static List<int> _expiryYears() {
    final now = DateTime.now().year;
    return [for (var year = now; year <= now + _maxExpiryYears; year++) year];
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppConstants.spacingLG),
      child: Form(
        key: formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TorvinDropdownField<int>(
              label: 'Tip kartice',
              hint: 'Izaberite tip',
              items: [for (final type in widget.types) DropdownMenuItem(value: type.id, child: Text(type.name))],
              forceErrorText: serverError('cardTypeId'),
              validator: (value) => value == null ? 'Tip kartice je obavezan.' : null,
              onChanged: (value) {
                _cardTypeId = value;
                clearServerError('cardTypeId');
              },
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinDropdownField<int>(
              label: 'Valuta',
              hint: 'Izaberite valutu',
              helper: 'Novac se šalje i prima u valuti kartice.',
              items: [
                for (final currency in widget.currencies)
                  DropdownMenuItem(value: currency.id, child: Text('${currency.code} · ${currency.name}')),
              ],
              forceErrorText: serverError('currencyId'),
              validator: (value) => value == null ? 'Valuta je obavezna.' : null,
              onChanged: (value) {
                _currencyId = value;
                clearServerError('currencyId');
              },
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _name,
              label: 'Naziv (opciono)',
              hint: 'npr. Putovanja',
              textCapitalization: TextCapitalization.sentences,
              textInputAction: TextInputAction.next,
              onChanged: clearServerError,
              forceErrorText: serverError('name'),
              validator: Validators.maxLength(_nameMaxLength, 'Naziv može imati najviše $_nameMaxLength znakova.'),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _number,
              label: 'Broj kartice',
              hint: '16 cifara',
              helper: '16 cifara bez razmaka. Čuvaju se samo zadnje 4.',
              keyboardType: TextInputType.number,
              textInputAction: TextInputAction.done,
              inputFormatters: [
                FilteringTextInputFormatter.digitsOnly,
                LengthLimitingTextInputFormatter(_cardNumberLength),
              ],
              onChanged: clearServerError,
              forceErrorText: serverError('cardNumber'),
              validator: Validators.combine([
                Validators.required('Broj kartice je obavezan.'),
                Validators.cardNumber,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: TorvinDropdownField<String>(
                    label: 'Mjesec isteka',
                    hint: 'MM',
                    items: [
                      for (var month = 1; month <= 12; month++)
                        DropdownMenuItem(value: _twoDigits(month), child: Text(_twoDigits(month))),
                    ],
                    validator: (value) => value == null ? 'Mjesec isteka je obavezan.' : null,
                    onChanged: (value) {
                      _expiryMonth = value;
                      clearServerError('expiry');
                    },
                  ),
                ),
                const SizedBox(width: AppConstants.spacingRow),
                Expanded(
                  child: TorvinDropdownField<String>(
                    label: 'Godina isteka',
                    hint: 'GGGG',
                    items: [
                      for (final year in _expiryYears())
                        DropdownMenuItem(value: _twoDigits(year % 100), child: Text('$year')),
                    ],
                    // Server javlja istekao mjesec ili predaleku godinu, a to je greška datuma kao cjeline
                    forceErrorText: serverError('expiry'),
                    validator: (value) => value == null ? 'Godina isteka je obavezna.' : null,
                    onChanged: (value) {
                      _expiryYear = value;
                      clearServerError('expiry');
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Dodaj karticu', busy: busy, onPressed: _submit),
          ],
        ),
      ),
    );
  }
}
