import 'package:flutter/material.dart';

import '../formatting/money_formatter.dart';
import '../theme/torvin_colors.dart';

/// Iznos u formatu `1.245,00 BAM`. Uz `signed` priliv je zelen s `+`, a odliv crven s `−`
/// (red transakcije); bez njega je iznos u boji teksta (balans).
class MoneyText extends StatelessWidget {
  const MoneyText(
    this.amount, {
    super.key,
    this.currency = MoneyFormatter.defaultCurrency,
    this.signed = false,
    this.style,
  });

  final num amount;
  final String currency;
  final bool signed;

  /// Podrazumijevano `titleMedium` (DS: iznos 16/24).
  final TextStyle? style;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final cents = MoneyFormatter.roundToCents(amount);

    final color = !signed || cents == 0
        ? theme.colorScheme.onSurface
        : (cents > 0 ? colors.successText : colors.errorText);

    return Text(
      MoneyFormatter.format(amount, currency: currency, signed: signed),
      style: (style ?? theme.textTheme.titleMedium)?.copyWith(
        color: color,
        // Cifre iste širine, da se iznosi u listi poravnaju
        fontFeatures: const [FontFeature.tabularFigures()],
      ),
    );
  }
}
