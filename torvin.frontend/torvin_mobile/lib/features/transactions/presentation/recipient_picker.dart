import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/widgets/initials_avatar.dart';
import '../../recipients/data/recipient_models.dart';
import '../../recipients/data/recipient_providers.dart';

/// Izbor primaoca u formi nove transakcije: omiljeni jednim dodirom, pretraga po imenu ili korisničkom imenu, ili
/// QR kod. Izbor drži roditelj, a ovaj widget samo prikazuje pretragu ili izabranog primaoca.
class RecipientPicker extends ConsumerStatefulWidget {
  const RecipientPicker({
    super.key,
    required this.selected,
    required this.favorites,
    required this.onSelected,
    required this.onScan,
    required this.scanning,
    required this.error,
    required this.onEdited,
  });

  /// Pretraga kreće tek od ovoliko znakova, isto kao `RecipientService.MinQueryLength`.
  static const int minQueryLength = 3;

  /// Najduža pretraga, isto kao `MaxLength` na `BaseSearchObject.Fts`.
  static const int maxQueryLength = 100;

  /// Koliko se čeka poslije zadnjeg slova prije pretrage, da svako slovo ne bude novi zahtjev.
  static const Duration searchDelay = Duration(milliseconds: 300);

  /// Koliko omiljenih stane u red iznad pretrage.
  static const int favoritesShown = 8;

  final Recipient? selected;
  final List<FavoriteRecipientResponse> favorites;
  final ValueChanged<Recipient?> onSelected;
  final VoidCallback onScan;

  /// Dok server razrješava skeniran kod, dugme za QR čeka.
  final bool scanning;

  /// Greška primaoca sa servera (`toUserId`) ili iz QR koda (`code`).
  final String? error;

  /// Korisnik je promijenio pretragu ili izbor, pa stara greška više ne važi.
  final VoidCallback onEdited;

  @override
  ConsumerState<RecipientPicker> createState() => _RecipientPickerState();
}

class _RecipientPickerState extends ConsumerState<RecipientPicker> {
  final _query = TextEditingController();
  Timer? _debounce;

  /// Tekst za koji se traže rezultati, poslije zakašnjenja.
  String _search = '';

  @override
  void dispose() {
    _debounce?.cancel();
    _query.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    widget.onEdited();
    _debounce?.cancel();
    _debounce = Timer(RecipientPicker.searchDelay, () {
      if (mounted) {
        setState(() => _search = value.trim());
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final selected = widget.selected;
    if (selected != null) {
      return _SelectedRecipient(
        recipient: selected,
        error: widget.error,
        onChange: () {
          widget.onEdited();
          widget.onSelected(null);
        },
      );
    }

    final favorites = widget.favorites.take(RecipientPicker.favoritesShown).toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (favorites.isNotEmpty) ...[
          Text('Omiljeni', style: FieldNote.labelStyle(Theme.of(context))),
          const SizedBox(height: AppConstants.spacingSM),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                for (final favorite in favorites)
                  _FavoriteAvatar(
                    favorite: favorite,
                    onTap: () {
                      widget.onEdited();
                      widget.onSelected(Recipient.fromFavorite(favorite));
                    },
                  ),
              ],
            ),
          ),
          const SizedBox(height: AppConstants.spacingMD),
        ],
        TorvinTextField(
          controller: _query,
          label: 'Primalac',
          hint: 'Ime, korisničko ime ili email',
          helper: 'Unesite od ${RecipientPicker.minQueryLength} do ${RecipientPicker.maxQueryLength} znakova.',
          textInputAction: TextInputAction.search,
          inputFormatters: [LengthLimitingTextInputFormatter(RecipientPicker.maxQueryLength)],
          // Provjera tek pri slanju: dok korisnik kuca pretragu, "obavezno" bi bila lažna greška
          autovalidateMode: AutovalidateMode.disabled,
          forceErrorText: widget.error,
          onChanged: _onChanged,
          // Upisano ime nije izbor: primalac je izabran tek dodirom na rezultat, omiljenog ili QR kod
          validator: (_) => 'Izaberite primaoca iz rezultata, omiljenih ili QR koda.',
          suffix: widget.scanning
              ? const Padding(
                  padding: EdgeInsets.all(AppConstants.spacingRow),
                  child: SizedBox.square(
                    dimension: AppConstants.iconSize,
                    child: CircularProgressIndicator(strokeWidth: AppConstants.borderWidthMedium),
                  ),
                )
              : IconButton(
                  tooltip: 'Skeniraj QR kod',
                  icon: const Icon(LucideIcons.scanLine, size: AppConstants.iconSize),
                  onPressed: widget.onScan,
                ),
        ),
        if (_search.length >= RecipientPicker.minQueryLength) _Results(query: _search, onSelected: widget.onSelected),
      ],
    );
  }
}

