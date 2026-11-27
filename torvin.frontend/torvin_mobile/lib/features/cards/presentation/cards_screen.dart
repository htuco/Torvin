import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/filter_chips.dart';
import '../../../app/widgets/paged_list_footer.dart';
import '../data/card_providers.dart';
import 'virtual_card_view.dart';

/// Lista mojih kartica s filterom po statusu, praznim stanjem i greškom.
class CardsScreen extends ConsumerStatefulWidget {
  const CardsScreen({super.key});

  @override
  ConsumerState<CardsScreen> createState() => _CardsScreenState();
}

class _CardsScreenState extends ConsumerState<CardsScreen> {
  /// ID statusa iz filtera, a `null` je "Sve".
  int? _statusId;

  void _addCard() => context.push(AppRoutes.newCard);

  @override
  Widget build(BuildContext context) {
    final cards = ref.watch(cardListProvider(_statusId));
    // Dok broj otvorenih kartica nije poznat, dugme je dostupno, a granicu i dalje čuva server
    final limitReached = (ref.watch(openCardCountProvider).value ?? 0) >= maxOpenCards;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Kartice'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: AppConstants.spacingMD),
            child: _NewCardButton(onPressed: limitReached ? null : _addCard),
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (limitReached)
            const Padding(
              padding: EdgeInsets.fromLTRB(AppConstants.spacingLG, 0, AppConstants.spacingLG, AppConstants.spacingRow),
              child: InfoNote(tooManyCardsMessage),
            ),
          _StatusFilter(selected: _statusId, onSelected: (id) => setState(() => _statusId = id)),
          const SizedBox(height: AppConstants.spacingMD),
          Expanded(
            child: cards.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ErrorState(
                title: 'Kartice se nisu učitale',
                message: ApiException.messageOf(error),
                onRetry: () => ref.invalidate(cardListProvider(_statusId)),
              ),
              data: (page) => page.items.isEmpty ? _empty() : _list(page),
            ),
          ),
        ],
      ),
    );
  }

  Widget _empty() {
    if (_statusId != null) {
      return EmptyState(
        icon: LucideIcons.searchX,
        title: 'Nema kartica s ovim statusom',
        message: 'Izaberite drugi status ili prikažite sve kartice.',
        actionLabel: 'Prikaži sve',
        onAction: () => setState(() => _statusId = null),
      );
    }

    return EmptyState(
      icon: LucideIcons.creditCard,
      title: 'Nemate nijednu karticu',
      message: 'Dodajte virtuelnu karticu za primanje i slanje novca. Možete imati do $maxOpenCards kartica.',
      actionLabel: 'Dodaj virtuelnu karticu',
      onAction: _addCard,
    );
  }

  Widget _list(PagedList<CardResponse> page) {
    final theme = Theme.of(context);

    return NotificationListener<ScrollNotification>(
      onNotification: (notification) {
        if (notification.metrics.extentAfter < PagedListFooter.loadMoreThreshold) {
          ref.read(cardListProvider(_statusId).notifier).loadMore();
        }
        return false;
      },
      child: ListView(
        padding: const EdgeInsets.fromLTRB(AppConstants.spacingLG, 0, AppConstants.spacingLG, AppConstants.spacingLG),
        children: [
          Text(
            _countLabel(page.totalCount),
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          for (final card in page.items) ...[
            const SizedBox(height: AppConstants.spacingMD),
            InkWell(
              onTap: () => context.push(AppRoutes.cardDetails(card.id)),
              borderRadius: BorderRadius.circular(AppConstants.radiusCard),
              child: VirtualCardView(card: card, showName: true),
            ),
          ],
          PagedListFooter(
            page: page,
            onRetry: () => ref.read(cardListProvider(_statusId).notifier).loadMore(),
          ),
        ],
      ),
    );
  }

  /// "1 kartica", "3 kartice", "5 kartica" (i 11–14 kartica).
  static String _countLabel(int count) {
    final lastTwo = count % 100;
    final last = count % 10;
    final few = last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14);
    return '$count ${few ? 'kartice' : 'kartica'}';
  }
}

/// "Nova": tint primarne s plusom. Kad je granica kartica dostignuta, dugme je onemogućeno, a razlog stoji
/// ispod zaglavlja.
class _NewCardButton extends StatelessWidget {
  const _NewCardButton({required this.onPressed});

  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return TextButton.icon(
      onPressed: onPressed,
      icon: const Icon(LucideIcons.plus, size: AppConstants.iconSize),
      label: const Text('Nova'),
      style: ButtonStyle(
        minimumSize: const WidgetStatePropertyAll(Size(0, AppConstants.compactButtonHeight)),
        padding: const WidgetStatePropertyAll(EdgeInsets.symmetric(horizontal: AppConstants.compactButtonPadding)),
        backgroundColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.disabled) ? scheme.surfaceContainerHigh : scheme.primaryContainer,
        ),
        foregroundColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.disabled) ? scheme.onSurfaceVariant : scheme.onPrimaryContainer,
        ),
      ),
    );
  }
}

/// Čipovi "Sve" i statusi iz baze. Dok statusi stižu (ili ako ne stignu) stoji samo "Sve", a lista radi i
/// bez filtera.
class _StatusFilter extends ConsumerWidget {
  const _StatusFilter({required this.selected, required this.onSelected});

  final int? selected;
  final ValueChanged<int?> onSelected;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final statuses = ref.watch(cardStatusesProvider).value ?? const <CardStatusResponse>[];
    return FilterChips<int?>(
      options: [('Sve', null), for (final status in statuses) (status.name, status.id)],
      selected: selected,
      onSelected: onSelected,
      padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingLG),
    );
  }
}
