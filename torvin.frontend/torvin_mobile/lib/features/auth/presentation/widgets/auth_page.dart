import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Okvir ekrana iz toka prijave: naslov, dugme nazad (ako ima kuda) i forma koja se skroluje.
class AuthPage extends StatelessWidget {
  const AuthPage({super.key, required this.title, this.subtitle, this.notice, required this.child});

  final String title;
  final String? subtitle;

  /// Poruka prethodnog koraka, npr. "Poslali smo kod za potvrdu na vaš email."
  final String? notice;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final subtitle = this.subtitle;
    final notice = this.notice;

    return Scaffold(
      appBar: Navigator.of(context).canPop() ? AppBar() : null,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppConstants.spacingLG),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(title, style: theme.textTheme.headlineSmall),
              if (subtitle != null) ...[
                const SizedBox(height: AppConstants.spacingXS),
                Text(subtitle, style: theme.textTheme.bodyMedium),
              ],
              if (notice != null) ...[
                const SizedBox(height: AppConstants.spacingMD),
                Text(notice, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.primary)),
              ],
              const SizedBox(height: AppConstants.spacingLG),
              child,
            ],
          ),
        ),
      ),
    );
  }
}
