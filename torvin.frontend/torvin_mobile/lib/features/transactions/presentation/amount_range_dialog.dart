import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/widgets/amount_field.dart';

/// Izabran raspon iznosa u feninzima. Obje granice prazne znače da filter iznosa nije izabran.
typedef AmountRange = ({int? fromCents, int? toCents});

/// Filter iznosa historije. Vraća raspon na "Primijeni", a `null` kad korisnik odustane.
Future<AmountRange?> showAmountRangeDialog(BuildContext context, {required AmountRange initial}) =>
    showTorvinDialog<AmountRange>(context, builder: (_) => _AmountRangeDialog(initial: initial));

class _AmountRangeDialog extends StatefulWidget {
  const _AmountRangeDialog({required this.initial});

  final AmountRange initial;

  @override
  State<_AmountRangeDialog> createState() => _AmountRangeDialogState();
}

class _AmountRangeDialogState extends State<_AmountRangeDialog> {
  final _formKey = GlobalKey<FormState>();
  late final _from = TextEditingController(text: _text(widget.initial.fromCents));
  late final _to = TextEditingController(text: _text(widget.initial.toCents));

  static String _text(int? cents) => cents == null ? '' : MoneyFormatter.centsText(cents);

  @override
  void dispose() {
    _from.dispose();
    _to.dispose();
    super.dispose();
  }

  void _apply() {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final range = (fromCents: MoneyFormatter.parseCents(_from.text), toCents: MoneyFormatter.parseCents(_to.text));
    Navigator.of(context).pop(range);
  }

  @override
  Widget build(BuildContext context) {
    return TorvinDialog(
      icon: LucideIcons.banknote,
      title: 'Iznos',
      message: 'Prikazuju se transakcije s iznosom u ovim granicama. Prazno polje nema granicu.',
      confirmLabel: 'Primijeni',
      onConfirm: _apply,
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AmountField(controller: _from, label: 'Od', hint: 'npr. 50,00', validator: Validators.amount),
            const SizedBox(height: AppConstants.spacingMD),
            AmountField(
              controller: _to,
              label: 'Do',
              hint: 'npr. 200,00',
              validator: Validators.combine([
                Validators.amount,
                Validators.amountAtLeast(() => MoneyFormatter.parseCents(_from.text), Validators.amountRangeMessage),
              ]),
            ),
          ],
        ),
      ),
    );
  }
}
