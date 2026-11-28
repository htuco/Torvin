import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:torvin_core/torvin_core.dart';

/// Polje za iznos u našem obliku (`1.245,00`). Propušta samo cifre, tačku i zarez, pa se minus ne može ukucati.
/// Tekst se pretvara u feninge kroz `MoneyFormatter.parseCents`, nikad kroz `double.parse`.
class AmountField extends StatelessWidget {
  const AmountField({
    super.key,
    required this.controller,
    required this.label,
    required this.validator,
    this.currency,
    this.hint = '0,00',
    this.helper,
    this.forceErrorText,
    this.onChanged,
  });

  final TextEditingController controller;
  final String label;
  final FormFieldValidator<String> validator;

  /// Kod valute iza iznosa (valuta kartice). Bez njega polje nema sufiks.
  final String? currency;
  final String hint;
  final String? helper;
  final String? forceErrorText;
  final ValueChanged<String>? onChanged;

  static final _allowed = FilteringTextInputFormatter.allow(RegExp('[0-9.,]'));

  @override
  Widget build(BuildContext context) {
    final currency = this.currency;
    return TorvinTextField(
      controller: controller,
      label: label,
      hint: hint,
      helper: helper,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [_allowed],
      suffix: currency == null
          ? null
          : Padding(
              padding: const EdgeInsets.only(right: AppConstants.spacingMD),
              child: Text(
                currency,
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
              ),
            ),
      forceErrorText: forceErrorText,
      onChanged: onChanged,
      validator: validator,
    );
  }
}
