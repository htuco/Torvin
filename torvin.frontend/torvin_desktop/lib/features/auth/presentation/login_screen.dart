import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:torvin_core/torvin_core.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  static const double maxWidth = 420;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  late final String? _notice = ref.read(sessionNoticeProvider).consume();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppConstants.spacingLG),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: LoginScreen.maxWidth),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(AppConstants.spacingXL),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text('Torvin Admin', style: theme.textTheme.headlineSmall),
                    const SizedBox(height: AppConstants.spacingXS),
                    Text('Prijava za administratore', style: theme.textTheme.bodyMedium),
                    const SizedBox(height: AppConstants.spacingLG),
                    LoginForm(app: ClientApp.desktop, notice: _notice),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
