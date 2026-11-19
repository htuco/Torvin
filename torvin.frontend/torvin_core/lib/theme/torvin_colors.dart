import 'package:flutter/material.dart';

/// Boje iz dizajn sistema v2 koje `ColorScheme` nema: statusi, sidebar, tabela, grafikoni i kartice.
///
/// Statusna boja ima tri nijanse: `*Dot` je puna (tačka, traka napretka, ikona), `*Text` je tamnija
/// za tekst i iznose na pozadini, a `*Chip` je tekst na tintu jer mora imati kontrast ≥ 4,5:1.
@immutable
class TorvinColors extends ThemeExtension<TorvinColors> {
  const TorvinColors({
    required this.successDot,
    required this.successText,
    required this.successTint,
    required this.successChip,
    required this.warningDot,
    required this.warningText,
    required this.warningTint,
    required this.warningChip,
    required this.errorDot,
    required this.errorText,
    required this.errorTint,
    required this.errorChip,
    required this.neutralTint,
    required this.neutralChip,
    required this.sidebar,
    required this.sidebarText,
    required this.sidebarBorder,
    required this.tableHeader,
    required this.tableZebra,
    required this.charts,
    required this.cardPrimary,
    required this.cardDark,
    required this.cardSlate,
    required this.cardBlocked,
    required this.balanceGradient,
  });

  final Color successDot;
  final Color successText;
  final Color successTint;
  final Color successChip;
  final Color warningDot;
  final Color warningText;
  final Color warningTint;
  final Color warningChip;
  final Color errorDot;
  final Color errorText;
  final Color errorTint;
  final Color errorChip;
  final Color neutralTint;
  final Color neutralChip;
  final Color sidebar;
  final Color sidebarText;
  final Color sidebarBorder;
  final Color tableHeader;
  final Color tableZebra;

  /// Pet boja serija grafikona, redom.
  final List<Color> charts;

  final Color cardPrimary;
  final Color cardDark;
  final Color cardSlate;
  final Color cardBlocked;

  /// Jedini gradijent u dizajnu: kartica ukupnog balansa.
  final List<Color> balanceGradient;

  static const TorvinColors light = TorvinColors(
    successDot: Color(0xFF2E9E5B),
    successText: Color(0xFF23824A),
    successTint: Color(0xFFE7F4EC),
    successChip: Color(0xFF1F7141),
    warningDot: Color(0xFFD69200),
    warningText: Color(0xFF8F6400),
    warningTint: Color(0xFFFBF2DE),
    warningChip: Color(0xFF7A5500),
    errorDot: Color(0xFFC10007),
    errorText: Color(0xFFC10007),
    errorTint: Color(0xFFFEF2F2),
    errorChip: Color(0xFFC10007),
    neutralTint: Color(0xFFF1F5F9),
    neutralChip: Color(0xFF45556C),
    sidebar: Color(0xFFF5F5F5),
    sidebarText: Color(0xFF262626),
    sidebarBorder: Color(0xFFE2E8F0),
    tableHeader: Color(0xFFF1F5F9),
    tableZebra: Color(0xFFF8FAFC),
    // Dizajn sistem daje paletu grafikona samo za tamnu temu, pa su ovo boje koje već koriste admin ekrani
    charts: [Color(0xFFE7000B), Color(0xFFD69200), Color(0xFF45556C), Color(0xFF2E9E5B), Color(0xFF90A1B9)],
    cardPrimary: Color(0xFFE7000B),
    cardDark: Color(0xFF262626),
    cardSlate: Color(0xFF45556C),
    cardBlocked: Color(0xFF62748E),
    balanceGradient: [Color(0xFF0A0A0A), Color(0xFF262626)],
  );

  static const TorvinColors dark = TorvinColors(
    successDot: Color(0xFF2E9E5B),
    successText: Color(0xFF4CB332),
    successTint: Color(0xFF0F2A1A),
    successChip: Color(0xFF4CB332),
    warningDot: Color(0xFFD69200),
    warningText: Color(0xFFE5A823),
    warningTint: Color(0xFF2E2410),
    warningChip: Color(0xFFE5A823),
    errorDot: Color(0xFFFF6467),
    errorText: Color(0xFFFF6467),
    errorTint: Color(0xFF2E0F11),
    errorChip: Color(0xFFFF6467),
    neutralTint: Color(0xFF27272A),
    neutralChip: Color(0xFFD4D4D4),
    sidebar: Color(0xFF171717),
    sidebarText: Color(0xFFE5E5E5),
    sidebarBorder: Color(0xFF404040),
    tableHeader: Color(0xFF27272A),
    tableZebra: Color(0xFF18181B),
    charts: [Color(0xFFFF6662), Color(0xFFFF9710), Color(0xFF00BCC8), Color(0xFF4CB332), Color(0xFF9499FA)],
    cardPrimary: Color(0xFFE7000B),
    cardDark: Color(0xFF262626),
    cardSlate: Color(0xFF45556C),
    cardBlocked: Color(0xFF62748E),
    balanceGradient: [Color(0xFF18181B), Color(0xFF262626)],
  );

