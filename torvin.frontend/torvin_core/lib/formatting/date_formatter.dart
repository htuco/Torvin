import 'package:intl/intl.dart';

/// Datumi kao u dizajnu: `05.05.2026`, `14:22`, `05.05.2026 · 14:22`.
///
/// API vraća UTC, a korisnik čita svoje vrijeme, pa se sve formatira u lokalnoj zoni uređaja.
class DateFormatter {
  DateFormatter._();

  static final DateFormat _date = DateFormat('dd.MM.yyyy');
  static final DateFormat _time = DateFormat('HH:mm');
  static final DateFormat _cardExpiry = DateFormat('MM/yy');

  static String date(DateTime value) => _date.format(value.toLocal());

  static String time(DateTime value) => _time.format(value.toLocal());

  static String dateTime(DateTime value) => '${date(value)} · ${time(value)}';

  /// Rok važenja kartice (`05/28`). To je kalendarski datum bez vremena, pa se zona ne mijenja.
  static String cardExpiry(DateTime value) => _cardExpiry.format(value);
}
