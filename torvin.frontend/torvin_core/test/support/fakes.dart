import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:torvin_core/torvin_core.dart';

/// Skladište u memoriji umjesto Keychaina i Credential Managera, kojih u testu nema.
class FakeSessionStorage implements SessionStorage {
  FakeSessionStorage([this.session]);

  AuthSession? session;

  @override
  Future<AuthSession?> read() async => session;

  @override
  Future<void> write(AuthSession value) async => session = value;

  @override
  Future<void> clear() async => session = null;
}

/// Izbor teme u memoriji. `failRead` glumi skladište koje nije dostupno.
class FakeThemeModeStorage implements ThemeModeStorage {
  FakeThemeModeStorage([this.mode]);

  ThemeMode? mode;
  bool failRead = false;

  @override
  Future<ThemeMode?> read() async {
    if (failRead) {
      throw StateError('Skladište nije dostupno');
    }
    return mode;
  }

  @override
  Future<void> write(ThemeMode value) async => mode = value;
}

AuthSession sessionExpiringIn(Duration duration) => AuthSession(
      token: 'token-123',
      expiresAt: DateTime.now().toUtc().add(duration),
      role: 'User',
      firstName: 'Amra',
      lastName: 'Hodžić',
    );

http.Response jsonResponse(int statusCode, Object body) => http.Response(
      jsonEncode(body),
      statusCode,
      headers: {'content-type': 'application/json; charset=utf-8'},
    );
