import 'dart:convert';

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
