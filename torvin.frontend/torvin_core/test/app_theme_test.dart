import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

/// Kontrast po WCAG 2.1: (svjetlija + 0,05) / (tamnija + 0,05).
double contrast(Color a, Color b) {
  final la = a.computeLuminance();
  final lb = b.computeLuminance();
  return (math.max(la, lb) + 0.05) / (math.min(la, lb) + 0.05);
}

void main() {
  test('svijetla i tamna tema imaju odgovarajuću svjetlinu i primarnu iz dizajna v2', () {
    expect(AppTheme.lightTheme.brightness, Brightness.light);
    expect(AppTheme.darkTheme.brightness, Brightness.dark);
    expect(AppTheme.lightTheme.colorScheme.primary, const Color(0xFFE7000B));
    expect(AppTheme.darkTheme.colorScheme.primary, const Color(0xFFFB2C36));
  });

  test('font je Inter iz paketa, bez preuzimanja s interneta', () {
    for (final theme in [AppTheme.lightTheme, AppTheme.darkTheme]) {
      expect(theme.textTheme.bodyMedium!.fontFamily, contains('Inter'));
    }
  });

  test('obje teme registruju TorvinColors', () {
    expect(AppTheme.lightTheme.extension<TorvinColors>(), TorvinColors.light);
    expect(AppTheme.darkTheme.extension<TorvinColors>(), TorvinColors.dark);
  });

  test('desktop tema ima niža dugmad od mobilne', () {
    double height(ThemeData theme) => theme.filledButtonTheme.style!.minimumSize!.resolve({})!.height;

    expect(height(AppTheme.lightTheme), AppConstants.buttonHeight);
    expect(height(AppTheme.desktopLightTheme), AppConstants.buttonHeightDesktop);
  });

  test('tekst ima kontrast najmanje 4,5:1 (WCAG AA) u obje teme', () {
    for (final (scheme, colors) in [
      (AppTheme.lightScheme(), TorvinColors.light),
      (AppTheme.darkScheme(), TorvinColors.dark),
    ]) {
      final card = scheme.surfaceContainerLowest;
      final pairs = <String, (Color, Color)>{
        'tekst na pozadini': (scheme.onSurface, scheme.surface),
        'sekundarni tekst na kartici': (scheme.onSurfaceVariant, card),
        'tekst na primarnoj': (scheme.onPrimary, scheme.primary),
        'tekst na grešci': (scheme.onError, scheme.error),
        'tint primarne': (scheme.onPrimaryContainer, scheme.primaryContainer),
        'uspjeh na kartici': (colors.successText, card),
        'upozorenje na kartici': (colors.warningText, card),
        'greška na kartici': (colors.errorText, card),
        'chip uspjeh': (colors.successChip, colors.successTint),
        'chip upozorenje': (colors.warningChip, colors.warningTint),
        'chip greška': (colors.errorChip, colors.errorTint),
        'chip neutralno': (colors.neutralChip, colors.neutralTint),
        'sidebar': (colors.sidebarText, colors.sidebar),
      };

      for (final MapEntry(key: name, value: (fg, bg)) in pairs.entries) {
        expect(contrast(fg, bg), greaterThanOrEqualTo(4.5), reason: '${scheme.brightness.name}: $name');
      }
    }
  });
}
