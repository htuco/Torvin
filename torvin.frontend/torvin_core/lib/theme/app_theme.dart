import 'package:flutter/material.dart';

import 'app_constants.dart';
import 'torvin_colors.dart';

/// Svijetla i tamna tema iz dizajn sistema v2 (tema „Crvena”).
///
/// Površine su pune boje: nijedno stanje (hover, pritisak, onemogućeno) ne koristi providnost.
class AppTheme {
  AppTheme._();

  /// Font je spakovan u `torvin_core`, pa se referencira s prefiksom paketa.
  static const String fontFamily = 'packages/torvin_core/Inter';

  /// Mobilna: dugmad i inputi visine 48.
  static ThemeData get lightTheme => theme(lightScheme(), TorvinColors.light, AppConstants.buttonHeight);
  static ThemeData get darkTheme => theme(darkScheme(), TorvinColors.dark, AppConstants.buttonHeight);

  /// Desktop: dugmad i inputi visine 40.
  static ThemeData get desktopLightTheme => theme(lightScheme(), TorvinColors.light, AppConstants.buttonHeightDesktop);
  static ThemeData get desktopDarkTheme => theme(darkScheme(), TorvinColors.dark, AppConstants.buttonHeightDesktop);

  static ColorScheme lightScheme() {
    return const ColorScheme(
      brightness: Brightness.light,
      primary: Color(0xFFE7000B),
      onPrimary: Color(0xFFFFFFFF),
      primaryContainer: Color(0xFFFEF2F2),
      onPrimaryContainer: Color(0xFFC10007),
      secondary: Color(0xFFF5F5F5),
      onSecondary: Color(0xFF262626),
      secondaryContainer: Color(0xFFF5F5F5),
      onSecondaryContainer: Color(0xFF262626),
      tertiary: Color(0xFF45556C),
      onTertiary: Color(0xFFFFFFFF),
      error: Color(0xFFC10007),
      onError: Color(0xFFFFFFFF),
      errorContainer: Color(0xFFFEF2F2),
      onErrorContainer: Color(0xFFC10007),
      surface: Color(0xFFFAFAFA),
      onSurface: Color(0xFF0A0A0A),
      onSurfaceVariant: Color(0xFF62748E),
      surfaceContainerLowest: Color(0xFFFDFDFD),
      surfaceContainerLow: Color(0xFFFDFDFD),
      surfaceContainer: Color(0xFFF1F5F9),
      surfaceContainerHigh: Color(0xFFF1F5F9),
      surfaceContainerHighest: Color(0xFFE2E8F0),
      outline: Color(0xFFE2E8F0),
      outlineVariant: Color(0xFFE2E8F0),
      shadow: Color(0xFF000000),
      scrim: Color(0xFF0A0A0A),
      inverseSurface: Color(0xFF0A0A0A),
      onInverseSurface: Color(0xFFFAFAFA),
      inversePrimary: Color(0xFFFB2C36),
      surfaceTint: Colors.transparent,
    );
  }

  static ColorScheme darkScheme() {
    return const ColorScheme(
      brightness: Brightness.dark,
      primary: Color(0xFFFB2C36),
      // Bijeli tekst na #FB2C36 ima kontrast 3,65:1, pa je tekst na primarnoj i destructive tamni (DS v2)
      onPrimary: Color(0xFF0A0A0A),
      primaryContainer: Color(0xFF2E0F11),
      onPrimaryContainer: Color(0xFFFF6467),
      secondary: Color(0xFF262626),
      onSecondary: Color(0xFFFAFAFA),
      secondaryContainer: Color(0xFF262626),
      onSecondaryContainer: Color(0xFFFAFAFA),
      tertiary: Color(0xFF90A1B9),
      onTertiary: Color(0xFF0A0A0A),
      error: Color(0xFFFF6467),
      onError: Color(0xFF0A0A0A),
      errorContainer: Color(0xFF2E0F11),
      onErrorContainer: Color(0xFFFF6467),
      surface: Color(0xFF0A0A0A),
      onSurface: Color(0xFFFAFAFA),
      onSurfaceVariant: Color(0xFFA1A1A1),
      surfaceContainerLowest: Color(0xFF18181B),
      surfaceContainerLow: Color(0xFF18181B),
      surfaceContainer: Color(0xFF262626),
      surfaceContainerHigh: Color(0xFF27272A),
      surfaceContainerHighest: Color(0xFF404040),
      outline: Color(0xFF404040),
      outlineVariant: Color(0xFF404040),
      shadow: Color(0xFF000000),
      scrim: Color(0xFF000000),
      inverseSurface: Color(0xFFFAFAFA),
      onInverseSurface: Color(0xFF0A0A0A),
      inversePrimary: Color(0xFFE7000B),
      surfaceTint: Colors.transparent,
    );
  }

