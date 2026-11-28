import 'package:intl/intl.dart';

/// Datumi kao u dizajnu: `05.05.2026`, `14:22`, `05.05.2026 · 14:22`.
///
/// API vraća UTC, a korisnik čita svoje vrijeme, pa se sve formatira u lokalnoj zoni uređaja.
class DateFormatter {
  DateFormatter._();

  static final DateFormat _date = DateFormat('dd.MM.yyyy');
  static final DateFormat _time = DateFormat('HH:mm');
  static final DateFormat _cardExpiry = DateFormat('MM/yy');
  static final DateFormat _iso = DateFormat('yyyy-MM-dd');

  static String date(DateTime value) => _date.format(value.toLocal());

  static String time(DateTime value) => _time.format(value.toLocal());

  static String dateTime(DateTime value) => '${date(value)} · ${time(value)}';

  /// Zaglavlje dana u historiji: "Danas · 05.05.2026", "Jučer · 04.05.2026" ili samo datum. Dan je lokalni.
  /// `now` je za test, a inače je sada.
  static String dayHeader(DateTime value, {DateTime? now}) {
    final local = value.toLocal();
    final clock = now ?? DateTime.now();
    final day = DateTime(local.year, local.month, local.day);
    final today = DateTime(clock.year, clock.month, clock.day);
    // DateTime sam preračuna dan 0 u zadnji dan prethodnog mjeseca
    final yesterday = DateTime(clock.year, clock.month, clock.day - 1);
    final text = date(local);
    if (day == today) {
      return 'Danas · $text';
    }
    if (day == yesterday) {
      return 'Jučer · $text';
    }
    return text;
  }

  /// Kalendarski datum za API (`2026-05-05`). Izbor datuma daje lokalnu ponoć, pa se zona ne mijenja.
  static String isoDate(DateTime value) => _iso.format(value);

  /// Rok važenja kartice (`05/28`). To je kalendarski datum bez vremena, pa se zona ne mijenja.
  static String cardExpiry(DateTime value) => _cardExpiry.format(value);
}
