import 'package:flutter/widgets.dart';

import '../api/api_exception.dart';

/// Zajednički tok forme koja šalje podatke na API: klijentska validacija, zahtjev, greška s backenda.
/// Greška polja ide u `forceErrorText` tog polja, a opšta poruka u `FormErrorText` ispod forme.
mixin ApiFormState<T extends StatefulWidget> on State<T> {
  final formKey = GlobalKey<FormState>();
  bool busy = false;
  ApiException? apiError;

  String? serverError(String field) => apiError?.fieldError(field);

  /// Serverska greška se briše pri prvoj izmjeni, jer `forceErrorText` inače blokira ponovno slanje.
  void clearServerError(String _) {
    if (apiError != null) {
      setState(() => apiError = null);
    }
  }

  /// Pokreće `action` samo ako je forma ispravna i ako zahtjev već ne traje.
  Future<void> submit(Future<void> Function() action) async {
    if (busy || !formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      busy = true;
      apiError = null;
    });
    try {
      await action();
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => apiError = error);
      }
    } finally {
      if (mounted) {
        setState(() => busy = false);
      }
    }
  }
}
