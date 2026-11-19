import 'package:flutter/material.dart';

import '../theme/app_constants.dart';

/// Dugme forme koje je onemogućeno dok zahtjev traje, da dvostruki klik ne pošalje dva zahtjeva.
class SubmitButton extends StatelessWidget {
  const SubmitButton({super.key, required this.label, required this.busy, required this.onPressed});

  final String label;
  final bool busy;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    // Visinu daje tema: 48 na mobilnoj, 40 na desktopu
    return FilledButton(
      onPressed: busy ? null : onPressed,
      child: busy
          ? const SizedBox.square(
              dimension: AppConstants.iconSize,
              child: CircularProgressIndicator(strokeWidth: AppConstants.borderWidthMedium),
            )
          : Text(label),
    );
  }
}
