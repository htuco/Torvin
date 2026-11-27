import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/paged_result.dart';
import '../auth/auth_providers.dart';
import 'reference_models.dart';

/// Čitanje šifarnika za padajuće liste i filtere. Šifarnici su kratki, pa jedna najveća stranica donosi sve.
class ReferenceRepository {
  const ReferenceRepository(this._api);

  static const int _allItems = 100;

  final ApiClient _api;

  Future<List<CardTypeResponse>> cardTypes() => _all('/card-types', CardTypeResponse.fromJson);

  Future<List<CardStatusResponse>> cardStatuses() => _all('/card-statuses', CardStatusResponse.fromJson);

  Future<List<CurrencyResponse>> currencies() => _all('/currencies', CurrencyResponse.fromJson);

  Future<List<T>> _all<T>(String path, T Function(Map<String, dynamic> json) fromJson) async {
    final json = await _api.get(path, query: {'pageSize': _allItems});
    return PagedResult.fromJson(json!, fromJson).items;
  }
}

final referenceRepositoryProvider =
    Provider<ReferenceRepository>((ref) => ReferenceRepository(ref.watch(apiClientProvider)));
