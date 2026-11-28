import 'package:intl/intl.dart';

/// Iznos kao u dizajnu: `1.245,00 BAM`, `−120,00 BAM`, uz predznak `+500,00 BAM`.
class MoneyFormatter {
  MoneyFormatter._();

  /// Pravi znak minus (U+2212), širi od crtice, kao u dizajnu.
  static const String minus = '−';

  /// Valuta kad pozivalac ne zada drugu (osnovna valuta sistema).
  static const String defaultCurrency = 'BAM';

  /// Feninga u jednoj marki (i centi u euru).
  static const int centsPerUnit = 100;

  static final NumberFormat _number = NumberFormat('#,##0.00', 'en_US');

  /// `signed` dodaje `+` pozitivnom iznosu (red transakcije). Negativan iznos uvijek ima minus.
  static String format(num amount, {String currency = defaultCurrency, bool signed = false}) {
    final cents = roundToCents(amount);
    final sign = cents < 0 ? minus : (signed && cents > 0 ? '+' : '');
    return '$sign${_digits(cents.abs())} $currency';
  }

  /// Iznos za polje forme (`1.245,00`), bez valute i predznaka.
  static String amountText(num amount) => _digits(roundToCents(amount).abs());

  // Tačka za hiljade u grupama od tri, pa opciono zarez i jedna ili dvije decimale (1245, 1.245, 1.245,00, 0,5).
  // Prva grupa ne počinje nulom, jer bi se "0.500" inače tiho pročitalo kao 500,00
  static final RegExp _grouped = RegExp(r'^[1-9]\d{0,2}(\.\d{3})+(,\d{1,2})?$');
  static final RegExp _plain = RegExp(r'^\d+(,\d{1,2})?$');

  /// Upisan iznos u feninzima, ili `null` kad tekst nije iznos u našem obliku. Minus, treća decimala i tačka kao
  /// decimalni znak (`12.50`) daju `null`, jer bi se inače tiho poslao drugi iznos od onog koji je korisnik vidio.
  /// Računa se samo s cijelim brojevima, bez `double`.
  static int? parseCents(String text) {
    final value = text.trim();
    if (!_grouped.hasMatch(value) && !_plain.hasMatch(value)) {
      return null;
    }

    final parts = value.replaceAll('.', '').split(',');
    final decimals = parts.length == 2 ? parts[1].padRight(2, '0') : '00';
    return int.tryParse('${parts[0]}$decimals');
  }

  /// Feninzi kao decimalni broj za API (`12050` → `120.5`).
  static double fromCents(int cents) => cents / centsPerUnit;

  static String _digits(int cents) => _number
      .format(fromCents(cents))
      // en_US daje 1,245.00, a kod nas je 1.245,00
      .replaceAll(',', ' ')
      .replaceAll('.', ',')
      .replaceAll(' ', '.');

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
