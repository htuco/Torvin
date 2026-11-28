import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';

import 'support/fakes.dart';

void main() {
  late http.Request lastRequest;
  late int unauthorizedCalls;

  ApiClient clientReturning(http.Response response, {String? token = 'token-123'}) {
    unauthorizedCalls = 0;
    return ApiClient(
      httpClient: MockClient((request) async {
        lastRequest = request;
        return response;
      }),
      readToken: () async => token,
      onUnauthorized: () => unauthorizedCalls++,
    );
  }

  test('zaštićen poziv šalje token, a anonimni ne', () async {
    final api = clientReturning(http.Response('', 204));

    await api.post('/auth/logout');
    expect(lastRequest.headers['Authorization'], 'Bearer token-123');

    await api.post('/auth/login', body: {'login': 'mobile'}, authorized: false);
    expect(lastRequest.headers.containsKey('Authorization'), isFalse);
    expect(jsonDecode(lastRequest.body), {'login': 'mobile'});
  });

  test('204 vraća null, a 200 JSON tijelo', () async {
    expect(await clientReturning(http.Response('', 204)).post('/auth/logout'), isNull);
    expect(
      await clientReturning(jsonResponse(201, {'message': 'Registracija je uspjela.'})).post('/auth/register'),
      {'message': 'Registracija je uspjela.'},
    );
  });

  test('400 s greškama po polju daje poruke s camelCase ključevima', () async {
    final api = clientReturning(jsonResponse(400, {
      'message': 'Podaci nisu ispravni.',
      'errors': {
        'email': ['Email je već registrovan.'],
        'phoneNumber': ['Telefon ima 6–15 cifara.'],
      },
    }));

    final error = await _capture(api.post('/auth/register', authorized: false));

    expect(error.statusCode, 400);
    expect(error.message, 'Podaci nisu ispravni.');
    expect(error.fieldError('email'), 'Email je već registrovan.');
    expect(error.fieldError('phoneNumber'), 'Telefon ima 6–15 cifara.');
    expect(error.fieldError('username'), isNull);
  });

  test('403 prenosi poruku backenda', () async {
    final api = clientReturning(jsonResponse(403, {'message': 'Desktop aplikacija je namijenjena samo administratorima.'}));

    final error = await _capture(api.post('/auth/login', authorized: false));

    expect(error.statusCode, 403);
    expect(error.message, 'Desktop aplikacija je namijenjena samo administratorima.');
    expect(error.fieldErrors, isEmpty);
  });

  test('401 na zaštićenom pozivu odjavljuje, a na anonimnom ne', () async {
    final api = clientReturning(jsonResponse(401, {'message': 'Prijava je istekla ili nije ispravna. Prijavite se ponovo.'}));

    await _capture(api.post('/auth/login', authorized: false));
    expect(unauthorizedCalls, 0);

    final error = await _capture(api.post('/auth/logout'));
    expect(unauthorizedCalls, 1);
    expect(error.statusCode, 401);
  });

  test('tijelo koje nije JSON daje opštu poruku', () async {
    final api = clientReturning(http.Response('<html>Bad Gateway</html>', 502));

    final error = await _capture(api.post('/auth/logout'));

    expect(error.statusCode, 502);
    expect(error.message, ApiClient.genericMessage);
  });

  test('server koji ne odgovara daje status 0 i poruku o vezi', () async {
    final api = ApiClient(
      httpClient: MockClient((_) => throw http.ClientException('Connection refused')),
      readToken: () async => null,
      onUnauthorized: () {},
    );

    final error = await _capture(api.post('/auth/login', authorized: false));

    expect(error.statusCode, 0);
    expect(error.message, ApiClient.unreachableMessage);
  });

  test('GET šalje token, a parametri bez vrijednosti ne idu u adresu', () async {
    final api = clientReturning(jsonResponse(200, {'items': [], 'totalCount': 0, 'page': 2, 'pageSize': 20}));

    final json = await api.get('/cards', query: {'page': 2, 'pageSize': 20, 'cardStatusId': null});

    expect(lastRequest.method, 'GET');
    expect(lastRequest.headers['Authorization'], 'Bearer token-123');
    expect(lastRequest.url.path, '/cards');
    expect(lastRequest.url.queryParameters, {'page': '2', 'pageSize': '20'});
    expect(json!['totalCount'], 0);
  });

  test('GET bez parametara nema upit u adresi', () async {
    final api = clientReturning(jsonResponse(200, {'id': 7}));

    await api.get('/cards/7');

    expect(lastRequest.url.path, '/cards/7');
    expect(lastRequest.url.hasQuery, isFalse);
  });

  test('401 na GET odjavljuje, a 404 prenosi poruku backenda bez odjave', () async {
    await _capture(clientReturning(jsonResponse(401, {'message': 'Prijava je istekla.'})).get('/cards'));
    expect(unauthorizedCalls, 1);

    final notFound = clientReturning(jsonResponse(404, {'message': 'Kartica nije pronađena.'}));
    final error = await _capture(notFound.get('/cards/9'));
    expect(unauthorizedCalls, 0);
    expect(error.statusCode, 404);
    expect(error.message, 'Kartica nije pronađena.');
  });

  test('PUT šalje tijelo i token, a DELETE s 204 vraća null', () async {
    final api = clientReturning(jsonResponse(200, {'id': 3, 'nickname': 'Cimerka'}));
    expect(await api.put('/favorite-recipients/3', body: {'nickname': 'Cimerka'}), {'id': 3, 'nickname': 'Cimerka'});
    expect(lastRequest.method, 'PUT');
    expect(lastRequest.headers['Authorization'], 'Bearer token-123');
    expect(jsonDecode(lastRequest.body), {'nickname': 'Cimerka'});

    expect(await clientReturning(http.Response('', 204)).delete('/favorite-recipients/3'), isNull);
    expect(lastRequest.method, 'DELETE');
    expect(lastRequest.headers['Authorization'], 'Bearer token-123');
  });

  test('401 na PUT i DELETE odjavljuje, a 404 prenosi poruku backenda', () async {
    final expired = clientReturning(jsonResponse(401, {'message': 'Prijava je istekla.'}));
    await _capture(expired.put('/favorite-recipients/3'));
    await _capture(expired.delete('/favorite-recipients/3'));
    expect(unauthorizedCalls, 2);

    final notFound = clientReturning(jsonResponse(404, {'message': 'Omiljeni primalac nije pronađen.'}));
    final error = await _capture(notFound.delete('/favorite-recipients/9'));
    expect(unauthorizedCalls, 0);
    expect(error.message, 'Omiljeni primalac nije pronađen.');
  });
}

Future<ApiException> _capture(Future<Object?> call) async {
  try {
    await call;
  } on ApiException catch (error) {
    return error;
  }
  fail('Očekivan je ApiException.');
}
