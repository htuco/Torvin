import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../../app/router/app_routes.dart';
import 'widgets/auth_page.dart';

class VerifyEmailScreen extends ConsumerStatefulWidget {
  const VerifyEmailScreen({super.key, required this.step});

  final EmailStep step;

  @override
  ConsumerState<VerifyEmailScreen> createState() => _VerifyEmailScreenState();
}

class _VerifyEmailScreenState extends ConsumerState<VerifyEmailScreen> with ApiFormState<VerifyEmailScreen> {
  late final _email = TextEditingController(text: widget.step.email);
  final _code = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _code.dispose();
    super.dispose();
  }

  Future<void> _submit() => submit(() async {
        await ref.read(authRepositoryProvider).verifyEmail(email: _email.text.trim(), code: _code.text.trim());
        if (mounted) {
          context.go(AppRoutes.login, extra: 'Račun je verifikovan. Prijavite se.');
        }
      });

  @override
  Widget build(BuildContext context) {
    return AuthPage(
      title: 'Potvrda emaila',
      subtitle: 'Unesite kod od 6 cifara iz emaila. Kod vrijedi 15 minuta, '
          'a novi dobijate kad se pokušate prijaviti.',
      notice: widget.step.notice,
      child: Form(
        key: formKey,
        autovalidateMode: AutovalidateMode.onUserInteraction,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _email,
              decoration: const InputDecoration(labelText: 'Email', hintText: 'ime@domena.ba'),
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
            TextFormField(
              controller: _code,
              decoration: const InputDecoration(labelText: 'Kod iz emaila'),
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(6)],
              autofillHints: const [AutofillHints.oneTimeCode],
              textInputAction: TextInputAction.done,
              onChanged: clearServerError,
              onFieldSubmitted: (_) => _submit(),
              forceErrorText: serverError('code'),
              validator: Validators.combine([Validators.required('Kod je obavezan.'), Validators.code]),
            ),
            const SizedBox(height: AppConstants.spacingSM),
            FormErrorText(apiError?.message),
            const SizedBox(height: AppConstants.spacingLG),
            SubmitButton(label: 'Potvrdi email', busy: busy, onPressed: _submit),
          ],
        ),
      ),
    );
  }
}
