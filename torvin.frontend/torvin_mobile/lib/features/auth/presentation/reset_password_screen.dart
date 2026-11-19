import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'widgets/auth_page.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key, required this.step});

  final EmailStep step;

  @override
  ConsumerState<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen>
    with ApiFormState<ResetPasswordScreen> {
  late final _email = TextEditingController(text: widget.step.email);
  final _code = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirmPassword = TextEditingController();

  @override
  void dispose() {
    for (final controller in [_email, _code, _newPassword, _confirmPassword]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() => submit(() async {
        await ref.read(authRepositoryProvider).resetPassword(
              email: _email.text.trim(),
              code: _code.text.trim(),
              newPassword: _newPassword.text,
              confirmPassword: _confirmPassword.text,
            );
        if (mounted) {
          context.go(AppRoutes.login, extra: 'Lozinka je promijenjena. Prijavite se novom lozinkom.');
        }
      });

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      title: 'Nova lozinka',
      subtitle: 'Unesite kod iz emaila i novu lozinku.',
      notice: widget.step.notice,
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
              textInputAction: TextInputAction.next,
              onChanged: clearServerError,
              forceErrorText: serverError('email'),
              validator: Validators.combine([
                Validators.required('Email je obavezan.'),
                Validators.maxLength(256, 'Email može imati najviše 256 znakova.'),
                Validators.email,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            OtpField(
              controller: _code,
              label: 'Kod iz emaila',
              textInputAction: TextInputAction.next,
              onChanged: clearServerError,
              forceErrorText: serverError('code'),
              validator: Validators.combine([Validators.required('Kod je obavezan.'), Validators.code]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _newPassword,
              label: 'Nova lozinka',
              helper: '8–100 znakova, veliko i malo slovo, broj i specijalni znak',
              obscureText: true,
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.newPassword],
              onChanged: clearServerError,
              forceErrorText: serverError('newPassword'),
              validator: Validators.combine([
                Validators.required('Nova lozinka je obavezna.'),
                Validators.strongPassword,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _confirmPassword,
              label: 'Potvrda lozinke',
              obscureText: true,
              textInputAction: TextInputAction.done,
              onChanged: clearServerError,
              onFieldSubmitted: (_) => _submit(),
              forceErrorText: serverError('confirmPassword'),
              validator: Validators.combine([
                Validators.required('Potvrda lozinke je obavezna.'),
                Validators.matches(() => _newPassword.text),
              ]),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Promijeni lozinku', busy: busy, onPressed: _submit),
          ],
        ),
      ),
    );
  }
}
