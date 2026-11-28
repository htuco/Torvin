import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:torvin_core/torvin_core.dart';

/// Polje pretrage: pretraga kreće tek kad korisnik stane s kucanjem, da svako slovo ne bude novi zahtjev.
/// `onSearch` dobija tekst bez razmaka na krajevima, a prazan tekst znači "bez pretrage". Polje se provjerava tek
/// pri slanju forme, jer bi greška dok korisnik kuca bila lažna.
class SearchField extends StatefulWidget {
  const SearchField({
    super.key,
    required this.label,
    required this.hint,
    required this.onSearch,
    this.controller,
    this.helper,
    this.validator,
    this.forceErrorText,
    this.suffix,
    this.onChanged,
  });

  /// Koliko se čeka poslije zadnjeg slova.
  static const Duration delay = Duration(milliseconds: 300);

  /// Najduža pretraga, isto kao `MaxLength` na `BaseSearchObject.Fts`.
  static const int maxLength = 100;

  final String label;
  final String hint;
  final ValueChanged<String> onSearch;

  /// Zadaje ga ekran koji pretragu briše (npr. "Ukloni filtere") ili čuva tekst dok je polje sakriveno.
  final TextEditingController? controller;
  final String? helper;
  final FormFieldValidator<String>? validator;
  final String? forceErrorText;

  /// Umjesto ikone pretrage (npr. dugme za QR kod).
  final Widget? suffix;

  /// Odmah pri svakoj izmjeni, prije zakašnjenja (npr. brisanje stare greške).
  final ValueChanged<String>? onChanged;

  @override
  State<SearchField> createState() => _SearchFieldState();
}

class _SearchFieldState extends State<SearchField> {
  late final TextEditingController _controller = widget.controller ?? TextEditingController();
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    if (widget.controller == null) {
      _controller.dispose();
    }
    super.dispose();
  }

  void _onChanged(String value) {
    widget.onChanged?.call(value);
    _debounce?.cancel();
    _debounce = Timer(SearchField.delay, () => widget.onSearch(value.trim()));
  }

  @override
  Widget build(BuildContext context) {
    return TorvinTextField(
      controller: _controller,
      label: widget.label,
      hint: widget.hint,
      helper: widget.helper,
      textInputAction: TextInputAction.search,
      inputFormatters: [LengthLimitingTextInputFormatter(SearchField.maxLength)],
      autovalidateMode: AutovalidateMode.disabled,
      validator: widget.validator,
      forceErrorText: widget.forceErrorText,
      onChanged: _onChanged,
      suffix: widget.suffix ?? const Icon(LucideIcons.search, size: AppConstants.iconSize),
    );
  }
}
