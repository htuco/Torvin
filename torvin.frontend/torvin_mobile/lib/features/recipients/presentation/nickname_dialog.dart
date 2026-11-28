import 'package:flutter/material.dart';
import 'package:torvin_core/torvin_core.dart';

import '../data/recipient_models.dart';

/// Nadimak omiljenog primaoca. Vraća nadimak na "Sačuvaj" (prazno polje je `''` i briše nadimak), a `null` kad
/// korisnik odustane.
Future<String?> showNicknameDialog(BuildContext context, FavoriteRecipientResponse favorite) =>
    showTorvinDialog<String>(context, builder: (_) => _NicknameDialog(favorite: favorite));

class _NicknameDialog extends StatefulWidget {
  const _NicknameDialog({required this.favorite});

  final FavoriteRecipientResponse favorite;

  @override
  State<_NicknameDialog> createState() => _NicknameDialogState();
}

class _NicknameDialogState extends State<_NicknameDialog> {
  final _formKey = GlobalKey<FormState>();
  late final _nickname = TextEditingController(text: widget.favorite.nickname ?? '');

  @override
  void dispose() {
    _nickname.dispose();
    super.dispose();
  }

  void _save() {
    if (_formKey.currentState!.validate()) {
      Navigator.of(context).pop(_nickname.text.trim());
    }
  }

  @override
  Widget build(BuildContext context) {
    return TorvinDialog(
      icon: LucideIcons.pencil,
      title: 'Nadimak',
      message: 'Nadimak za primaoca ${widget.favorite.fullName}. Vidite ga samo vi.',
      confirmLabel: 'Sačuvaj',
      onConfirm: _save,
      child: Form(
        key: _formKey,
        child: TorvinTextField(
          controller: _nickname,
          label: 'Nadimak (opciono)',
          hint: 'npr. Cimerka',
          helper: 'Prazno polje briše nadimak.',
          textCapitalization: TextCapitalization.sentences,
          validator: Validators.nickname,
        ),
      ),
    );
  }
}
