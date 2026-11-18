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
  Future<Map<String, dynamic>?> post(String path, {Object? body, bool authorized = true}) async {
    final headers = {'Content-Type': 'application/json', 'Accept': 'application/json'};
    if (authorized) {
      final token = await _readToken();
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    final http.Response response;
    try {
      response = await _httpClient
          .post(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: headers, body: jsonEncode(body))
          .timeout(timeout);
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
