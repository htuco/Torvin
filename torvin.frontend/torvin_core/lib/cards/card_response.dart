/// Mreža kartice koju server prepozna iz broja pri dodavanju. Pun broj se ne čuva.
enum CardNetwork {
  visa('Visa'),
  mastercard('Mastercard');

  const CardNetwork(this.apiName);

  final String apiName;

  static CardNetwork fromApi(String value) => values.firstWhere(
        (network) => network.apiName == value,
        orElse: () => throw FormatException('Nepoznata mreža kartice: $value'),
      );
}

/// Kartica iz odgovora `/cards` (`CardResponse` na backendu). Čitaju se polja koja ekrani prikazuju, a od broja
/// postoje samo zadnje 4 cifre.
class CardResponse {
  const CardResponse({
    required this.id,
    required this.name,
    required this.cardTypeId,
    required this.cardTypeName,
    required this.cardStatusCode,
    required this.cardStatusName,
    required this.currencyCode,
    required this.network,
    required this.last4Digits,
    required this.expiresAt,
    required this.balance,
    required this.isDefault,
    required this.createdAt,
    required this.userFirstName,
    required this.userLastName,
    required this.isBlockedByAdmin,
  });

  factory CardResponse.fromJson(Map<String, dynamic> json) => CardResponse(
        id: json['id'] as int,
        name: json['name'] as String?,
        cardTypeId: json['cardTypeId'] as int,
        cardTypeName: json['cardTypeName'] as String,
        cardStatusCode: json['cardStatusCode'] as String,
        cardStatusName: json['cardStatusName'] as String,
        currencyCode: json['currencyCode'] as String,
        network: CardNetwork.fromApi(json['network'] as String),
        last4Digits: json['last4Digits'] as String,
        // Backend šalje samo datum (DateOnly, "2029-08-31"): zadnji dan mjeseca do kojeg kartica važi
        expiresAt: DateTime.parse(json['expiresAt'] as String),
        balance: (json['balance'] as num).toDouble(),
        isDefault: json['isDefault'] as bool,
        createdAt: DateTime.parse(json['createdAt'] as String).toUtc(),
        userFirstName: json['userFirstName'] as String,
        userLastName: json['userLastName'] as String,
        isBlockedByAdmin: json['isBlockedByAdmin'] as bool,
      );

  final int id;
  final String? name;
  final int cardTypeId;
  final String cardTypeName;
  final String cardStatusCode;
  final String cardStatusName;
  final String currencyCode;
  final CardNetwork network;
  final String last4Digits;
  final DateTime expiresAt;
  final double balance;

  /// Kartica na koju stiže novac poslan korisniku.
  final bool isDefault;
  final DateTime createdAt;
  final String userFirstName;
  final String userLastName;

  /// Blokadu je postavio administrator, pa je vlasnik ne može sam poništiti.
  final bool isBlockedByAdmin;

  String get holderName => '$userFirstName $userLastName';

  /// Naziv koji je korisnik dao kartici, a bez njega naziv tipa.
  String get displayName => name ?? cardTypeName;

  /// `•••• 4821`, kako se kartica spominje u porukama.
  String get maskedNumber => '•••• $last4Digits';
}
