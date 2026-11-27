import 'dart:ui';

import '../theme/torvin_colors.dart';
import '../widgets/status_chip.dart';
import 'card_status_codes.dart';

/// Kako kartica izgleda: boja po tipu i ton statusa. Isto pravilo za mobilnu i desktop.
abstract final class CardAppearance {
  /// Tipovi iz baze nemaju boju, pa se boja bira po ID-u tipa redom kroz tri boje kartice iz DS v2
  /// (1 Debitna → primarna, 2 Prepaid → tamna, 3 Poslovna → slate). Novi tip dobija sljedeću u krugu.
  static Color color(TorvinColors colors, int cardTypeId) {
    final palette = [colors.cardPrimary, colors.cardDark, colors.cardSlate];
    return palette[(cardTypeId - 1) % palette.length];
  }

  static StatusTone tone(String statusCode) => switch (statusCode) {
        CardStatusCodes.active => StatusTone.success,
        CardStatusCodes.pending => StatusTone.warning,
        CardStatusCodes.closed => StatusTone.neutral,
        _ => StatusTone.error,
      };

  /// Kartica koja se ne može koristiti dobija sivoplavu boju i katanac. Kartica na čekanju to nije, jer se
  /// aktivira kodom.
  static bool isLocked(String statusCode) =>
      statusCode != CardStatusCodes.active && statusCode != CardStatusCodes.pending;
}
