import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import '../../../app/widgets/details_panel.dart';
import '../../../app/widgets/filter_chips.dart';
import '../../../app/widgets/load_more_listener.dart';
import '../../../app/widgets/paged_list_footer.dart';
import '../../../app/widgets/search_field.dart';
import '../../cards/data/card_providers.dart';
import '../data/transaction_providers.dart';
import 'amount_range_dialog.dart';
import 'transaction_tile.dart';

/// Historija mojih transakcija s pretragom i filterima (smjer, period, kartica, iznos), grupisana po danu.
class TransactionsScreen extends ConsumerStatefulWidget {
  const TransactionsScreen({super.key});

  /// Najraniji dan koji nudi izbor perioda.
  static final DateTime firstDay = DateTime(2020);

  @override
  ConsumerState<TransactionsScreen> createState() => _TransactionsScreenState();
}

class _TransactionsScreenState extends ConsumerState<TransactionsScreen> {
  final _search = TextEditingController();
  TransactionFilter _filter = TransactionFilter.none;

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _update(TransactionFilter filter) => setState(() => _filter = filter);

  void _clearFilters() {
    _search.clear();
    _update(TransactionFilter.none);
  }

  Future<void> _choosePeriod() async {
    final period = await showDateRangePicker(
      context: context,
      firstDate: TransactionsScreen.firstDay,
      lastDate: DateTime.now(),
      initialDateRange: _filter.period,
      // Ručni unos datuma otvara mali dijalog s providnom pozadinom, pa je izbor samo kalendarom
      initialEntryMode: DatePickerEntryMode.calendarOnly,
    );
    if (period != null) {
      _update(_filter.withPeriod(period));
    }
  }

  Future<void> _chooseAmount() async {
    final range = await showAmountRangeDialog(
      context,
      initial: (fromCents: _filter.amountFromCents, toCents: _filter.amountToCents),
    );
    if (range != null) {
      _update(_filter.withAmount(range.fromCents, range.toCents));
    }
  }

  @override
  Widget build(BuildContext context) {
    // Historija i moje kartice (za filter) se traže paralelno
    final history = ref.watch(transactionHistoryProvider(_filter));
    final cardsState = ref.watch(cardListProvider(null));
    final cards = cardsState.value?.items ?? const <CardResponse>[];
    final filter = _filter;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Transakcije'),
        actions: [
          IconButton(
            tooltip: 'Pošalji novac',
            icon: const Icon(LucideIcons.send),
            onPressed: () => context.push(AppRoutes.newTransaction),
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppConstants.spacingLG,
              0,
              AppConstants.spacingLG,
              AppConstants.spacingRow,
            ),
            child: SearchField(
              controller: _search,
              label: 'Pretraga',
              hint: 'Ime, napomena ili referenca',
              onSearch: (text) => _update(filter.withFts(text.isEmpty ? null : text)),
            ),
          ),
          FilterChips<TransactionDirection?>(
            padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingLG),
            options: const [
              ('Sve', null),
              ('Primljeno', TransactionDirection.received),
              ('Poslano', TransactionDirection.sent),
            ],
            selected: filter.direction,
            onSelected: (direction) => _update(filter.withDirection(direction)),
          ),
          const SizedBox(height: AppConstants.spacingSM),
          _MoreFilters(
            filter: filter,
            cards: cards,
            onPeriod: _choosePeriod,
            onAmount: _chooseAmount,
            onChanged: _update,
          ),
          // Bez kartica filter nudi samo "Sve kartice", pa ekran kaže zašto
          if (cardsState.hasError)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppConstants.spacingLG,
                AppConstants.spacingSM,
                AppConstants.spacingLG,
                0,
              ),
              child: InfoNote('Kartice za filter se nisu učitale. ${ApiException.messageOf(cardsState.error!)}'),
            ),
          const SizedBox(height: AppConstants.spacingMD),
          Expanded(
            child: history.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ErrorState(
                title: 'Transakcije se nisu učitale',
                message: ApiException.messageOf(error),
                onRetry: () => ref.invalidate(transactionHistoryProvider(filter)),
              ),
              data: (page) => page.items.isEmpty ? _empty() : _list(page),
            ),
          ),
        ],
      ),
    );
  }

  Widget _empty() {
    if (!_filter.isEmpty) {
      return EmptyState(
        icon: LucideIcons.searchX,
        title: 'Nema rezultata',
        message: 'Pokušajte s drugim periodom ili uklonite filtere.',
        actionLabel: 'Ukloni filtere',
        onAction: _clearFilters,
      );
    }

    return EmptyState(
      icon: LucideIcons.arrowLeftRight,
      title: 'Još nema transakcija',
      message: 'Ovdje će se pojaviti uplate i isplate.',
      actionLabel: 'Pošalji novac',
      onAction: () => context.push(AppRoutes.newTransaction),
    );
  }

  /// Redovi po lokalnom danu, najnoviji prvi (redoslijed servera). Zbira dana nema, jer su kartice u više valuta.
  Widget _list(PagedList<TransactionResponse> page) {
    final days = <String, List<TransactionResponse>>{};
    for (final transaction in page.items) {
      days.putIfAbsent(DateFormatter.dayHeader(transaction.createdAt), () => []).add(transaction);
    }

    return LoadMoreListener(
      onLoadMore: () => ref.read(transactionHistoryProvider(_filter).notifier).loadMore(),
      child: ListView(
        padding: const EdgeInsets.fromLTRB(AppConstants.spacingLG, 0, AppConstants.spacingLG, AppConstants.spacingLG),
        children: [
          for (final MapEntry(key: day, value: transactions) in days.entries) ...[
            SectionHeader(title: day),
            const SizedBox(height: AppConstants.spacingSM),
            DetailsPanel(
              children: [
                for (final transaction in transactions)
                  TransactionTile(
                    transaction: transaction,
                    timeOnly: true,
                    onTap: () => context.push(AppRoutes.transactionDetails(transaction.id)),
                  ),
              ],
            ),
            const SizedBox(height: AppConstants.spacingMD),
          ],
          PagedListFooter(
            page: page,
            onRetry: () => ref.read(transactionHistoryProvider(_filter).notifier).loadMore(),
          ),
        ],
      ),
    );
  }
}

