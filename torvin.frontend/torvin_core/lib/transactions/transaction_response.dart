import 'transaction_status_codes.dart';

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
  const TransactionResponse({
    required this.id,
    required this.referenceNumber,
    required this.direction,
    required this.amount,
    required this.currencyCode,
    required this.transactionTypeName,
    required this.transactionStatusCode,
    required this.transactionStatusName,
    required this.transactionCategoryName,
    required this.note,
    required this.myCardLast4Digits,
    required this.counterpartyUserId,
    required this.counterpartyFirstName,
    required this.counterpartyLastName,
    required this.counterpartyCardLast4Digits,
    required this.createdAt,
  });

  factory TransactionResponse.fromJson(Map<String, dynamic> json) => TransactionResponse(
        id: json['id'] as int,
        referenceNumber: json['referenceNumber'] as String,
        direction: TransactionDirection.fromApi(json['direction'] as String),
        amount: (json['amount'] as num).toDouble(),
        currencyCode: json['currencyCode'] as String,
        transactionTypeName: json['transactionTypeName'] as String,
        transactionStatusCode: json['transactionStatusCode'] as String,
        transactionStatusName: json['transactionStatusName'] as String,
        transactionCategoryName: json['transactionCategoryName'] as String?,
        note: json['note'] as String?,
        myCardLast4Digits: json['myCardLast4Digits'] as String?,
        counterpartyUserId: json['counterpartyUserId'] as int,
        counterpartyFirstName: json['counterpartyFirstName'] as String,
        counterpartyLastName: json['counterpartyLastName'] as String,
        counterpartyCardLast4Digits: json['counterpartyCardLast4Digits'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String).toUtc(),
      );

  final int id;

  /// Broj za korisnika i podršku (`TRV-…`). ID se ne prikazuje.
  final String referenceNumber;
  final TransactionDirection direction;

  /// Uvijek pozitivan. Predznak daje smjer.
  final double amount;
  final String currencyCode;
  final String transactionTypeName;
  final String transactionStatusCode;
  final String transactionStatusName;
  final String? transactionCategoryName;
  final String? note;

  /// Moja kartica u ovoj transakciji (zadnje 4 cifre). Nema je kad kartica više ne postoji.
  final String? myCardLast4Digits;

  /// Druga strana, za dodavanje u omiljene primaoce. Ne prikazuje se.
  final int counterpartyUserId;
  final String counterpartyFirstName;
  final String counterpartyLastName;
  final String? counterpartyCardLast4Digits;
  final DateTime createdAt;

  String get counterpartyName => '$counterpartyFirstName $counterpartyLastName';

  bool get isCompleted => transactionStatusCode == TransactionStatusCodes.completed;

  /// Iznos s predznakom za prikaz: poslano je odliv, primljeno priliv.
  double get signedAmount => direction == TransactionDirection.sent ? -amount : amount;
}
