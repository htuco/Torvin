import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/details_panel.dart';
import '../../../app/widgets/filter_chips.dart';
import '../../../app/widgets/load_more_listener.dart';
import '../../../app/widgets/paged_list_footer.dart';
import '../../transactions/presentation/transaction_tile.dart';
import '../data/card_providers.dart';
import '../data/cards_repository.dart';
import 'card_actions.dart';
import 'report_card_dialog.dart';
import 'virtual_card_view.dart';

/// Detalji kartice, master-details s listom: kartica, akcije, podaci i historija te kartice.
class CardDetailsScreen extends ConsumerStatefulWidget {
  const CardDetailsScreen({super.key, required this.cardId});

  final int cardId;

  @override
  ConsumerState<CardDetailsScreen> createState() => _CardDetailsScreenState();
}

class _CardDetailsScreenState extends ConsumerState<CardDetailsScreen> {
  /// Dok akcija traje, pločice su onemogućene, da dvostruki dodir ne pošalje dva zahtjeva.
  bool _busy = false;

  /// Filter historije: poslano, primljeno ili sve (`null`).
  TransactionDirection? _direction;

  CardHistory get _history => (cardId: widget.cardId, direction: _direction);

  CardsRepository get _repository => ref.read(cardsRepositoryProvider);

  /// Poslije akcije se osvježe i kartica i lista, i kad akcija ne uspije, jer se stanje možda promijenilo u
  /// međuvremenu (npr. administrator je blokirao karticu).
  Future<void> _run(Future<CardResponse> Function() action, String Function(CardResponse card) success) async {
    setState(() => _busy = true);
    try {
      final card = await action();
      if (mounted) {
        showTorvinSnackBar(context, success(card));
      }
    } on ApiException catch (error) {
      if (mounted) {
        showTorvinSnackBar(context, error.message);
      }
    } finally {
      if (mounted) {
        refreshCards(ref, cardId: widget.cardId);
        setState(() => _busy = false);
      }
    }
  }

  Future<void> _block(CardResponse card) async {
    final confirmed = await showConfirmDialog(
      context,
      icon: LucideIcons.lock,
      title: 'Blokirati karticu ${card.maskedNumber}?',
      message: 'Blokirana kartica ne može slati ni primati novac. Deblokirati je možete kad god želite.',
      confirmLabel: 'Blokiraj',
    );
    if (confirmed) {
      await _run(() => _repository.block(card.id), (c) => 'Kartica ${c.maskedNumber} je blokirana.');
    }
  }

  Future<void> _unblock(CardResponse card) =>
      _run(() => _repository.unblock(card.id), (c) => 'Kartica ${c.maskedNumber} je deblokirana.');

  Future<void> _report(CardResponse card) async {
    final report = await showReportCardDialog(context, card);
    if (report == null) {
      return;
    }
    final reason = report.reason == CardReportReason.lost ? 'izgubljena' : 'ukradena';
    await _run(
      () => _repository.report(card.id, reason: report.reason, note: report.note),
      (c) => 'Kartica ${c.maskedNumber} je prijavljena kao $reason.',
    );
  }

