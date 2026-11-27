/// Smjer iz pogleda prijavljenog korisnika, isto kao `TransactionDirection` na backendu.
enum TransactionDirection {
  sent('Sent'),
  received('Received');

  const TransactionDirection(this.apiName);

  final String apiName;

  static TransactionDirection fromApi(String value) => values.firstWhere(
        (direction) => direction.apiName == value,
        orElse: () => throw FormatException('Nepoznat smjer transakcije: $value'),
      );
}

/// Transakcija iz odgovora `/transactions` (`TransactionResponse` na backendu). Čitaju se polja koja ekrani prikazuju.
class TransactionResponse {
  /// Stalni kod statusa izvršene transakcije (`TransactionStatusCodes.Completed` na backendu).
  static const String completedStatusCode = 'Completed';

  const TransactionResponse({
    required this.id,
    required this.direction,
    required this.amount,
    required this.currencyCode,
    required this.transactionStatusCode,
    required this.transactionStatusName,
    required this.counterpartyFirstName,
    required this.counterpartyLastName,
    required this.createdAt,
  });

  factory TransactionResponse.fromJson(Map<String, dynamic> json) => TransactionResponse(
        id: json['id'] as int,
        direction: TransactionDirection.fromApi(json['direction'] as String),
        amount: (json['amount'] as num).toDouble(),
        currencyCode: json['currencyCode'] as String,
        transactionStatusCode: json['transactionStatusCode'] as String,
        transactionStatusName: json['transactionStatusName'] as String,
        counterpartyFirstName: json['counterpartyFirstName'] as String,
        counterpartyLastName: json['counterpartyLastName'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toUtc(),
      );

  final int id;
  final TransactionDirection direction;

  /// Uvijek pozitivan. Predznak daje smjer.
  final double amount;
  final String currencyCode;
  final String transactionStatusCode;
  final String transactionStatusName;
  final String counterpartyFirstName;
  final String counterpartyLastName;
  final DateTime createdAt;

  String get counterpartyName => '$counterpartyFirstName $counterpartyLastName';

  bool get isCompleted => transactionStatusCode == completedStatusCode;

  /// Iznos s predznakom za prikaz: poslano je odliv, primljeno priliv.
  double get signedAmount => direction == TransactionDirection.sent ? -amount : amount;
}
