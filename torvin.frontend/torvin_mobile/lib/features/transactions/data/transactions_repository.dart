import 'package:torvin_core/torvin_core.dart';

/// Pozivi `/transactions` prijavljenog korisnika. Za sada historija jedne kartice, uz filter smjera.
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
}
