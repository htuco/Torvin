import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Red filter čipova (DS v2): izabran je čip čija je vrijednost `selected` (najviše jedan, a za brze sume iznosa
/// nijedan kad iznos nije jedna od njih). Red se skrola kad čipovi ne stanu.
class FilterChips<T> extends StatelessWidget {
  const FilterChips({
    super.key,
    required this.options,
    required this.selected,
    required this.onSelected,
    this.padding = EdgeInsets.zero,
  });

  /// Naziv čipa i vrijednost filtera. `null` je obično "Sve".
  final List<(String label, T value)> options;
  final T selected;
  final ValueChanged<T> onSelected;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: padding,
      child: Row(
        children: [
          for (final (label, value) in options)
            Padding(
              padding: const EdgeInsets.only(right: AppConstants.spacingSM),
              child: SizedBox(
                height: AppConstants.filterChipHeight,
                child: ChoiceChip(
                  label: Text(label),
                  selected: selected == value,
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  onSelected: (_) => onSelected(value),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
