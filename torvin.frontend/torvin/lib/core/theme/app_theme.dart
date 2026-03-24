import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'app_constants.dart';

/// AppTheme provides light and dark theme configurations with Material 3 support
/// Generated with Flutter Theme Generator - Clean, modular, and maintainable
/// 
/// Features:
/// ✅ Uses AppConstants for consistent design tokens
/// ✅ Modular structure with separate theme components
/// ✅ Material 3 compliant color schemes
/// ✅ Support for 6 contrast modes (light, dark, medium/high contrast variants)
/// ✅ Production-ready with proper type declarations
class AppTheme {
  AppTheme._(); // Private constructor to prevent instantiation

  // ═══════════════════════════════════════════════════════════════════════════════
  // 🎨 PUBLIC THEME GETTERS
  // ═══════════════════════════════════════════════════════════════════════════════

  /// Light theme configuration
  static ThemeData get lightTheme => theme(lightScheme());

  /// Dark theme configuration
  static ThemeData get darkTheme => theme(darkScheme());

  /// Light medium contrast theme
  static ThemeData get lightMediumContrast => theme(lightMediumContrastScheme());

  /// Light high contrast theme
  static ThemeData get lightHighContrast => theme(lightHighContrastScheme());

  /// Dark medium contrast theme
  static ThemeData get darkMediumContrast => theme(darkMediumContrastScheme());

  /// Dark high contrast theme
  static ThemeData get darkHighContrast => theme(darkHighContrastScheme());

  // ═══════════════════════════════════════════════════════════════════════════════
  // 🌈 COLOR SCHEMES - Material 3 compliant
  // ═══════════════════════════════════════════════════════════════════════════════

  /// Light color scheme - Professional light purple with green accents (OKLCH based)
  static ColorScheme lightScheme() {
    return const ColorScheme(
      brightness: Brightness.light,
      primary: Color(0xFF7C3AED),
      surfaceTint: Color(0xFF7C3AED),
      onPrimary: Color(0xFFFFFFFF),
      primaryContainer: Color(0xFFEDE9FE),
      onPrimaryContainer: Color(0xFF3730A3),
      secondary: Color(0xFFC7A8F5),
      onSecondary: Color(0xFF2D1B47),
      secondaryContainer: Color(0xFFF3E8FF),
      onSecondaryContainer: Color(0xFF6B5B95),
      tertiary: Color(0xFF059669),
      onTertiary: Color(0xFFFFFFFF),
      tertiaryContainer: Color(0xFFD1FAE5),
      onTertiaryContainer: Color(0xFF005A47),
      error: Color(0xFFDC2626),
      onError: Color(0xFFFFFFFF),
      errorContainer: Color(0xFFFEE2E2),
      onErrorContainer: Color(0xFF991B1B),
      surface: Color(0xFFFAF5FF),
      onSurface: Color(0xFF1F1B29),
      onSurfaceVariant: Color(0xFF49454F),
      outline: Color(0xFF79747E),
      outlineVariant: Color(0xFFCAC4D0),
      shadow: Color(0xFF000000),
      scrim: Color(0xFF000000),
      inverseSurface: Color(0xFF313033),
      onInverseSurface: Color(0xFFF4EFF4),
      inversePrimary: Color(0xFFDDD6FE),
      primaryFixed: Color(0xFFEDE9FE),
      onPrimaryFixed: Color(0xFF2E0080),
      primaryFixedDim: Color(0xFFC7B5FF),
      onPrimaryFixedVariant: Color(0xFF5E37C8),
      secondaryFixed: Color(0xFFF3E8FF),
      onSecondaryFixed: Color(0xFF500F7F),
      secondaryFixedDim: Color(0xFFD4AFEA),
      onSecondaryFixedVariant: Color(0xFF7565A8),
      tertiaryFixed: Color(0xFFD1FAE5),
      onTertiaryFixed: Color(0xFF002618),
      tertiaryFixedDim: Color(0xFF9FFCD8),
      onTertiaryFixedVariant: Color(0xFF047857),
      surfaceDim: Color(0xFFEDE6F5),
      surfaceBright: Color(0xFFFAF5FF),
      surfaceContainerLowest: Color(0xFFFFFFFF),
      surfaceContainerLow: Color(0xFFF4EFFC),
      surfaceContainer: Color(0xFFEEE9F7),
      surfaceContainerHigh: Color(0xFFE8E2F0),
      surfaceContainerHighest: Color(0xFFE2DCEB),
    );
  }