  /// Boja primarnog dugmeta pod mišem ili pritisnutog: puna, tamnija nijansa umjesto providnog sloja.
  static Color _primaryPressed(ColorScheme scheme) =>
      scheme.brightness == Brightness.light ? const Color(0xFFC10007) : const Color(0xFFE7000B);

  static ThemeData theme(ColorScheme scheme, TorvinColors colors, double controlHeight) {
    final textTheme = _textTheme.apply(bodyColor: scheme.onSurface, displayColor: scheme.onSurface);
    final controlShape = RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppConstants.radiusControl));
    final controlSize = WidgetStatePropertyAll(Size(controlHeight, controlHeight));
    const controlPadding = WidgetStatePropertyAll(EdgeInsets.symmetric(horizontal: AppConstants.spacingLG));
    const noOverlay = WidgetStatePropertyAll<Color>(Colors.transparent);

    return ThemeData(
      useMaterial3: true,
      brightness: scheme.brightness,
      colorScheme: scheme,
      fontFamily: fontFamily,
      textTheme: textTheme,
      primaryTextTheme: textTheme,
      scaffoldBackgroundColor: scheme.surface,
      canvasColor: scheme.surface,
      splashFactory: NoSplash.splashFactory,
      hoverColor: scheme.surfaceContainer,
      highlightColor: Colors.transparent,
      extensions: [colors],
      filledButtonTheme: FilledButtonThemeData(
        style: ButtonStyle(
          minimumSize: controlSize,
          padding: controlPadding,
          shape: WidgetStatePropertyAll(controlShape),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
          elevation: const WidgetStatePropertyAll(0),
          overlayColor: noOverlay,
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return scheme.surfaceContainerHigh;
            }
            if (states.contains(WidgetState.hovered) || states.contains(WidgetState.pressed)) {
              return _primaryPressed(scheme);
            }
            return scheme.primary;
          }),
          foregroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.disabled) ? scheme.onSurfaceVariant : scheme.onPrimary,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: ButtonStyle(
          minimumSize: controlSize,
          padding: controlPadding,
          shape: WidgetStatePropertyAll(controlShape),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
          overlayColor: noOverlay,
          side: WidgetStatePropertyAll(BorderSide(color: scheme.outline)),
          backgroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.hovered) ? scheme.surfaceContainer : scheme.surfaceContainerLowest,
          ),
          foregroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.disabled) ? scheme.onSurfaceVariant : scheme.onSurface,
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: ButtonStyle(
          minimumSize: controlSize,
          shape: WidgetStatePropertyAll(controlShape),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
          overlayColor: noOverlay,
          backgroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.hovered) ? scheme.primaryContainer : Colors.transparent,
          ),
          foregroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.disabled) ? scheme.onSurfaceVariant : scheme.primary,
          ),
        ),
      ),
      iconButtonTheme: IconButtonThemeData(
        style: ButtonStyle(
          shape: WidgetStatePropertyAll(controlShape),
          overlayColor: noOverlay,
          iconSize: const WidgetStatePropertyAll(AppConstants.iconSize),
          backgroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.hovered) ? scheme.surfaceContainer : Colors.transparent,
          ),
          foregroundColor: WidgetStatePropertyAll(scheme.onSurface),
        ),
      ),
      // Labela, objašnjenje i greška su van polja (`TorvinTextField`), pa ovdje nema labelStyle ni errorStyle
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        // Neaktivno polje je muted (#F1F5F9), a aktivno ima boju kartice
        fillColor: WidgetStateColor.resolveWith(
          (states) =>
              states.contains(WidgetState.disabled) ? scheme.surfaceContainerHigh : scheme.surfaceContainerLowest,
        ),
        hoverColor: Colors.transparent,
        constraints: BoxConstraints.tightFor(height: controlHeight),
        contentPadding: const EdgeInsets.symmetric(horizontal: AppConstants.spacingMD),
        hintStyle: textTheme.bodyLarge?.copyWith(color: scheme.onSurfaceVariant),
        suffixIconColor: scheme.onSurfaceVariant,
        prefixIconColor: scheme.onSurfaceVariant,
        border: _inputBorder(scheme.outline, AppConstants.borderWidthThin),
        enabledBorder: _inputBorder(scheme.outline, AppConstants.borderWidthThin),
        disabledBorder: _inputBorder(scheme.outline, AppConstants.borderWidthThin),
        focusedBorder: _inputBorder(scheme.primary, AppConstants.borderWidthMedium),
        // DS: polje s greškom ima ivicu 2 px i kad nije u fokusu
        errorBorder: _inputBorder(scheme.error, AppConstants.borderWidthMedium),
        focusedErrorBorder: _inputBorder(scheme.error, AppConstants.borderWidthMedium),
      ),
      cardTheme: CardThemeData(
        color: scheme.surfaceContainerLowest,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.radiusCard),
          side: BorderSide(color: scheme.outline),
        ),
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: scheme.surface,
        foregroundColor: scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: textTheme.titleLarge,
      ),
      listTileTheme: ListTileThemeData(
        shape: controlShape,
        iconColor: scheme.onSurfaceVariant,
        textColor: scheme.onSurface,
        selectedColor: scheme.onPrimaryContainer,
        selectedTileColor: scheme.primaryContainer,
      ),
      // Filter čip: neizabran ima boju kartice i ivicu, a izabran je pun tamni, bez kvačice
      chipTheme: ChipThemeData(
        showCheckmark: false,
        side: WidgetStateBorderSide.resolveWith(
          (states) => states.contains(WidgetState.selected) ? BorderSide.none : BorderSide(color: scheme.outline),
        ),
        shape: const StadiumBorder(),
        color: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected) ? scheme.inverseSurface : scheme.surfaceContainerLowest,
        ),
        labelStyle: WidgetStateTextStyle.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? textTheme.bodySmall!.copyWith(color: scheme.onInverseSurface, fontWeight: FontWeight.w600)
              : textTheme.bodySmall!.copyWith(color: scheme.onSurface, fontWeight: FontWeight.w500),
        ),
        padding: const EdgeInsets.symmetric(horizontal: AppConstants.filterChipPadding),
        labelPadding: EdgeInsets.zero,
        pressElevation: 0,
        elevation: 0,
      ),
      // DS nema toast, pa poruka uspjeha ili greške ide u snackbar istih boja kao tooltip
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: scheme.inverseSurface,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: scheme.onInverseSurface, fontWeight: FontWeight.w500),
        shape: controlShape,
        elevation: 0,
      ),
      dividerTheme: DividerThemeData(color: scheme.outline, thickness: AppConstants.borderWidthThin, space: 0),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: scheme.primary),
      tooltipTheme: TooltipThemeData(
        decoration: BoxDecoration(
          color: scheme.inverseSurface,
          borderRadius: BorderRadius.circular(AppConstants.radiusGrid),
        ),
        textStyle: textTheme.bodySmall?.copyWith(color: scheme.onInverseSurface),
      ),
    );
  }

  static OutlineInputBorder _inputBorder(Color color, double width) => OutlineInputBorder(
    borderRadius: BorderRadius.circular(AppConstants.radiusControl),
    borderSide: BorderSide(color: color, width: width),
  );

  /// Tipografska skala DS v2 (veličina/visina linije). `height` je visina linije podijeljena veličinom.
  static const TextTheme _textTheme = TextTheme(
    displayLarge: TextStyle(fontSize: 36, height: 44 / 36, fontWeight: FontWeight.w600),
    displayMedium: TextStyle(fontSize: 36, height: 44 / 36, fontWeight: FontWeight.w600),
    // Balans
    displaySmall: TextStyle(fontSize: 36, height: 44 / 36, fontWeight: FontWeight.w600),
    headlineLarge: TextStyle(fontSize: 28, height: 36 / 28, fontWeight: FontWeight.w600),
    // H1
    headlineMedium: TextStyle(fontSize: 28, height: 36 / 28, fontWeight: FontWeight.w600),
    // H2
    headlineSmall: TextStyle(fontSize: 22, height: 28 / 22, fontWeight: FontWeight.w600),
    // H3
    titleLarge: TextStyle(fontSize: 18, height: 24 / 18, fontWeight: FontWeight.w600),
    // Iznos
    titleMedium: TextStyle(fontSize: 16, height: 24 / 16, fontWeight: FontWeight.w600),
    titleSmall: TextStyle(fontSize: 14, height: 20 / 14, fontWeight: FontWeight.w600),
    // Tekst
    bodyLarge: TextStyle(fontSize: 15, height: 22 / 15, fontWeight: FontWeight.w400),
    // Tabela
    bodyMedium: TextStyle(fontSize: 14, height: 20 / 14, fontWeight: FontWeight.w400),
    // Mali
    bodySmall: TextStyle(fontSize: 13, height: 18 / 13, fontWeight: FontWeight.w400),
    // Dugme
    labelLarge: TextStyle(fontSize: 14, height: 20 / 14, fontWeight: FontWeight.w600),
    // Labela
    labelMedium: TextStyle(fontSize: 12, height: 16 / 12, fontWeight: FontWeight.w500),
    // Naslov grupe u sidebaru (velika slova, razmak 0,08 em)
    labelSmall: TextStyle(fontSize: 11, height: 16 / 11, fontWeight: FontWeight.w600, letterSpacing: 0.88),
  );
}
