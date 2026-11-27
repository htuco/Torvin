import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_constants.dart';
import 'field_note.dart';

/// Polje forme kao u dizajnu v2: labela iznad polja, placeholder u polju, a ispod objašnjenje formata ili greška
/// s ikonom. Labela se ne smanjuje u ivicu polja kao kod Material `labelText`.
///
/// Radi kao `TextFormField`: validator i `forceErrorText` (greška s backenda). Polje se provjerava poslije svoje
/// izmjene, a ne kad korisnik mijenja drugo polje, pa prazno polje koje još nije dirano ne javlja grešku. Sva polja
/// se provjere pri slanju forme (`formKey.currentState!.validate()`).
class TorvinTextField extends FormField<String> {
  TorvinTextField({
    super.key,
    required String label,
    required TextEditingController controller,
    String? hint,
    String? helper,
    FormFieldValidator<String>? validator,
    super.forceErrorText,
    super.autovalidateMode = AutovalidateMode.onUserInteraction,
    ValueChanged<String>? onChanged,
    ValueChanged<String>? onFieldSubmitted,
    TextInputType? keyboardType,
    TextInputAction? textInputAction,
    TextCapitalization textCapitalization = TextCapitalization.none,
    List<TextInputFormatter>? inputFormatters,
    Iterable<String>? autofillHints,
    bool obscureText = false,
    Widget? suffix,
    super.enabled,
  }) : super(
          initialValue: controller.text,
          // Validira se tekst iz controllera, pa je provjera tačna i kad ekran sam upiše vrijednost (npr. email)
          validator: validator == null ? null : (_) => validator(controller.text),
          builder: (field) {
            final theme = Theme.of(field.context);
            final inputTheme = theme.inputDecorationTheme;
            final hasError = field.errorText != null;

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(label, style: FieldNote.labelStyle(theme)),
                const SizedBox(height: AppConstants.spacingSM),
                TextField(
                  controller: controller,
                  enabled: enabled,
                  obscureText: obscureText,
                  keyboardType: keyboardType,
                  textInputAction: textInputAction,
                  textCapitalization: textCapitalization,
                  inputFormatters: inputFormatters,
                  autofillHints: autofillHints,
                  style: theme.textTheme.bodyLarge,
                  onChanged: (value) {
                    field.didChange(value);
                    onChanged?.call(value);
                  },
                  onSubmitted: onFieldSubmitted,
                  decoration: InputDecoration(
                    hintText: hint,
                    suffixIcon: suffix,
                    // Greška ne ide u Material red ispod polja nego u `FieldNote`, pa se ivica bira ovdje
                    enabledBorder: hasError ? inputTheme.errorBorder : null,
                    focusedBorder: hasError ? inputTheme.focusedErrorBorder : null,
                  ),
                ),
                FieldNote(error: field.errorText, helper: helper),
              ],
            );
          },
        );
}
