import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';
import 'package:u_credit_card/u_credit_card.dart';

import '../formatting/date_formatter.dart';
import '../formatting/money_formatter.dart';
import '../theme/app_constants.dart';
import '../theme/torvin_colors.dart';
import 'money_text.dart';
import 'status_chip.dart';

/// Virtuelna kartica: crtež iz paketa `u_credit_card`, a ispod tip, status i balans u formatu aplikacije.
///
/// Pun broj kartice se nikad ne čuva ni ne prosljeđuje, nego samo zadnje 4 cifre.
class TorvinCard extends StatelessWidget {
  const TorvinCard({
    super.key,
    required this.last4Digits,
    required this.holderName,
    required this.expiresAt,
    required this.color,
    required this.typeName,
    required this.statusLabel,
    required this.statusTone,
    required this.balance,
    this.currency = MoneyFormatter.defaultCurrency,
    this.blocked = false,
  });

  final String last4Digits;
  final String holderName;
  final DateTime expiresAt;

  /// Boja po tipu kartice iz `TorvinColors` (`cardPrimary`, `cardDark`, `cardSlate`). Bira je ekran.
  final Color color;

  /// Naziv tipa iz referentnih podataka ("Debitna", "Štednja").
  final String typeName;
  final String statusLabel;
  final StatusTone statusTone;
  final num balance;
  final String currency;

  /// Blokirana kartica prelazi u sivoplavu boju s katancem (DS v2).
  final bool blocked;

  /// Paket prikaže prve 4 i zadnje 4 cifre, a ostalo maskira. Zvjezdice na mjestu prve četiri daju `**** **** **** 4821`.
  static String maskedNumber(String last4Digits) => '${'*' * 12}$last4Digits';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final cardColor = blocked ? colors.cardBlocked : color;

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        CreditCardUi(
          cardHolderFullName: holderName,
          cardNumber: maskedNumber(last4Digits),
          validThru: DateFormatter.cardExpiry(expiresAt),
          // Iste boje daju ravnu karticu; gradijent je opcija za kasnije
          topLeftColor: cardColor,
          bottomRightColor: cardColor,
          // Paket bi napisao engleski "Credit"/"Debit" i balans kao "$1245.00", pa tip i balans idu ispod
          cardType: CardType.other,
          showBalance: false,
          // Mreža (Visa, Mastercard) se još ne čuva
          creditCardType: CreditCardType.none,
          doesSupportNfc: false,
          showValidFrom: false,
          disableHapticFeedBack: true,
        ),
        const SizedBox(height: AppConstants.spacingRow),
        // Wrap prelama red na uskom ekranu umjesto da tip, status i balans izađu preko ivice
        Wrap(
          spacing: AppConstants.spacingSM,
          runSpacing: AppConstants.spacingSM,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            Text(typeName, style: theme.textTheme.labelMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
            if (blocked) Icon(LucideIcons.lock, size: AppConstants.iconSize, color: theme.colorScheme.onSurfaceVariant),
            StatusChip(label: statusLabel, tone: statusTone),
            MoneyText(balance, currency: currency),
          ],
        ),
      ],
    );
  }
}
