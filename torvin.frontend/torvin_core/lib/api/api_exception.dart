/// Greška API poziva u obliku koji UI prikazuje: opšta poruka i poruke po polju forme.
class ApiException implements Exception {
  const ApiException({
    required this.statusCode,
    required this.message,
    this.fieldErrors = const {},
  });

  /// HTTP status, ili 0 kad server nije odgovorio (nema mreže, istek vremena).
  final int statusCode;
  final String message;

  /// Ključevi su camelCase nazivi polja iz requesta, isti kao u `errors` tijela greške.
  final Map<String, List<String>> fieldErrors;

  String? fieldError(String field) {
    final messages = fieldErrors[field];
    return messages == null || messages.isEmpty ? null : messages.first;
  }

  @override
  String toString() => 'ApiException($statusCode): $message';
}
