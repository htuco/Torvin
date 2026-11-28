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

/// Otvara skener i vraća pročitan tekst, ili `null` kad korisnik izađe bez koda. Kroz provider je da bi ga test
/// zamijenio, jer se kamera u testu ne može pokrenuti.
final scanCodeProvider = Provider<Future<String?> Function(BuildContext context)>(
  (ref) => (context) => context.push<String>(AppRoutes.scanCode),
);
