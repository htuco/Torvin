import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Izbor teme na uređaju. Isto skladište kao sesija, da ne bude još jedne zavisnosti.
class ThemeModeStorage {
  ThemeModeStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const String _key = 'torvin.themeMode';

  final FlutterSecureStorage _storage;

  /// `null` znači da korisnik još nije birao, pa aplikacija prati sistem.
  Future<ThemeMode?> read() async {
    final value = await _storage.read(key: _key);
    return switch (value) {
      'light' => ThemeMode.light,
      'dark' => ThemeMode.dark,
      _ => null,
    };
  }

  Future<void> write(ThemeMode mode) => _storage.write(key: _key, value: mode.name);
}
