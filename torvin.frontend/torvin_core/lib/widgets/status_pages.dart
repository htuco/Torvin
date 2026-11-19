import 'package:flutter/material.dart';

import 'package:lucide_icons_flutter/lucide_icons.dart';

import 'empty_state.dart';

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
      body: EmptyState(
        icon: LucideIcons.searchX,
        title: 'Stranica ne postoji.',
        message: 'Adresa je pogrešna ili stranica više nije dostupna.',
        actionLabel: 'Na početnu',
        onAction: onBack,
      ),
    );
  }
}
