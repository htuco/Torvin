import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

/// Krug s inicijalima osobe (primalac, druga strana transakcije), u tintu primarne kao u dizajnu.
class InitialsAvatar extends StatelessWidget {
  const InitialsAvatar({
    super.key,
    required this.firstName,
    required this.lastName,
    this.size = AppConstants.listIconBox,
  });

  final String firstName;
  final String lastName;
  final double size;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final initials = [firstName, lastName].where((part) => part.isNotEmpty).map((part) => part[0]).join().toUpperCase();

    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(color: theme.colorScheme.primaryContainer, shape: BoxShape.circle),
      child: Text(
        initials,
        style: theme.textTheme.bodySmall?.copyWith(
          fontWeight: FontWeight.w600,
          color: theme.colorScheme.onPrimaryContainer,
        ),
      ),
    );
  }
}
