/// Aplikacija iz koje se korisnik prijavljuje. Backend po njoj provjerava rolu:
/// desktop je samo za administratore, mobilna samo za korisnike.
enum ClientApp {
  desktop('Desktop'),
  mobile('Mobile');

  const ClientApp(this.apiName);

  /// Vrijednost koju API očekuje u polju `app`.
  final String apiName;
}
