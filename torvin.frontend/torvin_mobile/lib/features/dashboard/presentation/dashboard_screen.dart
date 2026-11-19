import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

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
        child: Text(
          session == null ? 'Početna' : 'Dobrodošli, ${session.firstName}',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
      ),
    );
  }
}
