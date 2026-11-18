import 'package:flutter/material.dart';

/// Opšta poruka greške ispod forme (npr. pogrešni podaci ili server nije dostupan). Nikad dijalog.
class FormErrorText extends StatelessWidget {
  const FormErrorText(this.message, {super.key});

  final String? message;

  @override
  Widget build(BuildContext context) {
    final message = this.message;
    if (message == null) {
      return const SizedBox.shrink();
    }

    final theme = Theme.of(context);
    return Text(message, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.error));
  }
}
