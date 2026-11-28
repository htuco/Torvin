import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../cards/data/card_providers.dart';

/// Filteri historije. `null` znači da filter nije izabran. Iznosi su u feninzima, a period su lokalni kalendarski
/// dani. Jednakost po vrijednosti je bitna, jer je filter argument providera: isti filter daje istu listu.
@immutable
class TransactionFilter {
  const TransactionFilter({
    this.fts,
    this.direction,
    this.period,
    this.cardId,
    this.amountFromCents,
    this.amountToCents,
  });

  /// Historija bez filtera.
  static const TransactionFilter none = TransactionFilter();

  final String? fts;
  final TransactionDirection? direction;
  final DateTimeRange? period;
  final int? cardId;
  final int? amountFromCents;
  final int? amountToCents;

  bool get isEmpty => this == none;

  TransactionFilter withFts(String? value) => TransactionFilter(
        fts: value,
        direction: direction,
        period: period,
        cardId: cardId,
        amountFromCents: amountFromCents,
        amountToCents: amountToCents,
      );

  TransactionFilter withDirection(TransactionDirection? value) => TransactionFilter(
        fts: fts,
        direction: value,
        period: period,
        cardId: cardId,
        amountFromCents: amountFromCents,
        amountToCents: amountToCents,
      );

  TransactionFilter withPeriod(DateTimeRange? value) => TransactionFilter(
        fts: fts,
        direction: direction,
        period: value,
        cardId: cardId,
        amountFromCents: amountFromCents,
        amountToCents: amountToCents,
      );

  TransactionFilter withCard(int? value) => TransactionFilter(
        fts: fts,
        direction: direction,
        period: period,
        cardId: value,
        amountFromCents: amountFromCents,
        amountToCents: amountToCents,
      );

  TransactionFilter withAmount(int? fromCents, int? toCents) => TransactionFilter(
        fts: fts,
        direction: direction,
        period: period,
        cardId: cardId,
        amountFromCents: fromCents,
        amountToCents: toCents,
      );

  @override
  bool operator ==(Object other) =>
      other is TransactionFilter &&
      other.fts == fts &&
      other.direction == direction &&
      other.period == period &&
      other.cardId == cardId &&
      other.amountFromCents == amountFromCents &&
      other.amountToCents == amountToCents;

  @override
  int get hashCode => Object.hash(fts, direction, period, cardId, amountFromCents, amountToCents);
}

/// Moje transakcije s filterima, najnovije prve.
class TransactionHistory extends PagedNotifier<TransactionResponse> {
  TransactionHistory(this.filter);

  final TransactionFilter filter;

  @override
  Future<PagedResult<TransactionResponse>> fetchPage(int page) => ref.read(transactionsRepositoryProvider).getPage(
        page: page,
        fts: filter.fts,
        direction: filter.direction,
        dateFrom: filter.period?.start,
        dateTo: filter.period?.end,
        cardId: filter.cardId,
        amountFromCents: filter.amountFromCents,
        amountToCents: filter.amountToCents,
      );
}

final transactionHistoryProvider =
    AsyncNotifierProvider.autoDispose.family<TransactionHistory, PagedList<TransactionResponse>, TransactionFilter>(
  TransactionHistory.new,
  retry: noRetry,
);

final transactionProvider = FutureProvider.autoDispose.family<TransactionResponse, int>(
  (ref, id) => ref.read(transactionsRepositoryProvider).getById(id),
  retry: noRetry,
);

/// Poslije slanja novca: historija (svi filteri), detalji i sve iz `refreshCards` (balansi, historija kartice).
void refreshTransactions(WidgetRef ref, {required int cardId}) {
  ref.invalidate(transactionHistoryProvider);
  ref.invalidate(transactionProvider);
  refreshCards(ref, cardId: cardId);
}