/// Period, kartica i iznos. Aktivan filter je izabran čip s vrijednošću i X koji ga uklanja.
class _MoreFilters extends StatelessWidget {
  const _MoreFilters({
    required this.filter,
    required this.cards,
    required this.onPeriod,
    required this.onAmount,
    required this.onChanged,
  });

  final TransactionFilter filter;
  final List<CardResponse> cards;
  final VoidCallback onPeriod;
  final VoidCallback onAmount;
  final ValueChanged<TransactionFilter> onChanged;

  static String _amountLabel(int? from, int? to) {
    const text = MoneyFormatter.centsText;
    return switch ((from, to)) {
      (final from?, final to?) => '${text(from)} – ${text(to)}',
      (final from?, null) => 'od ${text(from)}',
      (null, final to?) => 'do ${text(to)}',
      _ => 'Iznos',
    };
  }

  @override
  Widget build(BuildContext context) {
    final period = filter.period;
    final card = cards.where((card) => card.id == filter.cardId).firstOrNull;
    final hasAmount = filter.amountFromCents != null || filter.amountToCents != null;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingLG),
      child: Row(
        children: [
          _FilterChip(
            label: period == null
                ? 'Period'
                : '${DateFormatter.date(period.start)} – ${DateFormatter.date(period.end)}',
            active: period != null,
            onPressed: onPeriod,
            onClear: () => onChanged(filter.withPeriod(null)),
          ),
          MenuAnchor(
            menuChildren: [
              MenuItemButton(onPressed: () => onChanged(filter.withCard(null)), child: const Text('Sve kartice')),
              for (final card in cards)
                MenuItemButton(onPressed: () => onChanged(filter.withCard(card.id)), child: Text(card.label)),
            ],
            builder: (context, controller, _) => _FilterChip(
              label: card?.label ?? 'Sve kartice',
              active: card != null,
              onPressed: () => controller.isOpen ? controller.close() : controller.open(),
              onClear: () => onChanged(filter.withCard(null)),
            ),
          ),
          _FilterChip(
            label: _amountLabel(filter.amountFromCents, filter.amountToCents),
            active: hasAmount,
            onPressed: onAmount,
            onClear: () => onChanged(filter.withAmount(null, null)),
          ),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({required this.label, required this.active, required this.onPressed, required this.onClear});

  final String label;
  final bool active;
  final VoidCallback onPressed;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: AppConstants.spacingSM),
      child: SizedBox(
        height: AppConstants.filterChipHeight,
        child: InputChip(
          label: Text(label),
          selected: active,
          materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
          onPressed: onPressed,
          onDeleted: active ? onClear : null,
          deleteButtonTooltipMessage: 'Ukloni filter',
          deleteIcon: const Icon(LucideIcons.x, size: AppConstants.iconSize),
        ),
      ),
    );
  }
}
