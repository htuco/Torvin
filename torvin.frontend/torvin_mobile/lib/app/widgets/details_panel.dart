import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Bijeli panel s ivicom i razdjelnicima između redova.
class DetailsPanel extends StatelessWidget {
  const DetailsPanel({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      decoration: BoxDecoration(
        color: scheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(AppConstants.radiusCard),
        border: Border.all(color: scheme.outline),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0) const Divider(),
            children[i],
          ],
        ],
      ),
    );
  }
}

/// Red podataka u dvije kolone: labela s ikonom lijevo i vrijednost desno. Tekst je `DetailsRow.text`, a može
/// biti i drugi widget (npr. `StatusChip`).
class DetailsRow extends StatelessWidget {
  const DetailsRow({super.key, required this.icon, required this.label, required this.value});

  DetailsRow.text({super.key, required this.icon, required this.label, required String text})
      : value = _ValueText(text);

  final IconData icon;
  final String label;
  final Widget value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingMD, vertical: AppConstants.spacingRow),
      child: Row(
        children: [
          Icon(icon, size: AppConstants.iconSize, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: AppConstants.spacingSM),
          Text(label, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
          const SizedBox(width: AppConstants.spacingMD),
          // Duga vrijednost (npr. napomena) se prelama desno, a labela ostaje u jednom redu
          Expanded(child: Align(alignment: Alignment.centerRight, child: value)),
        ],
      ),
    );
  }
}

class _ValueText extends StatelessWidget {
  const _ValueText(this.text);

  final String text;

  @override
  Widget build(BuildContext context) =>
      Text(
        text,
        textAlign: TextAlign.end,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500),
      );
}