  @override
  Widget build(BuildContext context) {
    // Kartica i historija se traže paralelno, jer obje zavise samo od ID-a iz rute
    final card = ref.watch(cardProvider(widget.cardId));
    final history = ref.watch(cardTransactionsProvider(_history));
    return Scaffold(
      appBar: AppBar(title: Text(card.value?.displayName ?? 'Kartica')),
      body: card.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorState(
          title: 'Kartica se nije učitala',
          message: ApiException.messageOf(error),
          onRetry: () => ref.invalidate(cardProvider(widget.cardId)),
        ),
        data: (card) => _details(card, history),
      ),
    );
  }

  Widget _details(CardResponse card, AsyncValue<PagedList<TransactionResponse>> history) {
    final actions = cardActionsFor(card.cardStatusCode, blockedByAdmin: card.isBlockedByAdmin);
    final reasons = {for (final state in actions.values) ?state.reason};

    VoidCallback? when(CardAction action, VoidCallback onTap) =>
        !_busy && (actions[action]?.enabled ?? false) ? onTap : null;

    final blockTile = actions.containsKey(CardAction.unblock)
        ? _ActionTile(
            icon: LucideIcons.lockOpen,
            label: 'Deblokiraj',
            onTap: when(CardAction.unblock, () => _unblock(card)),
          )
        : _ActionTile(icon: LucideIcons.lock, label: 'Blokiraj', onTap: when(CardAction.block, () => _block(card)));

    return LoadMoreListener(
      onLoadMore: () => ref.read(cardTransactionsProvider(_history).notifier).loadMore(),
      child: ListView(
        padding: const EdgeInsets.all(AppConstants.spacingLG),
        children: [
          VirtualCardView(card: card),
          if (actions.containsKey(CardAction.activate)) ...[
            const SizedBox(height: AppConstants.spacingMD),
            FilledButton.icon(
              onPressed: when(CardAction.activate, () => context.push(AppRoutes.activateCard(card.id))),
              icon: const Icon(LucideIcons.mailCheck, size: AppConstants.iconSize),
              label: const Text('Aktiviraj karticu'),
            ),
          ],
          const SizedBox(height: AppConstants.spacingMD),
          Row(
            children: [
              Expanded(child: blockTile),
              const SizedBox(width: AppConstants.spacingSM),
              Expanded(
                child: _ActionTile(
                  icon: LucideIcons.triangleAlert,
                  label: 'Prijavi',
                  destructive: true,
                  onTap: when(CardAction.report, () => _report(card)),
                ),
              ),
              const SizedBox(width: AppConstants.spacingSM),
              Expanded(
                child: _ActionTile(
                  icon: LucideIcons.qrCode,
                  label: 'QR za primanje',
                  onTap: when(CardAction.receive, () => context.push(AppRoutes.receiveCode(card.id))),
                ),
              ),
            ],
          ),
          for (final reason in reasons) ...[const SizedBox(height: AppConstants.spacingSM), InfoNote(reason)],
          const SizedBox(height: AppConstants.spacingLG),
          const SectionHeader(title: 'Podaci'),
          const SizedBox(height: AppConstants.spacingRow),
          _CardAttributes(card: card),
          const SizedBox(height: AppConstants.spacingLG),
          const SectionHeader(title: 'Historija'),
          const SizedBox(height: AppConstants.spacingRow),
          FilterChips<TransactionDirection?>(
            options: const [
              ('Sve', null),
              ('Poslano', TransactionDirection.sent),
              ('Primljeno', TransactionDirection.received),
            ],
            selected: _direction,
            onSelected: (direction) => setState(() => _direction = direction),
          ),
          const SizedBox(height: AppConstants.spacingRow),
          history.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(AppConstants.spacingLG),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (error, _) => ErrorState(
              title: 'Historija se nije učitala',
              message: ApiException.messageOf(error),
              onRetry: () => ref.invalidate(cardTransactionsProvider(_history)),
            ),
            data: (page) => page.items.isEmpty
                ? EmptyState(
                    icon: LucideIcons.arrowDownLeft,
                    title: switch (_direction) {
                      TransactionDirection.sent => 'Nema poslanih transakcija',
                      TransactionDirection.received => 'Nema primljenih transakcija',
                      null => 'Još nema transakcija',
                    },
                    message: 'Ovdje će se pojaviti uplate i isplate ove kartice.',
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      DetailsPanel(
                        children: [
                          for (final transaction in page.items)
                            TransactionTile(
                              transaction: transaction,
                              onTap: () => context.push(AppRoutes.transactionDetails(transaction.id)),
                            ),
                        ],
                      ),
                      PagedListFooter(
                        page: page,
                        onRetry: () => ref.read(cardTransactionsProvider(_history).notifier).loadMore(),
                      ),
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

/// Pločica akcije u detaljima. Onemogućena pločica je siva i ne reaguje, a razlog stoji ispod reda pločica.
class _ActionTile extends StatelessWidget {
  const _ActionTile({required this.icon, required this.label, required this.onTap, this.destructive = false});

  final IconData icon;
  final String label;
  final VoidCallback? onTap;
  final bool destructive;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final enabled = onTap != null;
    final iconColor = !enabled ? scheme.onSurfaceVariant : (destructive ? scheme.error : scheme.primary);
    final textColor = !enabled ? scheme.onSurfaceVariant : (destructive ? scheme.error : scheme.onSurface);

    return Material(
      color: enabled ? scheme.surfaceContainerLowest : scheme.surfaceContainerHigh,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppConstants.radiusCard),
        side: BorderSide(color: scheme.outline),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppConstants.radiusCard),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: AppConstants.spacingRow, horizontal: AppConstants.spacingSM),
          child: Column(
            children: [
              Icon(icon, size: AppConstants.actionTileIconSize, color: iconColor),
              const SizedBox(height: AppConstants.spacingSM),
              Text(
                label,
                textAlign: TextAlign.center,
                style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w500, color: textColor),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Atributi kartice u dvije kolone, labela s ikonom lijevo i vrijednost desno. ID se ne prikazuje.
class _CardAttributes extends StatelessWidget {
  const _CardAttributes({required this.card});

  final CardResponse card;

  @override
  Widget build(BuildContext context) {
    return DetailsPanel(
      children: [
        if (card.name != null) DetailsRow.text(icon: LucideIcons.tag, label: 'Naziv', text: card.name!),
        DetailsRow.text(icon: LucideIcons.creditCard, label: 'Tip', text: card.cardTypeName),
        DetailsRow.text(icon: LucideIcons.banknote, label: 'Valuta', text: card.currencyCode),
        DetailsRow(
          icon: LucideIcons.activity,
          label: 'Status',
          value: StatusChip(label: card.cardStatusName, tone: CardAppearance.tone(card.cardStatusCode)),
        ),
        DetailsRow.text(icon: LucideIcons.calendar, label: 'Važi do', text: DateFormatter.cardExpiry(card.expiresAt)),
        DetailsRow.text(icon: LucideIcons.user, label: 'Vlasnik', text: card.holderName),
        DetailsRow.text(icon: LucideIcons.calendarPlus, label: 'Dodana', text: DateFormatter.date(card.createdAt)),
        DetailsRow.text(icon: LucideIcons.star, label: 'Primarna za primanje', text: card.isDefault ? 'Da' : 'Ne'),
      ],
    );
  }
}
