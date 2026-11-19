import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../forms/api_form_state.dart';
import '../../theme/app_constants.dart';
import '../../validation/validators.dart';
import '../../widgets/form_error_text.dart';
import '../../widgets/submit_button.dart';
import '../../widgets/torvin_text_field.dart';
import '../auth_providers.dart';
import '../client_app.dart';

/// Forma prijave za obje aplikacije. Poslije uspjeha router sam vodi na početnu, jer prati auth stanje.
class LoginForm extends ConsumerStatefulWidget {
  const LoginForm({super.key, required this.app, this.notice, this.links = const []});

  final ClientApp app;

  /// Poruka iznad forme, npr. "Račun je verifikovan. Prijavite se."
  final String? notice;

  /// Linkovi ispod dugmeta (registracija, zaboravljena lozinka...).
  final List<Widget> links;

  @override
  ConsumerState<LoginForm> createState() => _LoginFormState();
}

class _LoginFormState extends ConsumerState<LoginForm> with ApiFormState<LoginForm> {
  final _login = TextEditingController();
  final _password = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _login.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() => submit(
        () => ref
            .read(authControllerProvider.notifier)
            .signIn(login: _login.text.trim(), password: _password.text, app: widget.app),
      );

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final notice = widget.notice;

    return Form(
      key: formKey,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (notice != null) ...[
            Text(notice, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.primary)),
            const SizedBox(height: AppConstants.spacingMD),
          ],
          TorvinTextField(
            controller: _login,
            label: 'Korisničko ime ili email',
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.next,
            autofillHints: const [AutofillHints.username],
            onChanged: clearServerError,
            forceErrorText: serverError('login'),
            validator: Validators.combine([
              Validators.required('Korisničko ime ili email je obavezno.'),
              Validators.maxLength(256, 'Korisničko ime ili email može imati najviše 256 znakova.'),
            ]),
          ),
          const SizedBox(height: AppConstants.spacingMD),
          TorvinTextField(
            controller: _password,
            label: 'Lozinka',
            suffix: IconButton(
              tooltip: _obscurePassword ? 'Prikaži lozinku' : 'Sakrij lozinku',
              icon: Icon(_obscurePassword ? LucideIcons.eye : LucideIcons.eyeOff),
              onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
            ),
            obscureText: _obscurePassword,
            textInputAction: TextInputAction.done,
            autofillHints: const [AutofillHints.password],
            onChanged: clearServerError,
            onFieldSubmitted: (_) => _submit(),
            forceErrorText: serverError('password'),
            validator: Validators.combine([
              Validators.required('Lozinka je obavezna.'),
              Validators.maxLength(100, 'Lozinka može imati najviše 100 znakova.'),
            ]),
          ),
          const SizedBox(height: AppConstants.spacingSM),
          FormErrorText(apiError?.message),
          const SizedBox(height: AppConstants.spacingLG),
          SubmitButton(label: 'Prijavi se', busy: busy, onPressed: _submit),
          if (widget.links.isNotEmpty) ...[
            const SizedBox(height: AppConstants.spacingMD),
            ...widget.links,
          ],
        ],
      ),
    );
  }
}
