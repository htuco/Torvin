import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';

import 'support/fakes.dart';

void main() {
  Future<void> pumpForm(WidgetTester tester, http.Response response) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [
        sessionStorageProvider.overrideWithValue(FakeSessionStorage()),
        httpClientProvider.overrideWithValue(MockClient((_) async => response)),
      ],
      child: const MaterialApp(home: Scaffold(body: LoginForm(app: ClientApp.mobile))),
    ));
  }

  Finder field(String label) => find.widgetWithText(TextFormField, label);

  testWidgets('prije unosa nema poruka, a prazna prijava ih prikazuje ispod oba polja', (tester) async {
    await pumpForm(tester, http.Response('', 204));

    expect(find.text('Korisničko ime ili email je obavezno.'), findsNothing);

    await tester.tap(find.text('Prijavi se'));
    await tester.pump();

    expect(find.text('Korisničko ime ili email je obavezno.'), findsOneWidget);
    expect(find.text('Lozinka je obavezna.'), findsOneWidget);
  });

  testWidgets('ispravan unos ne prikazuje nijednu poruku', (tester) async {
    await pumpForm(tester, http.Response('', 204));

    await tester.enterText(field('Korisničko ime ili email'), 'mobile');
    await tester.enterText(field('Lozinka'), 'test');
    await tester.pump();

    expect(find.text('Korisničko ime ili email je obavezno.'), findsNothing);
    expect(find.text('Lozinka je obavezna.'), findsNothing);
  });

  testWidgets('poruka backenda se prikazuje ispod forme i nestaje pri izmjeni', (tester) async {
    await pumpForm(tester, jsonResponse(400, {'message': 'Pogrešno korisničko ime ili lozinka.'}));

    await tester.enterText(field('Korisničko ime ili email'), 'mobile');
    await tester.enterText(field('Lozinka'), 'pogresna');
    await tester.tap(find.text('Prijavi se'));
    await tester.pumpAndSettle();

    expect(find.text('Pogrešno korisničko ime ili lozinka.'), findsOneWidget);

    await tester.enterText(field('Lozinka'), 'test');
    await tester.pump();

    expect(find.text('Pogrešno korisničko ime ili lozinka.'), findsNothing);
  });

  testWidgets('greška polja s backenda stoji ispod tog polja', (tester) async {
    await pumpForm(tester, jsonResponse(400, {
      'message': 'Podaci nisu ispravni.',
      'errors': {
        'password': ['Lozinka može imati najviše 100 znakova.'],
      },
    }));

    await tester.enterText(field('Korisničko ime ili email'), 'mobile');
    await tester.enterText(field('Lozinka'), 'test');
    await tester.tap(find.text('Prijavi se'));
    await tester.pumpAndSettle();

    expect(
      find.descendant(of: field('Lozinka'), matching: find.text('Lozinka može imati najviše 100 znakova.')),
      findsOneWidget,
    );
    expect(find.text('Podaci nisu ispravni.'), findsOneWidget);
  });
}
