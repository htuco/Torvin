import 'package:torvin_core/torvin_core.dart';

/// Pozivi `/transactions` prijavljenog korisnika: slanje novca i historija.
class TransactionsRepository {
  const TransactionsRepository(this._api);

  final ApiClient _api;

  /// `cardId` je kartica prijavljenog korisnika. Tuđa kartica daje praznu listu, jer server filtrira po vlasniku.
  Future<PagedResult<TransactionResponse>> getPage({
    required int page,
    int? cardId,
    TransactionDirection? direction,
  }) async {
    final json = await _api.get('/transactions', query: {
      'page': page,
      'pageSize': PagedNotifier.pageSize,
      'cardId': cardId,
      'direction': direction?.apiName,
    });
    return PagedResult.fromJson(json!, TransactionResponse.fromJson);
  }

  /// Prenos s moje kartice drugom korisniku, u valuti kartice. Iznos ide kao decimalni broj iz feninga, bez
  /// računa u `double`. Primaočevu karticu bira server.
  Future<TransactionResponse> create({
    required int fromCardId,
    required int toUserId,
    required int amountCents,
    required String? note,
  }) async {
    final json = await _api.post('/transactions', body: {
      'fromCardId': fromCardId,
      'toUserId': toUserId,
      'amount': MoneyFormatter.fromCents(amountCents),
      'note': note,
    });
    return TransactionResponse.fromJson(json!);
  }
}
