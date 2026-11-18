import 'package:flutter/material.dart';

import '../theme/app_constants.dart';

/// Prikazuje se dok se sesija čita iz skladišta, prije nego što router zna kuda ide.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(body: Center(child: CircularProgressIndicator()));
}

/// Ruta koja ne postoji. `onBack` vodi na početnu, jer core ne zna rute aplikacije.
class NotFoundPage extends StatelessWidget {
  const NotFoundPage({super.key, required this.onBack});

  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('Stranica ne postoji.', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: AppConstants.spacingMD),
            FilledButton(onPressed: onBack, child: const Text('Na početnu')),
          ],
        ),
      ),
    );
  }
}
