import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_session.dart';

/// Sesija u sigurnom skladištu sistema (Keychain, Windows Credential Manager, Android Keystore).
class SessionStorage {
  SessionStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const String _key = 'torvin.session';

  final FlutterSecureStorage _storage;

  Future<AuthSession?> read() async {
    final value = await _storage.read(key: _key);
    if (value == null) {
      return null;
    }

    try {
      return AuthSession.fromJson(jsonDecode(value) as Map<String, dynamic>);
    } on Object {
      // Oštećen zapis (npr. stara verzija aplikacije) ne smije zaključati korisnika, pa se briše
      await clear();
      return null;
    }
  }

  Future<void> write(AuthSession session) => _storage.write(key: _key, value: jsonEncode(session.toJson()));

  Future<void> clear() => _storage.delete(key: _key);
}
