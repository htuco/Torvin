import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/widgets/details_panel.dart';
import '../../recipients/data/recipient_models.dart';

/// Potvrda prije slanja novca (upute §6): iznos, primalac, kartica i napomena. Vraća `true` samo na "Pošalji", a
/// "Odustani", X i klik van dijaloga ne šalju ništa.
Future<bool> showConfirmTransactionDialog(
  BuildContext context, {
  required String amount,
  required Recipient recipient,
  required String cardLabel,
  required String? note,
}) async {
  final confirmed = await showTorvinDialog<bool>(
    context,
    builder: (context) => TorvinDialog(
      icon: LucideIcons.send,
      title: 'Poslati $amount?',
      note: 'Novac stiže primaocu odmah.',
      confirmLabel: 'Pošalji',
      onConfirm: () => Navigator.of(context).pop(true),
      child: DetailsPanel(
        children: [
          DetailsRow.text(
            icon: LucideIcons.user,
            label: 'Primalac',
            text: '${recipient.fullName} (@${recipient.username})',
          ),
          DetailsRow.text(icon: LucideIcons.creditCard, label: 'Sa kartice', text: cardLabel),
          if (note != null) DetailsRow.text(icon: LucideIcons.messageSquare, label: 'Napomena', text: note),
        ],
      ),
    ),
  );
  return confirmed ?? false;
}
