/// Tip kartice iz šifarnika ("Debitna", "Prepaid").
class CardTypeResponse {
  const CardTypeResponse({required this.id, required this.name});

  factory CardTypeResponse.fromJson(Map<String, dynamic> json) =>
      CardTypeResponse(id: json['id'] as int, name: json['name'] as String);

  final int id;
  final String name;
}

/// Status kartice iz šifarnika. Naziv mijenja admin, a logika se veže za `code`.
class CardStatusResponse {
  const CardStatusResponse({required this.id, required this.code, required this.name});

  factory CardStatusResponse.fromJson(Map<String, dynamic> json) =>
      CardStatusResponse(id: json['id'] as int, code: json['code'] as String, name: json['name'] as String);

  final int id;
  final String code;
  final String name;
}

/// Valuta iz šifarnika ("BAM", "Konvertibilna marka").
class CurrencyResponse {
  const CurrencyResponse({required this.id, required this.code, required this.name});

  factory CurrencyResponse.fromJson(Map<String, dynamic> json) =>
      CurrencyResponse(id: json['id'] as int, code: json['code'] as String, name: json['name'] as String);

  final int id;
  final String code;
  final String name;
}
