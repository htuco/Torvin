import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';
import '../theme/torvin_colors.dart';
import 'info_note.dart';

/// Otvara dijalog nad ekranom koji se zatamni filterom svjetline (DS v2), a ne providnim slojem preko njega.
/// Klik van dijaloga ga zatvara bez vrijednosti.
Future<T?> showTorvinDialog<T>(BuildContext context, {required WidgetBuilder builder}) {
  const b = AppConstants.dialogBackdropBrightness;
  return showGeneralDialog<T>(
    context: context,
    barrierDismissible: true,
    barrierLabel: 'Zatvori',
    barrierColor: Colors.transparent,
    pageBuilder: (context, _, _) => Stack(
      children: [
        // Množi boje ekrana ispod s 0,42, isto kao CSS `filter: brightness(.42)` u dizajnu
        Positioned.fill(
          child: BackdropFilter(
            filter: const ColorFilter.matrix([b, 0, 0, 0, 0, 0, b, 0, 0, 0, 0, 0, b, 0, 0, 0, 0, 0, 1, 0]),
            child: const SizedBox.expand(),
          ),
        ),
        // Tastatura (napomena u dijalogu) podiže dijalog, a dugi sadržaj se skrola
        Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppConstants.spacingLG) + MediaQuery.viewInsetsOf(context),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: AppConstants.dialogMaxWidth),
              child: Builder(builder: builder),
            ),
          ),
        ),
      ],
    ),
  );
}

/// Pitanje prije nepovratne akcije. Vraća `true` samo kad korisnik potvrdi, a "Odustani", X i klik van dijaloga
/// vraćaju `false`.
Future<bool> showConfirmDialog(
  BuildContext context, {
  required String title,
  required String message,
  required String confirmLabel,
  IconData icon = LucideIcons.circleAlert,
}) async {
  final confirmed = await showTorvinDialog<bool>(
    context,
    builder: (context) => TorvinDialog(
      icon: icon,
      title: title,
      message: message,
      confirmLabel: confirmLabel,
      onConfirm: () => Navigator.of(context).pop(true),
    ),
  );
  return confirmed ?? false;
}

/// Okvir dijaloga iz DS v2: ikona, naslov, poruka, opciona polja, savjet i dva dugmeta. X gore desno
/// zatvara dijalog kao "Odustani" (upute §6).
class TorvinDialog extends StatelessWidget {
  const TorvinDialog({
    super.key,
    required this.icon,
    required this.title,
    required this.confirmLabel,
    required this.onConfirm,
    this.message,
    this.note,
    this.destructive = false,
    this.child,
  });

  final IconData icon;
  final String title;
  final String? message;

  /// Polja dijaloga (npr. razlog i napomena). Stoje između poruke i savjeta.
  final Widget? child;

  /// Savjet ispod poruke, na sivoj podlozi s ikonom.
  final String? note;
  final String confirmLabel;
  final VoidCallback onConfirm;

  /// Nepovratna akcija: ikona i dugme potvrde su u boji greške.
  final bool destructive;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final colors = context.torvinColors;
    final message = this.message;
    final child = this.child;
    final note = this.note;

    return Container(
      decoration: BoxDecoration(
        color: scheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(AppConstants.radiusCard),
        boxShadow: const [AppConstants.dialogShadow],
      ),
      child: Material(
        type: MaterialType.transparency,
        child: Padding(
          padding: const EdgeInsets.all(AppConstants.spacingLG),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: AppConstants.dialogIconBox,
                    height: AppConstants.dialogIconBox,
                    decoration: BoxDecoration(
                      color: destructive ? colors.errorTint : scheme.primaryContainer,
                      borderRadius: BorderRadius.circular(AppConstants.radiusControl),
                    ),
                    child: Icon(
                      icon,
                      size: AppConstants.dialogIconSize,
                      color: destructive ? colors.errorText : scheme.primary,
                    ),
                  ),
                  const Spacer(),
                  IconButton(
                    tooltip: 'Zatvori',
                    icon: const Icon(LucideIcons.x),
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const SizedBox(height: AppConstants.spacingMD),
              Text(title, style: theme.textTheme.titleLarge),
              if (message != null) ...[
                const SizedBox(height: AppConstants.spacingSM),
                Text(message, style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant)),
              ],
              if (child != null) ...[const SizedBox(height: AppConstants.spacingMD), child],
              if (note != null) ...[const SizedBox(height: AppConstants.spacingMD), InfoNote(note)],
              const SizedBox(height: AppConstants.spacingMD),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Odustani')),
                  ),
                  const SizedBox(width: AppConstants.spacingRow),
                  Expanded(
                    child: FilledButton(
                      style: destructive
                          ? FilledButton.styleFrom(backgroundColor: scheme.error, foregroundColor: scheme.onError)
                          : null,
                      onPressed: onConfirm,
                      child: Text(confirmLabel),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
