import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

/// Lista brojeva po stranicama od dva reda, ukupno pet. `failPage` glumi stranicu koja ne stigne.
class _Numbers extends PagedNotifier<int> {
  _Numbers(this.requested, {this.failPage});

  final List<int> requested;
  final int? failPage;

  @override
  Future<PagedResult<int>> fetchPage(int page) async {
    requested.add(page);
    if (page == failPage) {
      throw const ApiException(statusCode: 0, message: ApiClient.unreachableMessage);
    }
    final items = [for (var n = (page - 1) * 2 + 1; n <= page * 2 && n <= 5; n++) n];
    return PagedResult(items: items, totalCount: 5, page: page, pageSize: 2);
  }
}

void main() {
  test('sljedeće stranice se dodaju na kraj, a poslije zadnje se ništa ne traži', () async {
    final requested = <int>[];
    final provider = AsyncNotifierProvider<_Numbers, PagedList<int>>(() => _Numbers(requested));
    final container = ProviderContainer();
    addTearDown(container.dispose);

    expect((await container.read(provider.future)).items, [1, 2]);
    await container.read(provider.notifier).loadMore();
    await container.read(provider.notifier).loadMore();
    await container.read(provider.notifier).loadMore();

    final list = container.read(provider).value!;
    expect(list.items, [1, 2, 3, 4, 5]);
    expect(list.hasMore, isFalse);
    expect(requested, [1, 2, 3]);
  });

  test('greška sljedeće stranice čuva već učitane redove, a ponovni pokušaj nastavlja', () async {
    final requested = <int>[];
    final provider = AsyncNotifierProvider<_Numbers, PagedList<int>>(() => _Numbers(requested, failPage: 2));
    final container = ProviderContainer();
    addTearDown(container.dispose);

    await container.read(provider.future);
    await container.read(provider.notifier).loadMore();

    final list = container.read(provider).value!;
    expect(list.items, [1, 2]);
    expect(list.loadingMore, isFalse);
    expect(list.loadMoreError?.message, ApiClient.unreachableMessage);
    expect(list.hasMore, isTrue);
  });
}
