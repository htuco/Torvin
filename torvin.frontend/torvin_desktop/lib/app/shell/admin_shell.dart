import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../router/app_routes.dart';

/// Okvir admin aplikacije: sidebar s modulima lijevo, sadržaj rute desno.
/// Moduli se dodaju u sidebar s taskovima koji ih prave.
class AdminShell extends ConsumerWidget {
  const AdminShell({super.key, required this.location, required this.child});

  static const double sidebarWidth = 240;

  final String location;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final session = ref.watch(authControllerProvider).value;

    return Scaffold(
      body: Row(
        children: [
          SizedBox(
            width: sidebarWidth,
            child: ColoredBox(
              color: theme.colorScheme.surfaceContainerLow,
              child: Padding(
                padding: const EdgeInsets.all(AppConstants.spacingMD),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text('Torvin Admin', style: theme.textTheme.titleLarge),
                    if (session != null)
                      Text('${session.firstName} ${session.lastName}', style: theme.textTheme.bodySmall),
                    const SizedBox(height: AppConstants.spacingLG),
                    ListTile(
                      leading: const Icon(Icons.home_outlined),
                      title: const Text('Početna'),
                      selected: location == AppRoutes.dashboard,
                      onTap: () => context.go(AppRoutes.dashboard),
                    ),
                    const Spacer(),
                    ListTile(
                      leading: const Icon(Icons.logout),
                      title: const Text('Odjava'),
                      onTap: () => ref.read(authControllerProvider.notifier).signOut(),
                    ),
                  ],
                ),
              ),
            ),
          ),
          const VerticalDivider(width: AppConstants.borderWidthThin),
          Expanded(child: child),
        ],
      ),
    );
  }
}
