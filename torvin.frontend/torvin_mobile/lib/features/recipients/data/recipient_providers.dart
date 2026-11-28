import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'recipient_models.dart';
import 'recipients_repository.dart';

final recipientsRepositoryProvider =
    Provider<RecipientsRepository>((ref) => RecipientsRepository(ref.watch(apiClientProvider)));

/// Rezultati pretrage primalaca za upisan tekst (najmanje 3 znaka).
final recipientSearchProvider = FutureProvider.autoDispose.family<List<RecipientResponse>, String>(
  (ref, fts) => ref.read(recipientsRepositoryProvider).search(fts),
  retry: noRetry,
);

/// Moji omiljeni primaoci, po prezimenu. Argument je pretraga, a prazan tekst znači sve.
class FavoriteList extends PagedNotifier<FavoriteRecipientResponse> {
  FavoriteList(this.fts);

  final String fts;

  @override
  Future<PagedResult<FavoriteRecipientResponse>> fetchPage(int page) =>
      ref.read(recipientsRepositoryProvider).favorites(page: page, fts: fts.isEmpty ? null : fts);
}

final favoriteListProvider =
    AsyncNotifierProvider.autoDispose.family<FavoriteList, PagedList<FavoriteRecipientResponse>, String>(
  FavoriteList.new,
  retry: noRetry,
);

/// Koliko omiljenih stane u red iznad pretrage u formi nove transakcije.
const int favoritesRowSize = 8;

/// Omiljeni za red u formi nove transakcije: samo onoliko koliko red prikazuje.
final favoritesRowProvider = FutureProvider.autoDispose<List<FavoriteRecipientResponse>>(
  (ref) async => (await ref.read(recipientsRepositoryProvider).favorites(page: 1, pageSize: favoritesRowSize)).items,
  retry: noRetry,
);

/// Ko je druga strana: korisnik i prezime, po kojem se traže omiljeni.
typedef FavoriteCheck = ({int userId, String lastName});

/// Da li je korisnik već među mojim omiljenim. Server ne filtrira po korisniku, pa se omiljeni traže po prezimenu
/// (najviše 100 s istim prezimenom), a dalje poredi `recipientUserId`. Duplikat i tako odbija server.
final isFavoriteProvider = FutureProvider.autoDispose.family<bool, FavoriteCheck>((ref, check) async {
  final page = await ref
      .read(recipientsRepositoryProvider)
      .favorites(page: 1, fts: check.lastName, pageSize: PagedResult.maxPageSize);
  return page.items.any((favorite) => favorite.recipientUserId == check.userId);
}, retry: noRetry);

/// Poslije dodavanja, izmjene ili brisanja omiljenog: lista (svaka pretraga), red u formi i provjera "već omiljen".
void refreshFavorites(WidgetRef ref) {
  ref.invalidate(favoriteListProvider);
  ref.invalidate(favoritesRowProvider);
  ref.invalidate(isFavoriteProvider);
}

/// Otvara skener i vraća pročitan tekst, ili `null` kad korisnik izađe bez koda. Kroz provider je da bi ga test
/// zamijenio, jer se kamera u testu ne može pokrenuti.
final scanCodeProvider = Provider<Future<String?> Function(BuildContext context)>(
  (ref) => (context) => context.push<String>(AppRoutes.scanCode),
);
