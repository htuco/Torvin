import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'widgets/auth_page.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key, this.notice});

  /// Poruka prethodnog koraka (verifikacija, reset). Bez nje se prikaže poruka o isteku prijave, ako postoji.
  final String? notice;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  late final String? _notice = widget.notice ?? ref.read(sessionNoticeProvider).consume();

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      title: 'Prijava',
      subtitle: 'Prijavite se u Torvin novčanik.',
      child: LoginForm(
        app: ClientApp.mobile,
        notice: _notice,
        links: [
          TextButton(
            onPressed: () => context.push(AppRoutes.forgotPassword),
            child: const Text('Zaboravljena lozinka?'),
          ),
          TextButton(
            onPressed: () => context.push(AppRoutes.register),
            child: const Text('Nemate račun? Registrujte se'),
          ),
          TextButton(
            onPressed: () => context.push(AppRoutes.verifyEmail),
            child: const Text('Imam kod za potvrdu emaila'),
          ),
        ],
      ),
    );
  }
}
