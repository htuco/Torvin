import 'package:torvin_core/torvin_core.dart';

/// Pozivi `/transactions` prijavljenog korisnika: slanje novca i historija.
class TransactionsRepository {
  const TransactionsRepository(this._api);

  final ApiClient _api;

  /// Moje transakcije, najnovije prve. `cardId` je kartica prijavljenog korisnika (tuđa daje praznu listu, jer server
  /// filtrira po vlasniku). Datumi su kalendarski dani (uključivo), a iznosi u feninzima. Filter koji je `null` se
  /// ne šalje.
  Future<PagedResult<TransactionResponse>> getPage({
    required int page,
    int? cardId,
    TransactionDirection? direction,
    String? fts,
    DateTime? dateFrom,
    DateTime? dateTo,
    int? amountFromCents,
    int? amountToCents,
  }) async {
    final json = await _api.get('/transactions', query: {
      'page': page,
      'pageSize': PagedNotifier.pageSize,
      'cardId': cardId,
      'direction': direction?.apiName,
      'fts': fts,
      'dateFrom': dateFrom == null ? null : DateFormatter.isoDate(dateFrom),
      'dateTo': dateTo == null ? null : DateFormatter.isoDate(dateTo),
      'amountFrom': amountFromCents == null ? null : MoneyFormatter.fromCents(amountFromCents),
      'amountTo': amountToCents == null ? null : MoneyFormatter.fromCents(amountToCents),
    });
    return PagedResult.fromJson(json!, TransactionResponse.fromJson);
  }

  /// Tuđa ili nepostojeća transakcija daje 404 ("Transakcija nije pronađena.").
  Future<TransactionResponse> getById(int id) async =>
      TransactionResponse.fromJson((await _api.get('/transactions/$id'))!);

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
