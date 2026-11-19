import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../router/app_routes.dart';

/// Okvir admin aplikacije (DS v2): sidebar s modulima lijevo, top bar i sadržaj rute desno.
/// Moduli se dodaju u sidebar s taskovima koji ih prave.
class AdminShell extends ConsumerWidget {
  const AdminShell({super.key, required this.location, required this.child});

  final String location;
  final Widget child;

  static const List<_NavItem> _overview = [
    _NavItem(label: 'Početna', icon: LucideIcons.house, route: AppRoutes.dashboard),
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final current = _overview.where((item) => item.route == location).firstOrNull;

    return Scaffold(
      body: Row(
        children: [
          _Sidebar(location: location, items: _overview),
          Expanded(
            child: Column(
              children: [
                _TopBar(crumb: current?.label ?? ''),
                Expanded(child: child),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _NavItem {
  const _NavItem({required this.label, required this.icon, required this.route});

  final String label;
  final IconData icon;
  final String route;
}

class _Sidebar extends StatelessWidget {
  const _Sidebar({required this.location, required this.items});

  final String location;
  final List<_NavItem> items;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;

    return Container(
      width: AppConstants.sidebarWidth,
      padding: const EdgeInsets.symmetric(vertical: AppConstants.spacingLG, horizontal: AppConstants.spacingMD),
      decoration: BoxDecoration(
        color: colors.sidebar,
        border: Border(right: BorderSide(color: colors.sidebarBorder)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingSM),
            child: Row(
              children: [
                Container(
                  width: AppConstants.avatarSize,
                  height: AppConstants.avatarSize,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: theme.colorScheme.primary,
                    borderRadius: BorderRadius.circular(AppConstants.radiusIcon),
                  ),
                  child: Text('T', style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.onPrimary)),
                ),
                const SizedBox(width: AppConstants.spacingSM),
                Text.rich(
                  TextSpan(
                    text: 'Torvin ',
                    children: [
                      TextSpan(
                        text: 'Admin',
                        style: TextStyle(fontWeight: FontWeight.w500, color: theme.colorScheme.onSurfaceVariant),
                      ),
                    ],
                  ),
                  style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppConstants.spacingLG),
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppConstants.spacingRow,
              0,
              AppConstants.spacingRow,
              AppConstants.spacingSM,
            ),
            child: Text(
              'PREGLED',
              style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ),
          for (final item in items) _SidebarItem(item: item, selected: item.route == location),
        ],
      ),
    );
  }
}

class _SidebarItem extends StatelessWidget {
  const _SidebarItem({required this.item, required this.selected});

  final _NavItem item;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.torvinColors;
    final foreground = selected ? theme.colorScheme.onPrimaryContainer : colors.sidebarText;

    return Material(
      color: selected ? theme.colorScheme.primaryContainer : colors.sidebar,
      borderRadius: BorderRadius.circular(AppConstants.radiusControl),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppConstants.radiusControl),
        onTap: () => context.go(item.route),
        child: SizedBox(
          height: AppConstants.sidebarItemHeight,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingRow),
            child: Row(
              children: [
                Icon(item.icon, size: AppConstants.iconSize, color: foreground),
                const SizedBox(width: AppConstants.spacingRow),
                Text(
                  item.label,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: foreground,
                    fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TopBar extends ConsumerWidget {
  const _TopBar({required this.crumb});

  final String crumb;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final session = ref.watch(authControllerProvider).value;
    final secondary = theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant, fontWeight: FontWeight.w500);

    return Container(
      height: AppConstants.topBarHeight,
      padding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingXL),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerLowest,
        border: Border(bottom: BorderSide(color: scheme.outline)),
      ),
      child: Row(
        children: [
          Text('Torvin Admin', style: secondary),
          const SizedBox(width: AppConstants.spacingSM),
          Icon(LucideIcons.chevronRight, size: AppConstants.iconSize, color: scheme.onSurfaceVariant),
          const SizedBox(width: AppConstants.spacingSM),
          Text(crumb, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
          const Spacer(),
          if (session != null) ...[
            Container(
              width: AppConstants.avatarSize,
              height: AppConstants.avatarSize,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: scheme.onSurface, shape: BoxShape.circle),
              child: Text(
                '${session.firstName.characters.take(1)}${session.lastName.characters.take(1)}'.toUpperCase(),
                style: theme.textTheme.labelMedium?.copyWith(color: scheme.surface, fontWeight: FontWeight.w600),
              ),
            ),
            const SizedBox(width: AppConstants.spacingSM),
            Text(
              '${session.firstName} ${session.lastName}',
              style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500),
            ),
            const SizedBox(width: AppConstants.spacingMD),
          ],
          const ThemeModeButton(),
          const SizedBox(width: AppConstants.spacingSM),
          SizedBox(
            height: AppConstants.spacingLG,
            child: VerticalDivider(width: AppConstants.borderWidthThin, color: scheme.outline),
          ),
          const SizedBox(width: AppConstants.spacingSM),
          TextButton.icon(
            onPressed: () => ref.read(authControllerProvider.notifier).signOut(),
            icon: const Icon(LucideIcons.logOut, size: AppConstants.iconSize),
            label: const Text('Odjava'),
          ),
        ],
      ),
    );
  }
}
