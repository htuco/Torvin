import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../transactions/data/transactions_repository.dart';
import 'cards_repository.dart';

final cardsRepositoryProvider = Provider<CardsRepository>((ref) => CardsRepository(ref.watch(apiClientProvider)));

final transactionsRepositoryProvider =
    Provider<TransactionsRepository>((ref) => TransactionsRepository(ref.watch(apiClientProvider)));

// Podaci prijavljenog korisnika su `autoDispose`: kad nijedan ekran ne prikazuje karticu (npr. poslije odjave),
// stanje se odbaci, pa sljedeći korisnik na istom uređaju nikad ne vidi tuđe kartice iz keša

/// Moje kartice, najnovija prva. Argument je ID statusa iz filtera, a `null` znači sve.
class CardList extends PagedNotifier<CardResponse> {
  CardList(this.cardStatusId);

  final int? cardStatusId;

  @override
  Future<PagedResult<CardResponse>> fetchPage(int page) =>
      ref.read(cardsRepositoryProvider).getPage(page: page, cardStatusId: cardStatusId);
}

final cardListProvider = AsyncNotifierProvider.autoDispose.family<CardList, PagedList<CardResponse>, int?>(
  CardList.new,
  retry: noRetry,
);

final cardProvider = FutureProvider.autoDispose.family<CardResponse, int>(
  (ref, id) => ref.read(cardsRepositoryProvider).getById(id),
  retry: noRetry,
);

/// Koja historija: kartica i smjer iz filtera (`null` je sve).
typedef CardHistory = ({int cardId, TransactionDirection? direction});

/// Historija jedne kartice: transakcije u kojima je ta kartica moja strana.
class CardTransactions extends PagedNotifier<TransactionResponse> {
  CardTransactions(this.history);

  final CardHistory history;

  @override
  Future<PagedResult<TransactionResponse>> fetchPage(int page) => ref
      .read(transactionsRepositoryProvider)
      .getPage(page: page, cardId: history.cardId, direction: history.direction);
}

final cardTransactionsProvider =
    AsyncNotifierProvider.autoDispose.family<CardTransactions, PagedList<TransactionResponse>, CardHistory>(
  CardTransactions.new,
  retry: noRetry,
);

final cardStatusesProvider = FutureProvider<List<CardStatusResponse>>(
  (ref) => ref.read(referenceRepositoryProvider).cardStatuses(),
  retry: noRetry,
);

final cardTypesProvider = FutureProvider<List<CardTypeResponse>>(
  (ref) => ref.read(referenceRepositoryProvider).cardTypes(),
  retry: noRetry,
);

final currenciesProvider = FutureProvider<List<CurrencyResponse>>(
  (ref) => ref.read(referenceRepositoryProvider).currencies(),
  retry: noRetry,
);

/// Koji kod: kartica i opcioni iznos u feninzima.
typedef ReceiveCodeRequest = ({int cardId, int? amountCents});

/// Kod za QR. Pri svakom otvaranju ekrana se pravi novi, jer stari ističe za 15 minuta.
final receiveCodeProvider = FutureProvider.autoDispose.family<ReceiveCode, ReceiveCodeRequest>(
  (ref, request) =>
      ref.read(cardsRepositoryProvider).receiveCode(request.cardId, amountCents: request.amountCents),
  retry: noRetry,
);

/// Moje aktivne kartice, jedine s kojih se može slati novac. Otvorenih kartica je najviše 5, pa je dovoljna prva
/// stranica.
final activeCardsProvider = FutureProvider.autoDispose<List<CardResponse>>((ref) async {
  final statuses = await ref.watch(cardStatusesProvider.future);
  final active = statuses.firstWhere((status) => status.code == CardStatusCodes.active);
  final page = await ref.read(cardsRepositoryProvider).getPage(page: 1, cardStatusId: active.id);
  return page.items;
}, retry: noRetry);

/// Najviše kartica koje nisu zatvorene ni prijavljene. Isto kao `CardService.MaxOpenCards` na backendu, koji ovo i
/// provjerava.
const int maxOpenCards = 5;
const String tooManyCardsMessage = 'Možete imati najviše $maxOpenCards kartica koje nisu zatvorene ni prijavljene.';

/// Broj kartica na čekanju, aktivnih i blokiranih, iz tri paralelna poziva (`totalCount` svakog statusa).
final openCardCountProvider = FutureProvider.autoDispose<int>((ref) async {
  final statuses = await ref.watch(cardStatusesProvider.future);
  const open = {CardStatusCodes.pending, CardStatusCodes.active, CardStatusCodes.blocked};
  final repository = ref.read(cardsRepositoryProvider);
  final counts = await Future.wait([
    for (final status in statuses.where((s) => open.contains(s.code))) repository.countByStatus(status.id),
  ]);
  return counts.fold<int>(0, (sum, count) => sum + count);
}, retry: noRetry);

/// Poslije svake promjene kartice ili balansa: lista (svi filteri), aktivne kartice, broj otvorenih i, kad je data,
/// ta kartica s historijom.
void refreshCards(WidgetRef ref, {int? cardId}) {
  ref.invalidate(cardListProvider);
  ref.invalidate(activeCardsProvider);
  ref.invalidate(openCardCountProvider);
  if (cardId != null) {
    ref.invalidate(cardProvider(cardId));
    // Historija se osvježi za svaki filter smjera
    ref.invalidate(cardTransactionsProvider);
  }
}
