import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Kartica iz odgovora API-ja nacrtana kao `TorvinCard`: boja po tipu, ton i katanac po statusu, mreža i balans.
class VirtualCardView extends StatelessWidget {
  const VirtualCardView({super.key, required this.card, this.showName = false});

  final CardResponse card;

  /// U listi se uz tip piše i naziv kartice, jer se kartice razlikuju po njemu. U detaljima je naziv u naslovu.
  final bool showName;

  @override
  Widget build(BuildContext context) {
    final name = card.name;
    return TorvinCard(
      last4Digits: card.last4Digits,
      holderName: card.holderName,
      expiresAt: card.expiresAt,
      color: CardAppearance.color(context.torvinColors, card.cardTypeId),
      typeName: showName && name != null ? '$name · ${card.cardTypeName}' : card.cardTypeName,
      statusLabel: card.cardStatusName,
      statusTone: CardAppearance.tone(card.cardStatusCode),
      balance: card.balance,
      currency: card.currencyCode,
      blocked: CardAppearance.isLocked(card.cardStatusCode),
      network: card.network,
    );
  }
}
