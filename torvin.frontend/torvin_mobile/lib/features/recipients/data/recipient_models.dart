import 'package:torvin_core/torvin_core.dart';

/// Korisnik iz pretrage primalaca (`RecipientResponse` na backendu). Email se ne vraća.
class RecipientResponse {
  const RecipientResponse({
    required this.userId,
    required this.firstName,
    required this.lastName,
    required this.username,
  });

  factory RecipientResponse.fromJson(Map<String, dynamic> json) => RecipientResponse(
        userId: json['userId'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        username: json['username'] as String,
      );

  final int userId;
  final String firstName;
  final String lastName;
  final String username;
}

/// Sačuvan primalac prijavljenog korisnika (`FavoriteRecipientResponse` na backendu).
class FavoriteRecipientResponse {
  const FavoriteRecipientResponse({
    required this.id,
    required this.recipientUserId,
    required this.firstName,
    required this.lastName,
    required this.username,
    required this.nickname,
  });

  factory FavoriteRecipientResponse.fromJson(Map<String, dynamic> json) => FavoriteRecipientResponse(
        id: json['id'] as int,
        recipientUserId: json['recipientUserId'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        username: json['username'] as String,
        nickname: json['nickname'] as String?,
      );

  final int id;
  final int recipientUserId;
  final String firstName;
  final String lastName;
  final String username;
  final String? nickname;

  String get fullName => '$firstName $lastName';

  /// Nadimak kad postoji, inače ime i prezime.
  String get displayName => nickname ?? fullName;
}

/// Primalac iz skeniranog QR koda (`ReceiveCodeRecipientResponse` na backendu). Kartica je samo maskirana.
class ReceiveCodeRecipientResponse {
  const ReceiveCodeRecipientResponse({
    required this.userId,
    required this.firstName,
    required this.lastName,
    required this.username,
    required this.network,
    required this.last4Digits,
    required this.currencyCode,
    required this.amount,
  });

  factory ReceiveCodeRecipientResponse.fromJson(Map<String, dynamic> json) => ReceiveCodeRecipientResponse(
        userId: json['userId'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        username: json['username'] as String,
        network: CardNetwork.fromApi(json['network'] as String),
        last4Digits: json['last4Digits'] as String,
        currencyCode: json['currencyCode'] as String,
        amount: (json['amount'] as num?)?.toDouble(),
      );

  final int userId;
  final String firstName;
  final String lastName;
  final String username;
  final CardNetwork network;
  final String last4Digits;
  final String currencyCode;

  /// Iznos koji je primalac zadao u kodu, ako ga je zadao.
  final double? amount;
}

/// Izabran primalac u formi nove transakcije, bez obzira odakle je došao (pretraga, omiljeni ili QR). Novac ide po
/// `userId`, a server bira primaočevu karticu.
class Recipient {
  const Recipient({
    required this.userId,
    required this.firstName,
    required this.lastName,
    required this.username,
    this.nickname,
    this.cardLabel,
    this.currencyCode,
  });

  factory Recipient.fromSearch(RecipientResponse recipient) => Recipient(
        userId: recipient.userId,
        firstName: recipient.firstName,
        lastName: recipient.lastName,
        username: recipient.username,
      );

  factory Recipient.fromFavorite(FavoriteRecipientResponse favorite) => Recipient(
        userId: favorite.recipientUserId,
        firstName: favorite.firstName,
        lastName: favorite.lastName,
        username: favorite.username,
        nickname: favorite.nickname,
      );

  factory Recipient.fromCode(ReceiveCodeRecipientResponse code) => Recipient(
        userId: code.userId,
        firstName: code.firstName,
        lastName: code.lastName,
        username: code.username,
        // Naziv mreže je isti kao na kartici (Visa, Mastercard)
        cardLabel: '${code.network.apiName} ${CardResponse.mask(code.last4Digits)}',
        currencyCode: code.currencyCode,
      );

  final int userId;
  final String firstName;
  final String lastName;
  final String username;
  final String? nickname;

  /// Kartica iz QR koda ("Visa •••• 1234"). Iz pretrage i omiljenih se kartica ne zna.
  final String? cardLabel;

  /// Valuta kartice iz QR koda.
  final String? currencyCode;

  String get fullName => '$firstName $lastName';
}
