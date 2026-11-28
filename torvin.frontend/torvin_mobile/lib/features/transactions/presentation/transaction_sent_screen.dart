import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/details_panel.dart';

/// Potvrda poslije slanja: koliko je poslano, kome i referentni broj. Forma je zamijenjena ovim ekranom, pa Back
/// ne vraća na već poslanu transakciju.
class TransactionSentScreen extends StatelessWidget {
  const TransactionSentScreen({super.key, required this.transaction});

  final TransactionResponse transaction;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final amount = MoneyFormatter.format(transaction.amount, currency: transaction.currencyCode);
    final myCard = transaction.myCardLast4Digits;

    return Scaffold(
      appBar: AppBar(title: const Text('Novac je poslan')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppConstants.spacingLG),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: AppConstants.emptyIconBox,
                height: AppConstants.emptyIconBox,
                decoration: BoxDecoration(color: colors.successTint, shape: BoxShape.circle),
                child: Icon(LucideIcons.circleCheck, size: AppConstants.emptyIconSize, color: colors.successText),
              ),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            // Ime se ne mijenja po padežu, jer se padež ne može pouzdano izvesti iz imena
            Text('Poslali ste $amount', textAlign: TextAlign.center, style: theme.textTheme.headlineSmall),
            const SizedBox(height: AppConstants.spacingXS),
            Text(
              'Primalac: ${transaction.counterpartyName}',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppConstants.spacingLG),
            DetailsPanel(
              children: [
                if (myCard != null)
                  DetailsRow.text(icon: LucideIcons.creditCard, label: 'Sa kartice', text: CardResponse.mask(myCard)),
                DetailsRow.text(icon: LucideIcons.hash, label: 'Referenca', text: transaction.referenceNumber),
                DetailsRow.text(
                  icon: LucideIcons.calendarClock,
                  label: 'Datum i vrijeme',
                  text: DateFormatter.dateTime(transaction.createdAt),
                ),
              ],
            ),
            const SizedBox(height: AppConstants.spacingLG),
            FilledButton(onPressed: () => context.go(AppRoutes.dashboard), child: const Text('Na početnu')),
            const SizedBox(height: AppConstants.spacingRow),
            OutlinedButton(
              onPressed: () => context.pushReplacement(AppRoutes.newTransaction),
              child: const Text('Nova transakcija'),
            ),
          ],
        ),
      ),
    );
  }
}
