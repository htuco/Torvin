/// Central route names used across the app.
abstract final class AppRoutes {
  static const String root = '/';
  static const String login = '/login';
  static const String dashboard = '/dashboard';

  /// Rute dostupne bez prijave. Desktop nema registraciju: administratore dodaje sistem.
  static const Set<String> publicRoutes = {login};
}
