import 'package:intl/intl.dart';

/// Iznos kao u dizajnu: `1.245,00 BAM`, `−120,00 BAM`, uz predznak `+500,00 BAM`.
class MoneyFormatter {
  MoneyFormatter._();

  /// Pravi znak minus (U+2212), širi od crtice, kao u dizajnu.
  static const String minus = '−';

  /// Valuta kad pozivalac ne zada drugu (osnovna valuta sistema).
  static const String defaultCurrency = 'BAM';

  static final NumberFormat _number = NumberFormat('#,##0.00', 'en_US');

  /// `signed` dodaje `+` pozitivnom iznosu (red transakcije). Negativan iznos uvijek ima minus.
  static String format(num amount, {String currency = defaultCurrency, bool signed = false}) {
    final cents = roundToCents(amount);
    final text = _number
        .format(cents.abs() / 100)
        // en_US daje 1,245.00, a kod nas je 1.245,00
        .replaceAll(',', ' ')
        .replaceAll('.', ',')
        .replaceAll(' ', '.');

    final sign = cents < 0 ? minus : (signed && cents > 0 ? '+' : '');
    return '$sign$text $currency';
  }

  /// Iznos u feninzima, zaokružen na pola dalje od nule (2,675 → 268, −0,005 → −1).
  ///
  /// Backend čuva novac kao decimal(19,4), pa iznos ima najviše 4 decimale. Zato se prvo pređe na cijeli
  /// broj desetohiljaditih dijelova, a zaokruživanje se radi nad cijelim brojem. Direktno `(x * 100).round()`
  /// bi pogriješilo kod 2,675, jer je 2,675 * 100 u double-u 267,4999…
  static int roundToCents(num amount) {
    final units = (amount * 10000).round();
    final whole = units.abs() ~/ 100;
    final rest = units.abs() % 100;
    final cents = rest >= 50 ? whole + 1 : whole;
    return units < 0 ? -cents : cents;
  }
}
