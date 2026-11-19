import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'widgets/auth_page.dart';

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen>
    with ApiFormState<ForgotPasswordScreen> {
  final _email = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  // Backend odgovara isto za postojeći i nepostojeći email, pa i poruka to kaže
  Future<void> _submit() => submit(() async {
        final email = _email.text.trim();
        await ref.read(authRepositoryProvider).forgotPassword(email: email);
        if (mounted) {
          context.pushReplacement(
            AppRoutes.resetPassword,
            extra: EmailStep(
              email: email,
              notice: 'Ako račun s ovim emailom postoji, poslali smo kod za reset. Kod vrijedi 30 minuta.',
            ),
          );
        }
      });

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      title: 'Zaboravljena lozinka',
      subtitle: 'Unesite email računa i poslaćemo vam kod za novu lozinku.',
      child: Form(
        key: formKey,
        autovalidateMode: AutovalidateMode.onUserInteraction,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TorvinTextField(
              controller: _email,
              label: 'Email',
              hint: 'ime@domena.ba',
              keyboardType: TextInputType.emailAddress,
              textInputAction: TextInputAction.done,
              autofillHints: const [AutofillHints.email],
              onChanged: clearServerError,
              onFieldSubmitted: (_) => _submit(),
              forceErrorText: serverError('email'),
              validator: Validators.combine([
                Validators.required('Email je obavezan.'),
                Validators.maxLength(256, 'Email može imati najviše 256 znakova.'),
                Validators.email,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Pošalji kod', busy: busy, onPressed: _submit),
          ],
        ),
      ),
    );
  }
}