/// Rezultati pretrage ispod polja: do 10 korisnika, bez emaila.
class _Results extends ConsumerWidget {
  const _Results({required this.query, required this.onSelected});

  final String query;
  final ValueChanged<Recipient?> onSelected;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final secondary = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    return Padding(
      padding: const EdgeInsets.only(top: AppConstants.spacingSM),
      child: switch (ref.watch(recipientSearchProvider(query))) {
        AsyncData(:final value) when value.isEmpty => Text('Nema korisnika za „$query”.', style: secondary),
        AsyncData(:final value) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (final recipient in value)
                _RecipientRow(
                  firstName: recipient.firstName,
                  lastName: recipient.lastName,
                  username: recipient.username,
                  onTap: () => onSelected(Recipient.fromSearch(recipient)),
                ),
            ],
          ),
        // Greška pretrage s backenda (npr. preduga pretraga) je poruka polja `fts`, a ne opšta poruka
        AsyncError(:final error) => FieldNote(
            error: error is ApiException ? error.fieldError('fts') ?? error.message : ApiException.messageOf(error),
          ),
        _ => const LinearProgressIndicator(),
      },
    );
  }
}

class _RecipientRow extends StatelessWidget {
  const _RecipientRow({required this.firstName, required this.lastName, required this.username, required this.onTap});

  final String firstName;
  final String lastName;
  final String username;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppConstants.radiusControl),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppConstants.spacingSM),
        child: _RecipientSummary(firstName: firstName, lastName: lastName, subtitle: '@$username'),
      ),
    );
  }
}

/// Avatar, ime i prezime, a ispod korisničko ime i ostalo što se zna o primaocu.
class _RecipientSummary extends StatelessWidget {
  const _RecipientSummary({
    required this.firstName,
    required this.lastName,
    required this.subtitle,
    this.avatarSize = AppConstants.listIconBox,
  });

  final String firstName;
  final String lastName;
  final String subtitle;
  final double avatarSize;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        InitialsAvatar(firstName: firstName, lastName: lastName, size: avatarSize),
        const SizedBox(width: AppConstants.spacingRow),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('$firstName $lastName', style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w500)),
              Text(subtitle, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
            ],
          ),
        ),
      ],
    );
  }
}

class _FavoriteAvatar extends StatelessWidget {
  const _FavoriteAvatar({required this.favorite, required this.onTap});

  final FavoriteRecipientResponse favorite;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(right: AppConstants.spacingRow),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppConstants.radiusControl),
        child: SizedBox(
          width: AppConstants.favoriteTileWidth,
          child: Column(
            children: [
              InitialsAvatar(
                firstName: favorite.firstName,
                lastName: favorite.lastName,
                size: AppConstants.favoriteAvatarSize,
              ),
              const SizedBox(height: AppConstants.favoriteNameGap),
              Text(
                favorite.displayName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.center,
                style: theme.textTheme.bodySmall?.copyWith(
                  fontWeight: FontWeight.w500,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Izabran primalac s "Promijeni". Nadimak se zna samo iz omiljenih, a kartica samo iz QR koda.
class _SelectedRecipient extends StatelessWidget {
  const _SelectedRecipient({required this.recipient, required this.error, required this.onChange});

  final Recipient recipient;
  final String? error;
  final VoidCallback onChange;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final hasError = error != null;
    final subtitle = ['@${recipient.username}', ?recipient.nickname, ?recipient.cardLabel].join(' · ');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text('Primalac', style: FieldNote.labelStyle(theme)),
        const SizedBox(height: AppConstants.spacingSM),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingRow, vertical: AppConstants.spacingSM),
          decoration: BoxDecoration(
            color: scheme.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(AppConstants.radiusControl),
            border: Border.all(
              color: hasError ? scheme.error : scheme.outline,
              width: hasError ? AppConstants.borderWidthMedium : AppConstants.borderWidthThin,
            ),
          ),
          child: Row(
            children: [
              Expanded(
                child: _RecipientSummary(
                  firstName: recipient.firstName,
                  lastName: recipient.lastName,
                  subtitle: subtitle,
                  avatarSize: AppConstants.avatarSize,
                ),
              ),
              TextButton(onPressed: onChange, child: const Text('Promijeni')),
            ],
          ),
        ),
        FieldNote(error: error),
      ],
    );
  }
}
