import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config/app_config.dart';
import 'api_exception.dart';

/// Jedini put do API-ja: base URL iz `AppConfig`, token u headeru i greške u obliku `ApiException`.
class ApiClient {
  ApiClient({
    required this._httpClient,
    required this._readToken,
    required this._onUnauthorized,
  });

  static const Duration timeout = Duration(seconds: 15);
  static const String genericMessage = 'Došlo je do greške. Pokušajte ponovo.';
  static const String unreachableMessage = 'Server nije dostupan. Provjerite vezu i pokušajte ponovo.';

  final http.Client _httpClient;
  final Future<String?> Function() _readToken;
  final void Function() _onUnauthorized;

  /// Vraća JSON tijelo odgovora, ili `null` kad ga nema (204).
  /// `authorized: false` je samo za anonimne rute prijave, koje ne šalju token.
  Future<Map<String, dynamic>?> post(String path, {Object? body, bool authorized = true}) => _send(
        authorized: authorized,
        headers: {'Content-Type': 'application/json', 'Accept': 'application/json'},
        request: (headers) =>
            _httpClient.post(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: headers, body: jsonEncode(body)),
      );

  /// Izmjena postojećeg zapisa, uvijek s tokenom.
  Future<Map<String, dynamic>?> put(String path, {Object? body}) => _send(
        authorized: true,
        headers: {'Content-Type': 'application/json', 'Accept': 'application/json'},
        request: (headers) =>
            _httpClient.put(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: headers, body: jsonEncode(body)),
      );

  /// Brisanje, uvijek s tokenom. Uspjeh je obično 204 bez tijela, pa vraća `null`.
  Future<Map<String, dynamic>?> delete(String path) => _send(
        authorized: true,
        headers: {'Accept': 'application/json'},
        request: (headers) => _httpClient.delete(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: headers),
      );

  /// Čitanje s tokenom. Parametar s vrijednošću `null` se ne šalje, pa filter koji nije izabran ne ide u adresu.
  Future<Map<String, dynamic>?> get(String path, {Map<String, Object?> query = const {}}) {
    final parameters = {
      for (final entry in query.entries)
        if (entry.value != null) entry.key: entry.value.toString(),
    };
    final uri = Uri.parse('${AppConfig.apiBaseUrl}$path');
    return _send(
      authorized: true,
      headers: {'Accept': 'application/json'},
      request: (headers) =>
          _httpClient.get(parameters.isEmpty ? uri : uri.replace(queryParameters: parameters), headers: headers),
    );
  }

  /// Zajedničko za svaku metodu: token, istek vremena, 401 → prijava i greška u obliku `ApiException`.
  Future<Map<String, dynamic>?> _send({
    required bool authorized,
    required Map<String, String> headers,
    required Future<http.Response> Function(Map<String, String> headers) request,
  }) async {
    if (authorized) {
      final token = await _readToken();
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    final http.Response response;
    try {
      response = await request(headers).timeout(timeout);
    } on SocketException {
      throw const ApiException(statusCode: 0, message: unreachableMessage);
    } on http.ClientException {
      throw const ApiException(statusCode: 0, message: unreachableMessage);
    } on TimeoutException {
      throw const ApiException(statusCode: 0, message: unreachableMessage);
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      return response.body.isEmpty ? null : jsonDecode(response.body) as Map<String, dynamic>;
    }

    // 401 na zaštićenoj ruti znači istekao ili opozvan token, pa korisnik ide na prijavu
    if (response.statusCode == HttpStatus.unauthorized && authorized) {
      _onUnauthorized();
    }

    throw _toException(response);
  }

  static ApiException _toException(http.Response response) {
    final Object? decoded;
    try {
      decoded = jsonDecode(response.body);
    } on FormatException {
      return ApiException(statusCode: response.statusCode, message: genericMessage);
    }

    if (decoded is! Map<String, dynamic>) {
      return ApiException(statusCode: response.statusCode, message: genericMessage);
    }

    final errors = decoded['errors'];
    return ApiException(
      statusCode: response.statusCode,
      message: decoded['message'] as String? ?? genericMessage,
      fieldErrors: errors is Map<String, dynamic>
          ? errors.map((field, messages) => MapEntry(field, List<String>.from(messages as List)))
          : const {},
    );
  }
}
