import 'package:flutter/painting.dart';

/// Razmaci, zaobljenja, visine i sjene iz dizajn sistema v2.
class AppConstants {
  AppConstants._();

  /// Ikona ↔ tekst.
  static const double spacingXS = 4;

  /// Labela ↔ polje.
  static const double spacingSM = 8;

  /// Unutar reda liste.
  static const double spacingRow = 12;

  /// Padding kartice na mobilnoj.
  static const double spacingMD = 16;

  /// Margina ekrana i razmak između sekcija.
  static const double spacingLG = 24;

  /// Padding panela na desktopu.
  static const double spacingXL = 32;

  /// Kartica i dijalog.
  static const double radiusCard = 20;

  /// Dugme, input i odabrana stavka.
  static const double radiusControl = 14;

  /// Male ikone i logo.
  static const double radiusIcon = 12;

  /// Mreža i trake grafikona.
  static const double radiusGrid = 8;

  /// Chip, avatar.
  static const double radiusFull = 9999;

  static const double borderWidthThin = 1;
  static const double borderWidthMedium = 2;

  /// Dugme i input na mobilnoj.
  static const double buttonHeight = 48;
  static const double buttonHeightDesktop = 40;

  static const double topBarHeight = 64;
  static const double sidebarWidth = 248;
  static const double sidebarItemHeight = 40;
  static const double avatarSize = 32;
  static const double statusDotSize = 8;
  static const double chipHeight = 28;

  /// Tačka ↔ riječ u status chipu.
  static const double chipGap = 6;
  static const double iconSize = 18;

  /// Kućica koda iz emaila (OTP).
  static const double otpBoxWidth = 48;
  static const double otpBoxHeight = 56;

  /// Ikona uz grešku ispod polja.
  static const double fieldNoteIconSize = 14;

  /// Ikona praznog stanja: kvadrat 64 s ikonom 28.
  static const double emptyIconBox = 64;
  static const double emptyIconSize = 28;

  /// Ikona greške: kvadrat 56 s ikonom 24.
  static const double errorIconBox = 56;
  static const double errorIconSize = 24;

  /// Najveća širina poruke praznog stanja.
  static const double emptyMessageWidth = 240;

  /// Filter čip iznad liste.
  static const double filterChipHeight = 36;
  static const double filterChipPadding = 14;

  /// Ikona u redu liste (historija transakcija): kvadrat 40 s ikonom 20.
  static const double listIconBox = 40;
  static const double listIconSize = 20;

  /// Ikona pločice akcije u detaljima kartice.
  static const double actionTileIconSize = 22;

  /// Ikona dijaloga: kvadrat 48 s ikonom 22.
  static const double dialogIconBox = 48;
  static const double dialogIconSize = 22;
  static const double dialogMaxWidth = 440;

  /// DS v2: pozadina iza dijaloga se zatamni filterom svjetline, bez providnog sloja.
  static const double dialogBackdropBrightness = 0.42;

  /// DS v2: sjena dijaloga `0 8 24`, 10 % crne.
  static const BoxShadow dialogShadow = BoxShadow(color: Color(0x1A000000), offset: Offset(0, 8), blurRadius: 24);

  /// Stranica QR koda za primanje.
  static const double qrSize = 220;

  /// Malo dugme u zaglavlju ekrana ("Nova" na listi kartica): visina 40, padding 14.
  static const double compactButtonHeight = 40;
  static const double compactButtonPadding = 14;

  /// Omiljeni primalac u redu iznad pretrage (1k): pločica 56 s avatarom 48 i imenom 6 ispod.
  static const double favoriteTileWidth = 56;
  static const double favoriteAvatarSize = 48;
  static const double favoriteNameGap = 6;
}
