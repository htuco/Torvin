import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_constants.dart';
import 'field_note.dart';

/// Kod iz emaila kao u dizajnu v2: šest kućica, a trenutna je obrubljena primarnom bojom.
///
/// Kućice samo prikazuju cifre. Unos ide kroz jedno nevidljivo polje preko njih, pa rade lijepljenje cijelog
/// koda, automatsko popunjavanje (`oneTimeCode`) i brisanje unazad bez skakanja između šest polja.
class OtpField extends StatefulWidget {
  const OtpField({
    super.key,
    required this.label,
    required this.controller,
    this.validator,
    this.forceErrorText,
    this.onChanged,
    this.onFieldSubmitted,
    this.textInputAction,
  });

  static const int length = 6;

  final String label;
  final TextEditingController controller;
  final FormFieldValidator<String>? validator;
  final String? forceErrorText;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onFieldSubmitted;
  final TextInputAction? textInputAction;

  @override
  State<OtpField> createState() => _OtpFieldState();
}

class _OtpFieldState extends State<OtpField> {
  final FocusNode _focus = FocusNode();

  @override
  void initState() {
    super.initState();
    // Aktivna kućica se crta samo dok je polje u fokusu
    _focus.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _focus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final validator = widget.validator;

    return FormField<String>(
      initialValue: widget.controller.text,
      forceErrorText: widget.forceErrorText,
      validator: validator == null ? null : (_) => validator(widget.controller.text),
      builder: (field) {
        final theme = Theme.of(context);
        final code = widget.controller.text;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.label, style: FieldNote.labelStyle(theme)),
            const SizedBox(height: AppConstants.spacingSM),
            SizedBox(
              width: OtpField.length * AppConstants.otpBoxWidth + (OtpField.length - 1) * AppConstants.spacingSM,
              height: AppConstants.otpBoxHeight,
              child: Stack(
                children: [
                  Row(
                    children: [
                      for (var i = 0; i < OtpField.length; i++) ...[
                        if (i > 0) const SizedBox(width: AppConstants.spacingSM),
                        _OtpBox(
                          digit: i < code.length ? code[i] : null,
                          active: _focus.hasFocus && i == code.length.clamp(0, OtpField.length - 1),
                          error: field.hasError,
                        ),
                      ],
                    ],
                  ),
                  Positioned.fill(
                    child: TextField(
                      controller: widget.controller,
                      focusNode: _focus,
                      keyboardType: TextInputType.number,
                      textInputAction: widget.textInputAction,
                      autofillHints: const [AutofillHints.oneTimeCode],
                      inputFormatters: [
                        FilteringTextInputFormatter.digitsOnly,
                        LengthLimitingTextInputFormatter(OtpField.length),
                      ],
                      // Polje je samo prijemnik unosa; cifre i kursor crtaju kućice ispod
                      showCursor: false,
                      enableInteractiveSelection: false,
                      style: const TextStyle(color: Colors.transparent),
                      decoration: const InputDecoration(
                        filled: false,
                        isCollapsed: true,
                        border: InputBorder.none,
                        enabledBorder: InputBorder.none,
                        focusedBorder: InputBorder.none,
                        constraints: BoxConstraints(),
                      ),
                      onChanged: (value) {
                        field.didChange(value);
                        widget.onChanged?.call(value);
                      },
                      onSubmitted: widget.onFieldSubmitted,
                    ),
                  ),
                ],
              ),
            ),
            FieldNote(error: field.errorText),
          ],
        );
      },
    );
  }
}

class _OtpBox extends StatelessWidget {
  const _OtpBox({required this.digit, required this.active, required this.error});

  final String? digit;
  final bool active;
  final bool error;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final digit = this.digit;

    final (color, width) = error
        ? (scheme.error, AppConstants.borderWidthMedium)
        : active
            ? (scheme.primary, AppConstants.borderWidthMedium)
            : (scheme.outline, AppConstants.borderWidthThin);

    return Container(
      width: AppConstants.otpBoxWidth,
      height: AppConstants.otpBoxHeight,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: scheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(AppConstants.radiusControl),
        border: Border.all(color: color, width: width),
      ),
      child: digit == null ? null : Text(digit, style: theme.textTheme.headlineSmall),
    );
  }
}
