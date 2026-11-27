import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

import '../data/cards_repository.dart';

/// Šta je korisnik izabrao u dijalogu prijave.
class CardReport {
  const CardReport({required this.reason, required this.note});

  final CardReportReason reason;
  final String? note;
}

/// Potvrda prijave gubitka ili krađe. Vraća izbor tek kad je forma ispravna, a `null` kad korisnik odustane.
Future<CardReport?> showReportCardDialog(BuildContext context, CardResponse card) =>
    showTorvinDialog<CardReport>(context, builder: (_) => _ReportCardDialog(card: card));

class _ReportCardDialog extends StatefulWidget {
  const _ReportCardDialog({required this.card});

  final CardResponse card;

  @override
  State<_ReportCardDialog> createState() => _ReportCardDialogState();
}

class _ReportCardDialogState extends State<_ReportCardDialog> {
  static const int _noteMaxLength = 200;

  final _formKey = GlobalKey<FormState>();
  final _note = TextEditingController();
  CardReportReason? _reason;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  void _confirm() {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final note = _note.text.trim();
    Navigator.of(context).pop(CardReport(reason: _reason!, note: note.isEmpty ? null : note));
  }

  @override
  Widget build(BuildContext context) {
    final card = widget.card;
    return TorvinDialog(
      icon: LucideIcons.triangleAlert,
      destructive: true,
      title: 'Prijaviti karticu ${card.maskedNumber}?',
      message: 'Prijava je trajna: kartica se odmah gasi i više se ne može aktivirati.',
      // Blokada je povratna, pa je savjet za aktivnu karticu. Blokirana kartica je već privremeno ugašena
      note: card.cardStatusCode == CardStatusCodes.active
          ? 'Ako ste je samo zagubili, možete je privremeno blokirati.'
          : null,
      confirmLabel: 'Prijavi',
      onConfirm: _confirm,
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            FormField<CardReportReason>(
              autovalidateMode: AutovalidateMode.onUserInteraction,
              validator: (_) => _reason == null ? 'Razlog prijave je obavezan.' : null,
              builder: (field) => Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Razlog', style: FieldNote.labelStyle(Theme.of(context))),
                  RadioGroup<CardReportReason>(
                    groupValue: _reason,
                    onChanged: (value) {
                      setState(() => _reason = value);
                      field.didChange(value);
                    },
                    child: const Column(
                      children: [
                        RadioListTile(
                          value: CardReportReason.lost,
                          title: Text('Izgubljena'),
                          contentPadding: EdgeInsets.zero,
                        ),
                        RadioListTile(
                          value: CardReportReason.stolen,
                          title: Text('Ukradena'),
                          contentPadding: EdgeInsets.zero,
                        ),
                      ],
                    ),
                  ),
                  FieldNote(error: field.errorText),
                ],
              ),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            TorvinTextField(
              controller: _note,
              label: 'Napomena (opciono)',
              hint: 'npr. Izgubljena u tramvaju',
              textCapitalization: TextCapitalization.sentences,
              validator: Validators.maxLength(_noteMaxLength, 'Napomena može imati najviše $_noteMaxLength znakova.'),
            ),
          ],
        ),
      ),
    );
  }
}
