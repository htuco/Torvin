import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';

import 'package:torvin_mobile/main.dart';

class _EmptySessionStorage implements SessionStorage {
  @override
  Future<AuthSession?> read() async => null;

  @override
  Future<void> write(AuthSession session) async {}

  @override
  Future<void> clear() async {}
}

void main() {
  Future<List<http.Request>> pumpApp(WidgetTester tester, http.Response response) async {
    final requests = <http.Request>[];
    await tester.pumpWidget(ProviderScope(
      overrides: [
        sessionStorageProvider.overrideWithValue(_EmptySessionStorage()),
        httpClientProvider.overrideWithValue(MockClient((request) async {
          requests.add(request);
          return response;
        })),
      ],
      child: const TorvinApp(),
    ));
    await tester.pumpAndSettle();
    return requests;
  }

  testWidgets('bez sesije prikazuje prijavu s linkovima toka', (WidgetTester tester) async {
    await pumpApp(tester, http.Response('', 204));

    expect(find.text('Prijavi se'), findsOneWidget);
    expect(find.text('Nemate račun? Registrujte se'), findsOneWidget);
    expect(find.text('Zaboravljena lozinka?'), findsOneWidget);
  });

  testWidgets('registracija vodi na potvrdu emaila s porukom backenda i popunjenim emailom', (tester) async {
    final requests = await pumpApp(
      tester,
      http.Response(
        jsonEncode({'message': 'Registracija je uspjela. Poslali smo kod za potvrdu na vaš email.'}),
        201,
        headers: {'content-type': 'application/json; charset=utf-8'},
      ),
    );

    await tester.tap(find.text('Nemate račun? Registrujte se'));
    await tester.pumpAndSettle();

    Finder field(String label) => find.widgetWithText(TorvinTextField, label);
    await tester.enterText(field('Ime'), 'Amra');
    await tester.enterText(field('Prezime'), 'Hodžić');
    await tester.enterText(field('Korisničko ime'), 'amra.h');
    await tester.enterText(field('Email'), 'amra@domena.ba');
    await tester.enterText(field('Lozinka'), 'Lozinka1!');
    await tester.enterText(field('Potvrda lozinke'), 'Lozinka1!');
    await tester.ensureVisible(find.text('Registruj se'));
    await tester.tap(find.text('Registruj se'));
    await tester.pumpAndSettle();

    final body = jsonDecode(requests.single.body) as Map<String, dynamic>;
    expect(requests.single.url.path, '/auth/register');
    expect(body['phoneNumber'], isNull);
    expect(find.text('Potvrda emaila'), findsOneWidget);
    expect(find.text('Registracija je uspjela. Poslali smo kod za potvrdu na vaš email.'), findsOneWidget);
    expect(find.widgetWithText(TorvinTextField, 'amra@domena.ba'), findsOneWidget);
  });
}
