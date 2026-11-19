import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'widgets/auth_page.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> with ApiFormState<RegisterScreen> {
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _username = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();

  @override
  void dispose() {
    for (final controller in [_firstName, _lastName, _username, _email, _phone, _password, _confirmPassword]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() => submit(() async {
        final email = _email.text.trim();
        final phone = _phone.text.trim();
        final message = await ref.read(authRepositoryProvider).register(
              firstName: _firstName.text.trim(),
              lastName: _lastName.text.trim(),
              username: _username.text.trim(),
              email: email,
              phoneNumber: phone.isEmpty ? null : phone,
              password: _password.text,
              confirmPassword: _confirmPassword.text,
            );
        if (mounted) {
          context.pushReplacement(AppRoutes.verifyEmail, extra: EmailStep(email: email, notice: message));
        }
      });

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      title: 'Registracija',
      subtitle: 'Poslije registracije potvrdite email kodom koji vam pošaljemo.',
      child: Form(
        key: formKey,
        autovalidateMode: AutovalidateMode.onUserInteraction,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TorvinTextField(
              controller: _firstName,
              label: 'Ime',
              textCapitalization: TextCapitalization.words,
              textInputAction: TextInputAction.next,
              onChanged: clearServerError,
              forceErrorText: serverError('firstName'),
              validator: Validators.combine([
                Validators.required('Ime je obavezno.'),
                Validators.maxLength(50, 'Ime može imati najviše 50 znakova.'),
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _lastName,
              label: 'Prezime',
              textCapitalization: TextCapitalization.words,
              textInputAction: TextInputAction.next,
              onChanged: clearServerError,
              forceErrorText: serverError('lastName'),
              validator: Validators.combine([
                Validators.required('Prezime je obavezno.'),
                Validators.maxLength(50, 'Prezime može imati najviše 50 znakova.'),
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _username,
              label: 'Korisničko ime',
              helper: '3–50 znakova: slova, cifre, tačka i donja crta',
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.newUsername],
              onChanged: clearServerError,
              forceErrorText: serverError('username'),
              validator: Validators.combine([
                Validators.required('Korisničko ime je obavezno.'),
                Validators.username,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _email,
              label: 'Email',
              hint: 'ime@domena.ba',
              keyboardType: TextInputType.emailAddress,
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.email],
              onChanged: clearServerError,
              forceErrorText: serverError('email'),
              validator: Validators.combine([
                Validators.required('Email je obavezan.'),
                Validators.maxLength(256, 'Email može imati najviše 256 znakova.'),
                Validators.email,
              ]),
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _phone,
              label: 'Telefon (nije obavezan)',
              hint: '+387 61 234 567',
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.telephoneNumber],
              onChanged: clearServerError,
              forceErrorText: serverError('phoneNumber'),
              validator: Validators.phone,
            ),
            const SizedBox(height: AppConstants.spacingMD),
            TorvinTextField(
              controller: _password,
              label: 'Lozinka',
              helper: '8–100 znakova, veliko i malo slovo, broj i specijalni znak',
              obscureText: true,
              textInputAction: TextInputAction.next,
              autofillHints: const [AutofillHints.newPassword],
              onChanged: clearServerError,
              forceErrorText: serverError('password'),
              validator: Validators.combine([
                Validators.required('Lozinka je obavezna.'),
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
                Validators.matches(() => _password.text),
              ]),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Registruj se', busy: busy, onPressed: _submit),
          ],
        ),
      ),
    );
  }
}
