import 'package:torvin_core/torvin_core.dart';

/// Razlog prijave kartice, isto kao `CardReportReason` na backendu.
enum CardReportReason {
  lost('Lost'),
  stolen('Stolen');

  const CardReportReason(this.apiName);

  final String apiName;
}

/// Kod koji mobilna prikaže kao QR. Poslije `expiresAt` treba tražiti novi.
class ReceiveCode {
  const ReceiveCode({required this.code, required this.amount, required this.expiresAt});

  factory ReceiveCode.fromJson(Map<String, dynamic> json) => ReceiveCode(
        code: json['code'] as String,
        amount: (json['amount'] as num?)?.toDouble(),
        expiresAt: DateTime.parse(json['expiresAt'] as String).toUtc(),
      );

  final String code;

  /// Iznos koji pošiljalac dobija popunjen, ako ga je primalac zadao.
  final double? amount;
  final DateTime expiresAt;
}

/// Pozivi `/cards/*` prijavljenog korisnika. Vlasnika server uzima iz tokena, pa se `userId` nikad ne šalje.
class CardsRepository {
  const CardsRepository(this._api);

  final ApiClient _api;

  /// Najnovija kartica je prva (sort na serveru).
  Future<PagedResult<CardResponse>> getPage({required int page, int? cardStatusId}) async {
    final json = await _api.get('/cards', query: {
      'page': page,
      'pageSize': PagedNotifier.pageSize,
      'cardStatusId': cardStatusId,
    });
    return PagedResult.fromJson(json!, CardResponse.fromJson);
  }

  /// Koliko kartica ima dati status. Stranica od jednog reda je dovoljna, jer se čita samo `totalCount`.
  Future<int> countByStatus(int cardStatusId) async {
    final json = await _api.get('/cards', query: {'cardStatusId': cardStatusId, 'pageSize': 1});
    return PagedResult.fromJson(json!, CardResponse.fromJson).totalCount;
  }

  Future<CardResponse> getById(int id) async => CardResponse.fromJson((await _api.get('/cards/$id'))!);

  /// Pun broj ide samo u tijelo zahtjeva. Server ga provjeri, a čuva samo zadnje 4 cifre i mrežu.
  Future<CardResponse> create({
    required int cardTypeId,
    required int currencyId,
    required String? name,
    required String cardNumber,
    required String expiry,
  }) async {
    final json = await _api.post('/cards', body: {
      'cardTypeId': cardTypeId,
      'currencyId': currencyId,
      'name': name,
      'cardNumber': cardNumber,
      'expiry': expiry,
    });
    return CardResponse.fromJson(json!);
  }

  Future<CardResponse> verify(int id, {required String code}) async =>
      CardResponse.fromJson((await _api.post('/cards/$id/verify', body: {'code': code}))!);

  Future<void> resendCode(int id) => _api.post('/cards/$id/resend-code');

  Future<CardResponse> block(int id) async => CardResponse.fromJson((await _api.post('/cards/$id/block'))!);

  Future<CardResponse> unblock(int id) async => CardResponse.fromJson((await _api.post('/cards/$id/unblock'))!);

  Future<CardResponse> report(int id, {required CardReportReason reason, String? note}) async =>
      CardResponse.fromJson((await _api.post('/cards/$id/report', body: {'reason': reason.apiName, 'note': note}))!);

  /// `amountCents` je opcioni iznos u kodu. API dobija decimalni broj (`50.0`), a bez iznosa se parametar ne šalje.
  Future<ReceiveCode> receiveCode(int id, {int? amountCents}) async {
    final amount = amountCents == null ? null : MoneyFormatter.fromCents(amountCents);
    return ReceiveCode.fromJson((await _api.get('/cards/$id/receive-code', query: {'amount': amount}))!);
  }
}