  @override
  TorvinColors copyWith({
    Color? successDot,
    Color? successText,
    Color? successTint,
    Color? successChip,
    Color? warningDot,
    Color? warningText,
    Color? warningTint,
    Color? warningChip,
    Color? errorDot,
    Color? errorText,
    Color? errorTint,
    Color? errorChip,
    Color? neutralTint,
    Color? neutralChip,
    Color? sidebar,
    Color? sidebarText,
    Color? sidebarBorder,
    Color? tableHeader,
    Color? tableZebra,
    List<Color>? charts,
    Color? cardPrimary,
    Color? cardDark,
    Color? cardSlate,
    Color? cardBlocked,
    List<Color>? balanceGradient,
  }) {
    return TorvinColors(
      successDot: successDot ?? this.successDot,
      successText: successText ?? this.successText,
      successTint: successTint ?? this.successTint,
      successChip: successChip ?? this.successChip,
      warningDot: warningDot ?? this.warningDot,
      warningText: warningText ?? this.warningText,
      warningTint: warningTint ?? this.warningTint,
      warningChip: warningChip ?? this.warningChip,
      errorDot: errorDot ?? this.errorDot,
      errorText: errorText ?? this.errorText,
      errorTint: errorTint ?? this.errorTint,
      errorChip: errorChip ?? this.errorChip,
      neutralTint: neutralTint ?? this.neutralTint,
      neutralChip: neutralChip ?? this.neutralChip,
      sidebar: sidebar ?? this.sidebar,
      sidebarText: sidebarText ?? this.sidebarText,
      sidebarBorder: sidebarBorder ?? this.sidebarBorder,
      tableHeader: tableHeader ?? this.tableHeader,
      tableZebra: tableZebra ?? this.tableZebra,
      charts: charts ?? this.charts,
      cardPrimary: cardPrimary ?? this.cardPrimary,
      cardDark: cardDark ?? this.cardDark,
      cardSlate: cardSlate ?? this.cardSlate,
      cardBlocked: cardBlocked ?? this.cardBlocked,
      balanceGradient: balanceGradient ?? this.balanceGradient,
    );
  }

  @override
  TorvinColors lerp(TorvinColors? other, double t) {
    if (other == null) {
      return this;
    }

    Color mix(Color a, Color b) => Color.lerp(a, b, t)!;
    List<Color> mixAll(List<Color> a, List<Color> b) => [for (var i = 0; i < a.length; i++) mix(a[i], b[i])];

    return TorvinColors(
      successDot: mix(successDot, other.successDot),
      successText: mix(successText, other.successText),
      successTint: mix(successTint, other.successTint),
      successChip: mix(successChip, other.successChip),
      warningDot: mix(warningDot, other.warningDot),
      warningText: mix(warningText, other.warningText),
      warningTint: mix(warningTint, other.warningTint),
      warningChip: mix(warningChip, other.warningChip),
      errorDot: mix(errorDot, other.errorDot),
      errorText: mix(errorText, other.errorText),
      errorTint: mix(errorTint, other.errorTint),
      errorChip: mix(errorChip, other.errorChip),
      neutralTint: mix(neutralTint, other.neutralTint),
      neutralChip: mix(neutralChip, other.neutralChip),
      sidebar: mix(sidebar, other.sidebar),
      sidebarText: mix(sidebarText, other.sidebarText),
      sidebarBorder: mix(sidebarBorder, other.sidebarBorder),
      tableHeader: mix(tableHeader, other.tableHeader),
      tableZebra: mix(tableZebra, other.tableZebra),
      charts: mixAll(charts, other.charts),
      cardPrimary: mix(cardPrimary, other.cardPrimary),
      cardDark: mix(cardDark, other.cardDark),
      cardSlate: mix(cardSlate, other.cardSlate),
      cardBlocked: mix(cardBlocked, other.cardBlocked),
      balanceGradient: mixAll(balanceGradient, other.balanceGradient),
    );
  }
}

extension TorvinColorsContext on BuildContext {
  /// Boje teme koje `ColorScheme` nema. Tema ih uvijek registruje, pa nikad nisu `null`.
  TorvinColors get torvinColors => Theme.of(this).extension<TorvinColors>()!;
}