  /// Dark color scheme - Rich purple accents with very dark background (OKLCH based, final palette)
  static ColorScheme darkScheme() {
    return const ColorScheme(
      brightness: Brightness.dark,
      primary: Color(0xFFA88CFA),
      surfaceTint: Color(0xFFA88CFA),
      onPrimary: Color(0xFF1A0A3D),
      primaryContainer: Color(0xFF7C3AED),
      onPrimaryContainer: Color(0xFFEDE9FE),
      secondary: Color(0xFFC084FC),
      onSecondary: Color(0xFF2D1B47),
      secondaryContainer: Color(0xFF9B78F5),
      onSecondaryContainer: Color(0xFFF3E8FF),
      tertiary: Color(0xFF10B981),
      onTertiary: Color(0xFF051F16),
      tertiaryContainer: Color(0xFF047857),
      onTertiaryContainer: Color(0xFFD1FAE5),
      error: Color(0xFFFF6B6B),
      onError: Color(0xFF37000B),
      errorContainer: Color(0xFFB3261E),
      onErrorContainer: Color(0xFFFFDAD6),
      surface: Color(0xFF0D0A0F),
      onSurface: Color(0xFFC8C3D2),
      onSurfaceVariant: Color(0xFFC9C4D0),
      outline: Color(0xFF938F99),
      outlineVariant: Color(0xFF49454F),
      shadow: Color(0xFF000000),
      scrim: Color(0xFF000000),
      inverseSurface: Color(0xFFE5E0EB),
      onInverseSurface: Color(0xFF0D0A0F),
      inversePrimary: Color(0xFF7C3AED),
      primaryFixed: Color(0xFFEDE9FE),
      onPrimaryFixed: Color(0xFF2E0080),
      primaryFixedDim: Color(0xFFD0BCFF),
      onPrimaryFixedVariant: Color(0xFF5E37C8),
      secondaryFixed: Color(0xFFF3E8FF),
      onSecondaryFixed: Color(0xFF500F7F),
      secondaryFixedDim: Color(0xFFD4AFEA),
      onSecondaryFixedVariant: Color(0xFF7565A8),
      tertiaryFixed: Color(0xFFD1FAE5),
      onTertiaryFixed: Color(0xFF002618),
      tertiaryFixedDim: Color(0xFF9FFCD8),
      onTertiaryFixedVariant: Color(0xFF047857),
      surfaceDim: Color(0xFF0D0A0F),
      surfaceBright: Color(0xFF34313B),
      surfaceContainerLowest: Color(0xFF080506),
      surfaceContainerLow: Color(0xFF15121B),
      surfaceContainer: Color(0xFF1A161F),
      surfaceContainerHigh: Color(0xFF242129),
      surfaceContainerHighest: Color(0xFF2F2C33),
    );
  }

  /// Light medium contrast color scheme
  static ColorScheme lightMediumContrastScheme() {
    return lightScheme().copyWith(
      primary: Color(0xFF3a3895),
      surface: Color(0xFFfaf6f9),
    );
  }

  /// Light high contrast color scheme
  static ColorScheme lightHighContrastScheme() {
    return lightScheme().copyWith(
      primary: Color(0xFF2b2986),
      surface: Color(0xFFf5f1f4),
      outline: const Color(0xff000000),
    );
  }

  /// Dark medium contrast color scheme
  static ColorScheme darkMediumContrastScheme() {
    return darkScheme().copyWith(
      primary: Color(0xFF44429f),
      surface: Color(0xFF150e12),
    );
  }

