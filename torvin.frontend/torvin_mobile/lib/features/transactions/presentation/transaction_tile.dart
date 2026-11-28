import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Red historije: smjer kao ikona, druga strana, vrijeme i iznos s predznakom. Status se prikazuje samo kad
/// transakcija nije izvršena, jer je izvršena uobičajen slučaj. Dodir otvara detalje.
class TransactionTile extends StatelessWidget {
  const TransactionTile({super.key, required this.transaction, required this.onTap, this.timeOnly = false});

  final TransactionResponse transaction;
  final VoidCallback onTap;

  /// U listi grupisanoj po danu datum je u zaglavlju, pa red ima vrijeme i moju karticu ("14:22 · •••• 4821").
  final bool timeOnly;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final sent = transaction.direction == TransactionDirection.sent;
    final myCard = transaction.myCardLast4Digits;
    final subtitle = timeOnly
        ? [DateFormatter.time(transaction.createdAt), if (myCard != null) CardResponse.mask(myCard)].join(' · ')
        : DateFormatter.dateTime(transaction.createdAt);

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingMD, vertical: AppConstants.spacingRow),
        child: Row(
          children: [
            Container(
              width: AppConstants.listIconBox,
              height: AppConstants.listIconBox,
              decoration: BoxDecoration(
                color: sent ? colors.errorTint : colors.successTint,
                borderRadius: BorderRadius.circular(AppConstants.radiusControl),
              ),
              child: Icon(
                sent ? LucideIcons.arrowUpRight : LucideIcons.arrowDownLeft,
                size: AppConstants.listIconSize,
                color: sent ? colors.errorText : colors.successText,
              ),
            ),
            const SizedBox(width: AppConstants.spacingRow),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    transaction.counterpartyName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w500),
                  ),
                  Text(subtitle, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
                  if (!transaction.isCompleted) ...[
                    const SizedBox(height: AppConstants.spacingXS),
                    StatusChip(
                      label: transaction.transactionStatusName,
                      tone: TransactionAppearance.tone(transaction.transactionStatusCode),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: AppConstants.spacingRow),
            MoneyText(
              transaction.signedAmount,
              currency: transaction.currencyCode,
              signed: true,
              style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
          ],
        ),
      ),
    );
  }
}
