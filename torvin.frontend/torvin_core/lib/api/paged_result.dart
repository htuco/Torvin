/// Jedna stranica liste, isti oblik kao `PagedResult<T>` na backendu.
class PagedResult<T> {
  /// Najveća stranica koju server daje, isto kao `BaseSearchObject.MaxPageSize`.
  static const int maxPageSize = 100;

  const PagedResult({required this.items, required this.totalCount, required this.page, required this.pageSize});

  factory PagedResult.fromJson(Map<String, dynamic> json, T Function(Map<String, dynamic> json) itemFromJson) =>
      PagedResult(
        items: [for (final item in json['items'] as List) itemFromJson(item as Map<String, dynamic>)],
        totalCount: json['totalCount'] as int,
        page: json['page'] as int,
        pageSize: json['pageSize'] as int,
      );

  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;

  bool get hasMore => page * pageSize < totalCount;
}