  /// Dark high contrast color scheme
  static ColorScheme darkHighContrastScheme() {
    return darkScheme().copyWith(
      primary: Color(0xFF5351ae),
      surface: Color(0xFF1a1317),
      outline: const Color(0xffffffff),
    );
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // 🎯 MAIN THEME BUILDER - Clean and modular structure
  // ═══════════════════════════════════════════════════════════════════════════════

  /// Main theme function that combines all theme components
  /// Uses clean, modular structure with proper AppConstants integration
  static ThemeData theme(ColorScheme colorScheme) => ThemeData(
    useMaterial3: true,
    brightness: colorScheme.brightness,
    colorScheme: colorScheme,
    fontFamily: GoogleFonts.poppins().fontFamily,
    textTheme: GoogleFonts.poppinsTextTheme(_textTheme),
    primaryTextTheme: GoogleFonts.poppinsTextTheme(_textTheme),
    appBarTheme: colorScheme.brightness == Brightness.light ? _lightAppBarTheme : _darkAppBarTheme,
    elevatedButtonTheme: elevatedButtonTheme(colorScheme),
    filledButtonTheme: filledButtonTheme(colorScheme),
    textButtonTheme: textButtonTheme(colorScheme),
    outlinedButtonTheme: outlinedButtonTheme(colorScheme),
    iconButtonTheme: iconButtonTheme(colorScheme),
    inputDecorationTheme: _inputDecorationTheme,
    cardTheme: _cardTheme,
    chipTheme: _chipTheme,
    progressIndicatorTheme: _progressIndicatorTheme,
    dividerTheme: _dividerTheme,
    bottomNavigationBarTheme: _bottomNavigationBarTheme,
    tabBarTheme: _tabBarTheme,
    switchTheme: switchTheme(colorScheme),
    checkboxTheme: _checkboxTheme,
    radioTheme: _radioTheme,
    sliderTheme: _sliderTheme,
    scaffoldBackgroundColor: colorScheme.surface,
    canvasColor: colorScheme.surface,
  );

  // ═══════════════════════════════════════════════════════════════════════════════
  // 🎨 THEME COMPONENTS - All using AppConstants for consistency
  // ═══════════════════════════════════════════════════════════════════════════════


  /// Text theme using AppConstants for consistent font sizes
  static final TextTheme _textTheme = TextTheme(
    displayLarge: TextStyle(
      fontSize: AppConstants.fontSizeDisplayLarge,
      fontWeight: FontWeight.w400,
      letterSpacing: -0.25,
      height: 1.12,
    ),
    displayMedium: TextStyle(
      fontSize: AppConstants.fontSizeDisplayMedium,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.16,
    ),
    displaySmall: TextStyle(
      fontSize: AppConstants.fontSizeDisplaySmall,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.22,
    ),
    headlineLarge: TextStyle(
      fontSize: AppConstants.fontSizeHeadlineLarge,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.25,
    ),
    headlineMedium: TextStyle(
      fontSize: AppConstants.fontSizeHeadlineMedium,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.29,
    ),
    headlineSmall: TextStyle(
      fontSize: AppConstants.fontSizeHeadlineSmall,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.33,
    ),
    titleLarge: TextStyle(
      fontSize: AppConstants.fontSizeTitleLarge,
      fontWeight: FontWeight.w400,
      letterSpacing: 0,
      height: 1.27,
    ),
    titleMedium: TextStyle(
      fontSize: AppConstants.fontSizeTitleMedium,
      fontWeight: FontWeight.w500,
      letterSpacing: 0.15,
      height: 1.50,
    ),
    titleSmall: TextStyle(
      fontSize: AppConstants.fontSizeTitleSmall,
      fontWeight: FontWeight.w500,
      letterSpacing: 0.1,
      height: 1.43,
    ),
    labelLarge: TextStyle(
      fontSize: AppConstants.fontSizeLabelLarge,
      fontWeight: FontWeight.w500,
      letterSpacing: 0.1,
      height: 1.43,
    ),
    labelMedium: TextStyle(
      fontSize: AppConstants.fontSizeLabelMedium,
      fontWeight: FontWeight.w500,
      letterSpacing: 0.5,
      height: 1.33,
    ),
    labelSmall: TextStyle(
      fontSize: AppConstants.fontSizeLabelSmall,
      fontWeight: FontWeight.w500,
      letterSpacing: 0.5,
      height: 1.45,
    ),
    bodyLarge: TextStyle(
      fontSize: AppConstants.fontSizeBodyLarge,
      fontWeight: FontWeight.w400,
      letterSpacing: 0.15,
      height: 1.50,
    ),
    bodyMedium: TextStyle(
      fontSize: AppConstants.fontSizeBodyMedium,
      fontWeight: FontWeight.w400,
      letterSpacing: 0.25,
      height: 1.43,
    ),
    bodySmall: TextStyle(
      fontSize: AppConstants.fontSizeBodySmall,
      fontWeight: FontWeight.w400,
      letterSpacing: 0.4,
      height: 1.33,
    ),
  );


  /// Elevated button theme - M3 compliant with WidgetStateProperty
  static ElevatedButtonThemeData elevatedButtonTheme(ColorScheme colorScheme) => ElevatedButtonThemeData(
    style: ButtonStyle(
      elevation: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) return 0;
        if (states.contains(WidgetState.hovered)) return AppConstants.elevationLevel3;
        if (states.contains(WidgetState.pressed)) return AppConstants.elevationLevel1;
        return AppConstants.elevationLevel2;
      }),
      padding: WidgetStateProperty.all(
        EdgeInsets.symmetric(
          horizontal: AppConstants.spacingLG,
          vertical: AppConstants.spacingMD,
        ),
      ),
      shape: WidgetStateProperty.all(
        RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.radiusMD),
        ),
      ),
      backgroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.12);
        }
        return colorScheme.primary;
      }),
      foregroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.38);
        }
        return colorScheme.onPrimary;
      }),
      overlayColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.pressed)) {
          return colorScheme.onPrimary.withValues(alpha: 0.1);
        }
        if (states.contains(WidgetState.hovered)) {
          return colorScheme.onPrimary.withValues(alpha: 0.08);
        }
        if (states.contains(WidgetState.focused)) {
          return colorScheme.onPrimary.withValues(alpha: 0.1);
        }
        return null;
      }),
      shadowColor: WidgetStateProperty.all(colorScheme.shadow),
    ),
  );

  /// Filled button theme - M3 compliant with WidgetStateProperty
  static FilledButtonThemeData filledButtonTheme(ColorScheme colorScheme) => FilledButtonThemeData(
    style: ButtonStyle(
      padding: WidgetStateProperty.all(
        EdgeInsets.symmetric(
          horizontal: AppConstants.spacingLG,
          vertical: AppConstants.spacingMD,
        ),
      ),
      shape: WidgetStateProperty.all(
        RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.radiusMD),
        ),
      ),
      backgroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.12);
        }
        return colorScheme.secondaryContainer;
      }),
      foregroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.38);
        }
        return colorScheme.onSecondaryContainer;
      }),
      overlayColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.pressed)) {
          return colorScheme.onSecondaryContainer.withValues(alpha: 0.1);
        }
        if (states.contains(WidgetState.hovered)) {
          return colorScheme.onSecondaryContainer.withValues(alpha: 0.08);
        }
        return null;
      }),
    ),
  );

  /// Text button theme - M3 compliant with WidgetStateProperty
  static TextButtonThemeData textButtonTheme(ColorScheme colorScheme) => TextButtonThemeData(
    style: ButtonStyle(
      padding: WidgetStateProperty.all(
        EdgeInsets.symmetric(
          horizontal: AppConstants.spacingLG,
          vertical: AppConstants.spacingMD,
        ),
      ),
      shape: WidgetStateProperty.all(
        RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.radiusMD),
        ),
      ),
      foregroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.38);
        }
        return colorScheme.primary;
      }),
      overlayColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.pressed)) {
          return colorScheme.primary.withValues(alpha: 0.1);
        }
        if (states.contains(WidgetState.hovered)) {
          return colorScheme.primary.withValues(alpha: 0.08);
        }
        if (states.contains(WidgetState.focused)) {
          return colorScheme.primary.withValues(alpha: 0.1);
        }
        return null;
      }),
    ),
  );

  /// Outlined button theme - M3 compliant with WidgetStateProperty
  static OutlinedButtonThemeData outlinedButtonTheme(ColorScheme colorScheme) => OutlinedButtonThemeData(
    style: ButtonStyle(
      padding: WidgetStateProperty.all(
        EdgeInsets.symmetric(
          horizontal: AppConstants.spacingLG,
          vertical: AppConstants.spacingMD,
        ),
      ),
      shape: WidgetStateProperty.all(
        RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.radiusMD),
        ),
      ),
      side: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.12));
        }
        if (states.contains(WidgetState.focused)) {
          return BorderSide(color: colorScheme.primary);
        }
        return BorderSide(color: colorScheme.outline);
      }),
      foregroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.38);
        }
        return colorScheme.primary;
      }),
      overlayColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.pressed)) {
          return colorScheme.primary.withValues(alpha: 0.1);
        }
        if (states.contains(WidgetState.hovered)) {
          return colorScheme.primary.withValues(alpha: 0.08);
        }
        return null;
      }),
    ),
  );

  /// Icon button theme - M3 compliant with WidgetStateProperty
  static IconButtonThemeData iconButtonTheme(ColorScheme colorScheme) => IconButtonThemeData(
    style: ButtonStyle(
      foregroundColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.disabled)) {
          return colorScheme.onSurface.withValues(alpha: 0.38);
        }
        return colorScheme.onSurfaceVariant;
      }),
      overlayColor: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.pressed)) {
          return colorScheme.onSurfaceVariant.withValues(alpha: 0.1);
        }
        if (states.contains(WidgetState.hovered)) {
          return colorScheme.onSurfaceVariant.withValues(alpha: 0.08);
        }
        return null;
      }),
    ),
  );


  /// Input decoration theme
  static final InputDecorationTheme _inputDecorationTheme = InputDecorationTheme(
    contentPadding: EdgeInsets.symmetric(
      horizontal: AppConstants.spacingMD,
      vertical: AppConstants.spacingMD,
    ),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusMD),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusMD),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusMD),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusMD),
    ),
  );


  /// App bar theme for light mode
  static final AppBarTheme _lightAppBarTheme = AppBarTheme(
    elevation: AppConstants.elevationLevel1,
    centerTitle: false,
    titleSpacing: AppConstants.spacingMD,
    scrolledUnderElevation: AppConstants.elevationLevel1,
  );

  /// App bar theme for dark mode
  static final AppBarTheme _darkAppBarTheme = AppBarTheme(
    elevation: AppConstants.elevationLevel1,
    centerTitle: false,
    titleSpacing: AppConstants.spacingMD,
    scrolledUnderElevation: AppConstants.elevationLevel1,
  );

  /// Card theme
  static final CardThemeData _cardTheme = CardThemeData(
    elevation: AppConstants.elevationLevel1,
    margin: EdgeInsets.all(AppConstants.spacingSM),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusLG),
    ),
  );

  /// Chip theme
  static final ChipThemeData _chipTheme = ChipThemeData(
    padding: EdgeInsets.symmetric(
      horizontal: AppConstants.spacingMD,
      vertical: AppConstants.spacingSM,
    ),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusFull),
    ),
  );

  /// Progress indicator theme
  static final ProgressIndicatorThemeData _progressIndicatorTheme = ProgressIndicatorThemeData();

  /// Divider theme
  static final DividerThemeData _dividerTheme = DividerThemeData(
    thickness: AppConstants.borderWidthThin,
    space: AppConstants.spacingMD,
  );

  /// Bottom navigation bar theme
  static final BottomNavigationBarThemeData _bottomNavigationBarTheme = BottomNavigationBarThemeData(
    type: BottomNavigationBarType.fixed,
  );

  /// Tab bar theme
  static final TabBarThemeData _tabBarTheme = TabBarThemeData(
    labelPadding: EdgeInsets.symmetric(
      horizontal: AppConstants.spacingMD,
      vertical: AppConstants.spacingSM,
    ),
  );

  /// Switch theme - uses colorScheme from theme() parameter
  static SwitchThemeData switchTheme(ColorScheme colorScheme) => SwitchThemeData(
    thumbColor: WidgetStateProperty.resolveWith((states) {
      if (states.contains(WidgetState.selected)) {
        return colorScheme.primary;
      }
      return null;
    }),
    trackColor: WidgetStateProperty.resolveWith((states) {
      if (states.contains(WidgetState.selected)) {
        return colorScheme.primaryContainer;
      }
      return null;
    }),
  );

  /// Checkbox theme
  static final CheckboxThemeData _checkboxTheme = CheckboxThemeData(
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.radiusXS),
    ),
  );

  /// Radio theme
  static final RadioThemeData _radioTheme = RadioThemeData();

  /// Slider theme
  static final SliderThemeData _sliderTheme = SliderThemeData();
}

/// Custom theme colors extension for additional brand colors
extension CustomColors on ColorScheme {
  /// Success color for positive actions and states
  Color get success => const Color(0xFF2E7D32);
  
  /// Warning color for caution states
  Color get warning => const Color(0xFFF57C00);
  
  /// Info color for informational states
  Color get info => const Color(0xFF1976D2);
}
