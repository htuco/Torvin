import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/details_panel.dart';
import '../../../app/widgets/initials_avatar.dart';
import '../../../app/widgets/load_more_listener.dart';
import '../../../app/widgets/paged_list_footer.dart';
import '../../../app/widgets/search_field.dart';
import '../data/recipient_models.dart';
import '../data/recipient_providers.dart';
import 'nickname_dialog.dart';

/// Moji omiljeni primaoci s pretragom: dodir šalje novac, a nadimak se mijenja i primalac uklanja iz reda.
/// Novi omiljeni se dodaje iz detalja transakcije.
class FavoritesScreen extends ConsumerStatefulWidget {
  const FavoritesScreen({super.key});

  @override
  ConsumerState<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends ConsumerState<FavoritesScreen> {
  String _fts = '';

  /// Dok izmjena ili brisanje traje, dugmad reda su onemogućena, da dvostruki dodir ne pošalje dva zahtjeva.
  bool _busy = false;

  /// Uspjeh ili poruka backenda (greška polja `field` kad je ima), pa osvježavanje i kad akcija ne uspije (zapis se
  /// mogao promijeniti).
  Future<void> _run(Future<void> Function() action, String success, {String? field}) async {
    setState(() => _busy = true);
    try {
      await action();
      if (mounted) {
        showTorvinSnackBar(context, success);
      }
    } on ApiException catch (error) {
      if (mounted) {
        showTorvinSnackBar(context, (field == null ? null : error.fieldError(field)) ?? error.message);
      }
    } finally {
      if (mounted) {
        refreshFavorites(ref);
        setState(() => _busy = false);
      }
    }
  }

  Future<void> _rename(FavoriteRecipientResponse favorite) async {
    final nickname = await showNicknameDialog(context, favorite);
    // Odustajanje ili isti nadimak kao prije: nema šta poslati
    if (nickname == null || nickname == (favorite.nickname ?? '')) {
      return;
    }
    await _run(
      () => ref
          .read(recipientsRepositoryProvider)
          .updateFavorite(favorite.id, nickname: nickname.isEmpty ? null : nickname),
      nickname.isEmpty ? 'Nadimak je uklonjen.' : 'Nadimak je sačuvan.',
      field: 'nickname',
    );
  }

  Future<void> _remove(FavoriteRecipientResponse favorite) async {
    final confirmed = await showConfirmDialog(
      context,
      icon: LucideIcons.trash2,
      title: 'Ukloniti iz omiljenih?',
      message: '${favorite.fullName} više neće biti na listi omiljenih. Novac i dalje možete slati pretragom.',
      confirmLabel: 'Ukloni',
    );
    if (confirmed) {
      await _run(
        () => ref.read(recipientsRepositoryProvider).deleteFavorite(favorite.id),
        'Uklonjeno iz omiljenih: ${favorite.fullName}.',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final favorites = ref.watch(favoriteListProvider(_fts));
    return Scaffold(
      appBar: AppBar(title: const Text('Omiljeni primaoci')),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppConstants.spacingLG,
              0,
              AppConstants.spacingLG,
              AppConstants.spacingMD,
            ),
            child: SearchField(
              label: 'Pretraga',
              hint: 'Nadimak, ime ili korisničko ime',
              onSearch: (text) => setState(() => _fts = text),
            ),
          ),
          Expanded(
            child: favorites.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ErrorState(
                title: 'Omiljeni se nisu učitali',
                message: ApiException.messageOf(error),
                onRetry: () => ref.invalidate(favoriteListProvider(_fts)),
              ),
              data: (page) => page.items.isEmpty ? _empty() : _list(page),
            ),
          ),
        ],
      ),
    );
  }

  Widget _empty() => _fts.isEmpty
      ? const EmptyState(
          icon: LucideIcons.star,
          title: 'Nemate omiljenih primalaca',
          message: 'Primaoca dodajete iz detalja transakcije.',
        )
      : EmptyState(
          icon: LucideIcons.searchX,
          title: 'Nema rezultata',
          message: 'Nema omiljenih primalaca za „$_fts”.',
        );

  Widget _list(PagedList<FavoriteRecipientResponse> page) {
    return LoadMoreListener(
      onLoadMore: () => ref.read(favoriteListProvider(_fts).notifier).loadMore(),
      child: ListView(
        padding: const EdgeInsets.fromLTRB(AppConstants.spacingLG, 0, AppConstants.spacingLG, AppConstants.spacingLG),
        children: [
          DetailsPanel(
            children: [
              for (final favorite in page.items)
                _FavoriteRow(
                  favorite: favorite,
                  busy: _busy,
                  onSend: () => context.push(AppRoutes.newTransaction, extra: Recipient.fromFavorite(favorite)),
                  onRename: () => _rename(favorite),
                  onRemove: () => _remove(favorite),
                ),
            ],
          ),
          PagedListFooter(page: page, onRetry: () => ref.read(favoriteListProvider(_fts).notifier).loadMore()),
        ],
      ),
    );
  }
}

class _FavoriteRow extends StatelessWidget {
  const _FavoriteRow({
    required this.favorite,
    required this.busy,
    required this.onSend,
    required this.onRename,
    required this.onRemove,
  });

  final FavoriteRecipientResponse favorite;
  final bool busy;
  final VoidCallback onSend;
  final VoidCallback onRename;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final username = '@${favorite.username}';
    final subtitle = favorite.nickname == null ? username : '${favorite.fullName} · $username';

    return InkWell(
      onTap: onSend,
      child: Padding(
        // Desno nema razmaka, jer dugmad ikona već imaju svoj
        padding: const EdgeInsets.only(
          left: AppConstants.spacingMD,
          top: AppConstants.spacingSM,
          bottom: AppConstants.spacingSM,
        ),
        child: Row(
          children: [
            InitialsAvatar(firstName: favorite.firstName, lastName: favorite.lastName),
            const SizedBox(width: AppConstants.spacingRow),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(favorite.displayName, style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w500)),
                  Text(subtitle, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
                ],
              ),
            ),
            IconButton(
              tooltip: 'Izmijeni nadimak',
              icon: const Icon(LucideIcons.pencil, size: AppConstants.iconSize),
              onPressed: busy ? null : onRename,
            ),
            IconButton(
              tooltip: 'Ukloni iz omiljenih',
              icon: Icon(LucideIcons.trash2, size: AppConstants.iconSize, color: theme.colorScheme.error),
              onPressed: busy ? null : onRemove,
            ),
          ],
        ),
      ),
    );
  }
}
