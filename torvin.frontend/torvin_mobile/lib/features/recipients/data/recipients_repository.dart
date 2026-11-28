import 'package:torvin_core/torvin_core.dart';

import 'recipient_models.dart';

/// Pretraga primalaca, QR kod i omiljeni primaoci prijavljenog korisnika. Vlasnika server uzima iz tokena.
class RecipientsRepository {
  const RecipientsRepository(this._api);

  /// Server vraća najviše 10 rezultata pretrage (`RecipientService.MaxResults`).
  static const int searchResults = 10;

  final ApiClient _api;

  /// `fts` ima najmanje 3 znaka, inače server vraća grešku polja `fts`.
  Future<List<RecipientResponse>> search(String fts) async {
    final json = await _api.get('/recipients/search', query: {'fts': fts, 'pageSize': searchResults});
    return PagedResult.fromJson(json!, RecipientResponse.fromJson).items;
  }

  /// Provjeru potpisa, roka i primaoca radi server. Pogrešan kod daje grešku polja `code`.
  Future<ReceiveCodeRecipientResponse> resolveCode(String code) async =>
      ReceiveCodeRecipientResponse.fromJson((await _api.post('/recipients/resolve-code', body: {'code': code}))!);

  Future<PagedResult<FavoriteRecipientResponse>> favorites({
    required int page,
    String? fts,
    int pageSize = PagedNotifier.pageSize,
  }) async {
    final json = await _api.get('/favorite-recipients', query: {'page': page, 'pageSize': pageSize, 'fts': fts});
    return PagedResult.fromJson(json!, FavoriteRecipientResponse.fromJson);
  }

  /// Duplikat daje "Primalac je već među omiljenim.", a korisnik koji ne može primati novac "Primalac nije
  /// pronađen."
  Future<FavoriteRecipientResponse> addFavorite(int recipientUserId) async => FavoriteRecipientResponse.fromJson(
        (await _api.post('/favorite-recipients', body: {'recipientUserId': recipientUserId}))!,
      );

  /// Prazan nadimak (`null`) briše nadimak.
  Future<FavoriteRecipientResponse> updateFavorite(int id, {required String? nickname}) async =>
      FavoriteRecipientResponse.fromJson((await _api.put('/favorite-recipients/$id', body: {'nickname': nickname}))!);

  /// Meko brisanje na serveru. Isti primalac se kasnije može ponovo dodati.
  Future<void> deleteFavorite(int id) => _api.delete('/favorite-recipients/$id');
}
