import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/widgets/details_panel.dart';
import '../../../app/widgets/initials_avatar.dart';
import '../../recipients/data/recipient_providers.dart';
import '../data/transaction_providers.dart';

/// Detalji jedne moje transakcije iz mog pogleda (poslano ili primljeno). ID-evi se ne prikazuju, a za podršku je
/// tu referentni broj.
class TransactionDetailsScreen extends ConsumerWidget {
  const TransactionDetailsScreen({super.key, required this.transactionId});

  final int transactionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final transaction = ref.watch(transactionProvider(transactionId));
    return Scaffold(
      appBar: AppBar(title: const Text('Detalji transakcije')),
      body: transaction.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorState(
          title: 'Transakcija se nije učitala',
          message: ApiException.messageOf(error),
          onRetry: () => ref.invalidate(transactionProvider(transactionId)),
        ),
        data: (transaction) => _Details(transaction: transaction),
      ),
    );
  }
}

class _Details extends StatelessWidget {
  const _Details({required this.transaction});

  final TransactionResponse transaction;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final myCard = transaction.myCardLast4Digits;
    final otherCard = transaction.counterpartyCardLast4Digits;
    final note = transaction.note;
    final category = transaction.transactionCategoryName;
    final sent = transaction.direction == TransactionDirection.sent;

    return ListView(
      padding: const EdgeInsets.all(AppConstants.spacingLG),
      children: [
        Center(
          child: InitialsAvatar(
            firstName: transaction.counterpartyFirstName,
            lastName: transaction.counterpartyLastName,
            size: AppConstants.favoriteAvatarSize,
          ),
        ),
        const SizedBox(height: AppConstants.spacingRow),
        Text(transaction.counterpartyName, textAlign: TextAlign.center, style: theme.textTheme.titleLarge),
        const SizedBox(height: AppConstants.spacingXS),
        Center(
          child: MoneyText(
            transaction.signedAmount,
            currency: transaction.currencyCode,
            signed: true,
            style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w600),
          ),
        ),
        const SizedBox(height: AppConstants.spacingSM),
        Center(
          child: StatusChip(
            label: transaction.transactionStatusName,
            tone: TransactionAppearance.tone(transaction.transactionStatusCode),
          ),
        ),
        const SizedBox(height: AppConstants.spacingLG),
        DetailsPanel(
          children: [
            DetailsRow.text(
              icon: LucideIcons.calendarClock,
              label: 'Datum i vrijeme',
              text: DateFormatter.dateTime(transaction.createdAt),
            ),
            DetailsRow.text(icon: LucideIcons.arrowLeftRight, label: 'Tip', text: transaction.transactionTypeName),
            DetailsRow.text(
              icon: sent ? LucideIcons.arrowUpRight : LucideIcons.arrowDownLeft,
              label: 'Smjer',
              text: sent ? 'Poslano' : 'Primljeno',
            ),
            if (myCard != null)
              DetailsRow.text(
                icon: LucideIcons.creditCard,
                label: sent ? 'Sa kartice' : 'Na karticu',
                text: CardResponse.mask(myCard),
              ),
            if (otherCard != null)
              DetailsRow.text(
                icon: LucideIcons.creditCard,
                label: sent ? 'Kartica primaoca' : 'Kartica pošiljaoca',
                text: CardResponse.mask(otherCard),
              ),
            if (note != null) DetailsRow.text(icon: LucideIcons.messageSquare, label: 'Napomena', text: note),
            if (category != null) DetailsRow.text(icon: LucideIcons.tag, label: 'Kategorija', text: category),
            DetailsRow.text(icon: LucideIcons.hash, label: 'Referenca', text: transaction.referenceNumber),
          ],
        ),
        const SizedBox(height: AppConstants.spacingLG),
        _AddFavorite(transaction: transaction),
      ],
    );
  }
}

/// "Dodaj u omiljene" za drugu stranu. Kad je već među omiljenim, dugme je onemogućeno uz razlog. Dok provjera
/// traje ili kad ne uspije, dugme je dostupno, a duplikat i tako odbija server.
class _AddFavorite extends ConsumerStatefulWidget {
  const _AddFavorite({required this.transaction});

  final TransactionResponse transaction;

  @override
  ConsumerState<_AddFavorite> createState() => _AddFavoriteState();
}

class _AddFavoriteState extends ConsumerState<_AddFavorite> {
  /// Isto kao `FavoriteRecipientService.AlreadyFavorite` na backendu.
  static const String alreadyFavorite = 'Primalac je već među omiljenim.';

  bool _busy = false;

  FavoriteCheck get _check =>
      (userId: widget.transaction.counterpartyUserId, lastName: widget.transaction.counterpartyLastName);

  Future<void> _add() async {
    setState(() => _busy = true);
    try {
      await ref.read(recipientsRepositoryProvider).addFavorite(widget.transaction.counterpartyUserId);
      if (mounted) {
        showTorvinSnackBar(context, 'Dodano u omiljene: ${widget.transaction.counterpartyName}.');
      }
    } on ApiException catch (error) {
      // Server javlja razlog kao grešku polja ("Primalac nije pronađen."), a opšta poruka je samo
      // "Podaci nisu ispravni."
      if (mounted) {
        showTorvinSnackBar(context, error.fieldError('recipientUserId') ?? error.message);
      }
    } finally {
      if (mounted) {
        refreshFavorites(ref);
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isFavorite = ref.watch(isFavoriteProvider(_check)).value ?? false;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        OutlinedButton.icon(
          onPressed: isFavorite || _busy ? null : _add,
          icon: const Icon(LucideIcons.star, size: AppConstants.iconSize),
          label: const Text('Dodaj u omiljene'),
        ),
        if (isFavorite) ...[const SizedBox(height: AppConstants.spacingSM), const InfoNote(alreadyFavorite)],
      ],
    );
  }
}
