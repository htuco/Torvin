import 'package:flutter/material.dart';

import '../theme/app_constants.dart';
import '../theme/torvin_colors.dart';

/// Ton statusa. Koji status kartice, transakcije ili zahtjeva dobija koji ton odlučuje ekran tog featurea.
enum StatusTone { success, warning, error, neutral }

/// Status uvijek tačkom i riječju (DS v2): boja nikad nije jedini nosilac značenja.
class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.label, required this.tone});

  final String label;
  final StatusTone tone;

  @override
  Widget build(BuildContext context) {
    final colors = context.torvinColors;
    final (tint, dot, text) = switch (tone) {
      StatusTone.success => (colors.successTint, colors.successDot, colors.successChip),
      StatusTone.warning => (colors.warningTint, colors.warningDot, colors.warningChip),
      StatusTone.error => (colors.errorTint, colors.errorDot, colors.errorChip),
      StatusTone.neutral => (colors.neutralTint, colors.neutralChip, colors.neutralChip),
    };

    return Container(
      height: AppConstants.chipHeight,
      padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingRow),
      decoration: BoxDecoration(color: tint, borderRadius: BorderRadius.circular(AppConstants.radiusFull)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: AppConstants.statusDotSize,
            height: AppConstants.statusDotSize,
            decoration: BoxDecoration(color: dot, shape: BoxShape.circle),
          ),
          const SizedBox(width: AppConstants.chipGap),
          Text(
            label,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: text, fontWeight: FontWeight.w500),
          ),
        ],
      ),
    );
  }
}
