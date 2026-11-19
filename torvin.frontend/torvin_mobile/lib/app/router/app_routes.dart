/// Central route names used across the app.
abstract final class AppRoutes {
  static const String root = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String verifyEmail = '/verify-email';
  static const String forgotPassword = '/forgot-password';
  static const String resetPassword = '/reset-password';
  static const String dashboard = '/dashboard';

  /// Rute dostupne bez prijave: cijeli tok prijave, registracije i reseta lozinke.
  static const Set<String> publicRoutes = {login, register, verifyEmail, forgotPassword, resetPassword};
}

/// Šta korak s kodom (potvrda emaila, reset lozinke) dobija od prethodnog ekrana.
class EmailStep {
  const EmailStep({this.email, this.notice});

  final String? email;
  final String? notice;
}
