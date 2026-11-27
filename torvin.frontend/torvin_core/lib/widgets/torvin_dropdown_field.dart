import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_constants.dart';
import 'field_note.dart';

/// Padajuća lista kao `TorvinTextField`: labela iznad, izbor u polju, a ispod objašnjenje ili greška s ikonom.
/// Stavke dolaze iz baze, a vrijednost je ID koji se ne prikazuje. Provjerava se poslije svog izbora, kao
/// `TorvinTextField`.
class TorvinDropdownField<T> extends FormField<T> {
  TorvinDropdownField({
    super.key,
    required String label,
    required List<DropdownMenuItem<T>> items,
    String? hint,
    String? helper,
    super.initialValue,
    super.validator,
    super.forceErrorText,
    super.autovalidateMode = AutovalidateMode.onUserInteraction,
    ValueChanged<T?>? onChanged,
    super.enabled,
  }) : super(
          builder: (field) {
            final theme = Theme.of(field.context);
            final inputTheme = theme.inputDecorationTheme;
            final hasError = field.errorText != null;

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(label, style: FieldNote.labelStyle(theme)),
                const SizedBox(height: AppConstants.spacingSM),
                InputDecorator(
                  textAlignVertical: TextAlignVertical.center,
                  isEmpty: field.value == null,
                  decoration: InputDecoration(
                    enabled: field.widget.enabled,
                    // Greška ide u `FieldNote`, pa se ivica bira ovdje, isto kao u `TorvinTextField`
                    enabledBorder: hasError ? inputTheme.errorBorder : null,
                  ).applyDefaults(inputTheme),
                  child: DropdownButtonHideUnderline(
                    child: DropdownButton<T>(
                      value: field.value,
                      items: items,
                      isExpanded: true,
                      isDense: true,
                      hint: hint == null ? null : Text(hint, style: inputTheme.hintStyle),
                      style: theme.textTheme.bodyLarge,
                      icon: Icon(
                        LucideIcons.chevronDown,
                        size: AppConstants.iconSize,
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                      dropdownColor: theme.colorScheme.surfaceContainerLowest,
                      borderRadius: BorderRadius.circular(AppConstants.radiusControl),
                      onChanged: field.widget.enabled
                          ? (value) {
                              field.didChange(value);
                              onChanged?.call(value);
                            }
                          : null,
                    ),
                  ),
                ),
                FieldNote(error: field.errorText, helper: helper),
              ],
            );
          },
        );
}
