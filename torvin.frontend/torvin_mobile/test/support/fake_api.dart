import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';
import 'package:torvin_mobile/app/router/app_router.dart';
import 'package:torvin_mobile/main.dart';

/// Sesija u memoriji umjesto Keychaina i Credential Managera, kojih u testu nema.
class FakeSessionStorage implements SessionStorage {
  FakeSessionStorage(this.session);

  AuthSession? session;

  @override
  Future<AuthSession?> read() async => session;

  @override
  Future<void> write(AuthSession value) async => session = value;

  @override
  Future<void> clear() async => session = null;
}

/// Izbor teme u memoriji: aplikacija prati sistem.
class FakeThemeModeStorage implements ThemeModeStorage {
  @override
  Future<ThemeMode?> read() async => null;

  @override
  Future<void> write(ThemeMode value) async {}
}

/// API koji odgovara po "METODA /putanja" i bilježi svaki zahtjev. Ruta koja nije zadana vraća 404, da test
/// odmah pokaže poziv koji nije očekivao.
class FakeApi {
  FakeApi(this.routes);

  final Map<String, http.Response Function(http.Request request)> routes;
  final List<http.Request> requests = [];

  http.Client get client => MockClient((request) async {
        requests.add(request);
        final handler = routes['${request.method} ${request.url.path}'];
        return handler == null
            ? json(404, {'message': 'Ruta ${request.method} ${request.url.path} nije zadana u testu.'})
            : handler(request);
      });

  List<http.Request> sent(String method, String path) =>
      [for (final request in requests) if (request.method == method && request.url.path == path) request];
}

http.Response json(int statusCode, Object body) =>
    http.Response(jsonEncode(body), statusCode, headers: {'content-type': 'application/json; charset=utf-8'});

Map<String, dynamic> page(List<Map<String, dynamic>> items) =>
    {'items': items, 'totalCount': items.length, 'page': 1, 'pageSize': 20};

Map<String, dynamic> cardJson({
  int id = 7,
  String status = 'Active',
  String statusName = 'Aktivna',
  String? name = 'Glavna',
  String last4 = '4821',
  bool blockedByAdmin = false,
}) =>
    {
      'id': id,
      'userId': 3,
      'userFirstName': 'Amra',
      'userLastName': 'Hodžić',
      'name': name,
      'cardTypeId': 1,
      'cardTypeName': 'Debitna',
      'cardStatusId': 2,
      'cardStatusCode': status,
      'cardStatusName': statusName,
      'currencyId': 1,
      'currencyCode': 'BAM',
      'network': 'Visa',
      'last4Digits': last4,
      'expiresAt': '2029-08-31',
      'balance': 1245.0,
      'isDefault': true,
      'createdAt': '2026-11-20T09:15:00Z',
      'isBlockedByAdmin': blockedByAdmin,
    };

Map<String, dynamic> transactionJson({String direction = 'Sent', double amount = 120, String? note}) => {
      'id': 1,
      'referenceNumber': 'TRV-261120-0001',
      'direction': direction,
      'amount': amount,
      'currencyCode': 'BAM',
      'transactionTypeCode': 'Transfer',
      'transactionTypeName': 'Prenos',
      'transactionStatusCode': 'Completed',
      'transactionStatusName': 'Izvršena',
      'transactionCategoryId': null,
      'transactionCategoryName': null,
      'note': note,
      'myCardId': 7,
      'myCardLast4Digits': '4821',
      'counterpartyUserId': 5,
      'counterpartyFirstName': 'Marko',
      'counterpartyLastName': 'Petrović',
      'counterpartyCardLast4Digits': '1234',
      'createdAt': '2026-11-20T09:15:00Z',
    };

Map<String, dynamic> recipientJson({int userId = 5, String firstName = 'Marko', String lastName = 'Petrović'}) => {
      'userId': userId,
      'firstName': firstName,
      'lastName': lastName,
      'username': '${firstName.toLowerCase()}.p',
    };

Map<String, dynamic> favoriteJson({int id = 3, int userId = 6, String? nickname = 'Cimerka'}) => {
      'id': id,
      'recipientUserId': userId,
      'firstName': 'Selma',
      'lastName': 'Hadžić',
      'username': 'selma',
      'nickname': nickname,
      'createdAt': '2026-09-01T10:00:00Z',
    };

const List<Map<String, dynamic>> cardStatusesJson = [
  {'id': 1, 'code': 'Pending', 'name': 'Na čekanju'},
  {'id': 2, 'code': 'Active', 'name': 'Aktivna'},
  {'id': 3, 'code': 'Blocked', 'name': 'Blokirana'},
  {'id': 4, 'code': 'Lost', 'name': 'Izgubljena'},
  {'id': 5, 'code': 'Stolen', 'name': 'Ukradena'},
  {'id': 6, 'code': 'Closed', 'name': 'Zatvorena'},
];

/// Pokrene cijelu aplikaciju s prijavljenim korisnikom i otvori `location` kroz pravi router. `overrides` mijenja
/// ono što se u testu ne može pokrenuti (npr. kamera).
Future<ProviderContainer> pumpAt(
  WidgetTester tester,
  FakeApi api,
  String location, {
  List<Override> overrides = const [],
}) async {
  final container = ProviderContainer(overrides: [
    sessionStorageProvider.overrideWithValue(FakeSessionStorage(AuthSession(
      token: 'token-123',
      expiresAt: DateTime.now().toUtc().add(const Duration(hours: 1)),
      role: 'User',
      firstName: 'Amra',
      lastName: 'Hodžić',
    ))),
    themeModeStorageProvider.overrideWithValue(FakeThemeModeStorage()),
    httpClientProvider.overrideWithValue(api.client),
    ...overrides,
  ]);
  addTearDown(container.dispose);

  await tester.pumpWidget(UncontrolledProviderScope(container: container, child: const TorvinApp()));
  await tester.pumpAndSettle();
  container.read(routerProvider).go(location);
  await tester.pumpAndSettle();
  return container;
}
