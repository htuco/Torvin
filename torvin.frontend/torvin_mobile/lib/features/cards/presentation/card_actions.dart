import 'package:torvin_core/torvin_core.dart';

/// Akcije vlasnika u detaljima kartice.
enum CardAction { activate, block, unblock, report, receive }

/// Da li je akcija dostupna, a ako nije, zašto. Razlog se prikazuje ispod akcija.
class CardActionState {
  const CardActionState.enabled() : enabled = true, reason = null;

  const CardActionState.disabled(String this.reason) : enabled = false;

  final bool enabled;
  final String? reason;
}

// Poruke su iste kao na backendu (`CardStateMachine`, `ReceiveCodeService`), da korisnik ne dobije dva objašnjenja
const String pendingReason = 'Kartica još nije aktivirana kodom iz emaila.';
const String receiveNeedsActiveReason = 'Kod za primanje se pravi samo za aktivnu karticu.';
const String lostReason = 'Kartica prijavljena kao izgubljena ne može se ponovo aktivirati.';
const String stolenReason = 'Kartica prijavljena kao ukradena ne može se ponovo aktivirati.';
const String closedReason = 'Zatvorena kartica ne može promijeniti status.';
// Isto kao `CardService.BlockedByAdmin`
const String blockedByAdminReason = 'Karticu je blokirao administrator. Obratite se podršci.';

/// Šta vlasnik može uraditi s karticom u datom statusu. Ovo samo prikazuje dugmad: dozvoljene prelaze i dalje
/// odlučuje server (`CardStateMachine` i provjera blokade administratora).
Map<CardAction, CardActionState> cardActionsFor(String statusCode, {bool blockedByAdmin = false}) {
  const on = CardActionState.enabled();
  return switch (statusCode) {
    CardStatusCodes.pending => {
        CardAction.activate: on,
        CardAction.block: const CardActionState.disabled(pendingReason),
        CardAction.report: const CardActionState.disabled(pendingReason),
        CardAction.receive: const CardActionState.disabled(pendingReason),
      },
    CardStatusCodes.active => {CardAction.block: on, CardAction.report: on, CardAction.receive: on},
    CardStatusCodes.blocked => {
        // Blokadu administratora vlasnik ne poništava, ali karticu i dalje smije prijaviti kao izgubljenu
        CardAction.unblock: blockedByAdmin ? const CardActionState.disabled(blockedByAdminReason) : on,
        CardAction.report: on,
        CardAction.receive: const CardActionState.disabled(receiveNeedsActiveReason),
      },
    _ => _allDisabled(switch (statusCode) {
        CardStatusCodes.lost => lostReason,
        CardStatusCodes.stolen => stolenReason,
        _ => closedReason,
      }),
  };
}

Map<CardAction, CardActionState> _allDisabled(String reason) => {
      CardAction.block: CardActionState.disabled(reason),
      CardAction.report: CardActionState.disabled(reason),
      CardAction.receive: CardActionState.disabled(reason),
    };
