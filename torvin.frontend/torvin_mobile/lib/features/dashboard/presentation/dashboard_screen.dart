import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).value;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Torvin'),
        actions: [
          const ThemeModeButton(),
          IconButton(
            tooltip: 'Odjava',
            icon: const Icon(LucideIcons.logOut),
            onPressed: () => ref.read(authControllerProvider.notifier).signOut(),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(AppConstants.spacingLG),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              session == null ? 'Početna' : 'Dobrodošli, ${session.firstName}',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: AppConstants.spacingLG),
            OutlinedButton.icon(
              onPressed: () => context.push(AppRoutes.cards),
              icon: const Icon(LucideIcons.creditCard),
              label: const Text('Moje kartice'),
            ),
            const SizedBox(height: AppConstants.spacingRow),
            FilledButton.icon(
              onPressed: () => context.push(AppRoutes.newTransaction),
              icon: const Icon(LucideIcons.send),
              label: const Text('Pošalji novac'),
            ),
          ],
        ),
      ),
    );
  }
}
