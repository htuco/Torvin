import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_exception.dart';
import '../api/paged_result.dart';

/// Do sada učitani redovi liste i stanje učitavanja sljedeće stranice.
class PagedList<T> {
  const PagedList({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
    this.loadMoreError,
  });

  final List<T> items;
  final int totalCount;
  final int page;
  final bool hasMore;
  final bool loadingMore;

  /// Sljedeća stranica se nije učitala. Već učitani redovi ostaju na ekranu.
  final ApiException? loadMoreError;

  PagedList<T> copyWith({bool? loadingMore, ApiException? loadMoreError}) => PagedList(
        items: items,
        totalCount: totalCount,
        page: page,
        hasMore: hasMore,
        loadingMore: loadingMore ?? this.loadingMore,
        loadMoreError: loadMoreError,
      );
}

/// Lista sa server-side paginacijom: prva stranica pri otvaranju, a sljedeća kad korisnik dođe do kraja liste.
abstract class PagedNotifier<T> extends AsyncNotifier<PagedList<T>> {
  static const int pageSize = 20;

  Future<PagedResult<T>> fetchPage(int page);

  @override
  Future<PagedList<T>> build() async {
    final result = await fetchPage(1);
    return PagedList(items: result.items, totalCount: result.totalCount, page: 1, hasMore: result.hasMore);
  }

  /// Ne radi ništa dok prva stranica nije tu, dok sljedeća već stiže ili kad stranica više nema.
  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore || state.isLoading) {
      return;
    }

    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final result = await fetchPage(current.page + 1);
      state = AsyncData(PagedList(
        items: [...current.items, ...result.items],
        totalCount: result.totalCount,
        page: current.page + 1,
        hasMore: result.hasMore,
      ));
    } on ApiException catch (error) {
      state = AsyncData(current.copyWith(loadingMore: false, loadMoreError: error));
    }
  }
}
