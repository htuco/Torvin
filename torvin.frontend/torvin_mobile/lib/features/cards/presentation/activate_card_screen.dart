import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../data/card_providers.dart';

/// Aktivacija kartice kodom iz emaila. Otvara se poslije dodavanja ili iz detalja kartice koja čeka kod.
class ActivateCardScreen extends ConsumerStatefulWidget {
  const ActivateCardScreen({super.key, required this.cardId});

  final int cardId;

  @override
  ConsumerState<ActivateCardScreen> createState() => _ActivateCardScreenState();
}

class _ActivateCardScreenState extends ConsumerState<ActivateCardScreen> with ApiFormState<ActivateCardScreen> {
  final _code = TextEditingController();
  bool _resending = false;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _submit() => submit(() async {
        final card = await ref.read(cardsRepositoryProvider).verify(widget.cardId, code: _code.text.trim());
        if (!mounted) {
          return;
        }
        refreshCards(ref, cardId: card.id);
        showTorvinSnackBar(context, 'Kartica ${card.maskedNumber} je aktivirana.');
        // Nazad tamo odakle je došao: na listu poslije dodavanja, ili na detalje kartice
        context.pop();
      });

  Future<void> _resend() async {
    setState(() => _resending = true);
    try {
      await ref.read(cardsRepositoryProvider).resendCode(widget.cardId);
      if (mounted) {
        showTorvinSnackBar(context, 'Poslali smo novi kod na vaš email.');
      }
    } on ApiException catch (error) {
      if (mounted) {
        showTorvinSnackBar(context, error.message);
      }
    } finally {
      if (mounted) {
        setState(() => _resending = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Aktivacija kartice')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppConstants.spacingLG),
        child: Form(
          key: formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Unesite kod od 6 cifara koji smo poslali na vaš email. Kod vrijedi 15 minuta.',
                style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: AppConstants.spacingLG),
              OtpField(
                controller: _code,
                label: 'Kod iz emaila',
                textInputAction: TextInputAction.done,
                onChanged: clearServerError,
                onFieldSubmitted: (_) => _submit(),
                forceErrorText: serverError('code'),
                validator: Validators.combine([Validators.required('Kod je obavezan.'), Validators.code]),
              ),
              const SizedBox(height: AppConstants.spacingSM),
              FormErrorText(apiError?.message),
              const SizedBox(height: AppConstants.spacingLG),
              SubmitButton(label: 'Aktiviraj', busy: busy, onPressed: _submit),
              const SizedBox(height: AppConstants.spacingSM),
              TextButton(onPressed: _resending ? null : _resend, child: const Text('Pošalji novi kod')),
            ],
          ),
        ),
      ),
    );
  }
}
