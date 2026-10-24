/// Konfiguracija koja se zadaje pri pokretanju aplikacije kroz `--dart-define`.
abstract final class AppConfig {
  static const String apiBaseUrl = String.fromEnvironment('API_BASE_URL');

  /// Poziva se u `main()` prije `runApp`, da se greška vidi odmah pri startu.
  static void validate() {
    if (apiBaseUrl.isEmpty) {
      throw StateError(
        'API_BASE_URL nije zadan. Pokreni aplikaciju s '
        '--dart-define=API_BASE_URL=<adresa API-ja>',
      );
    }
  }
}
