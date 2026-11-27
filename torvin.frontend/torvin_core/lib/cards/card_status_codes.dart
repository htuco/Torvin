/// Stalni kodovi statusa kartice, isti kao `CardStatusCodes` na backendu. Naziv statusa bira admin, a kod se
/// ne mijenja.
abstract final class CardStatusCodes {
  static const String pending = 'Pending';
  static const String active = 'Active';
  static const String blocked = 'Blocked';
  static const String lost = 'Lost';
  static const String stolen = 'Stolen';
  static const String closed = 'Closed';
}
