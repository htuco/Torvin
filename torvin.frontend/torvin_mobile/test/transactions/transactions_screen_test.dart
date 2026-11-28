import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

import '../support/fake_api.dart';

void main() {
  const createdAt = '2026-11-20T09:15:00Z';

  /// Historija vraća jednu transakciju, a uz filter smjera "Primljeno" nijednu.
  FakeApi api() => FakeApi({
        'GET /transactions': (request) => json(200, page([
              if (request.url.queryParameters['direction'] != 'Received') transactionJson(note: 'Večera'),
            ])),
        'GET /transactions/1': (_) => json(200, transactionJson(note: 'Večera')),
        'GET /cards': (_) => json(200, page([cardJson()])),
        'GET /favorite-recipients': (_) => json(200, page([])),
      });

  Map<String, String> lastQuery(FakeApi fake) => fake.sent('GET', '/transactions').last.url.queryParameters;

  Finder field(String label) => find.widgetWithText(TorvinTextField, label);

  testWidgets('grupiše po danu, a smjer i pretraga idu u upit', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions');

    expect(find.text(DateFormatter.dayHeader(DateTime.parse(createdAt))), findsOneWidget);
    expect(find.text('Marko Petrović'), findsOneWidget);
    expect(find.text('−120,00 BAM'), findsOneWidget);

    await tester.tap(find.text('Poslano'));
    await tester.pumpAndSettle();
    expect(lastQuery(fake)['direction'], 'Sent');

    await tester.enterText(field('Pretraga'), 'večera');
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    expect(lastQuery(fake)['fts'], 'večera');
    expect(lastQuery(fake)['direction'], 'Sent');
  });

  testWidgets('prazno uz filter nudi "Ukloni filtere", koje vraća historiju bez filtera', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions');

    await tester.tap(find.text('Primljeno'));
    await tester.pumpAndSettle();
    expect(find.text('Nema rezultata'), findsOneWidget);

    await tester.tap(find.text('Ukloni filtere'));
    await tester.pumpAndSettle();
    expect(lastQuery(fake).containsKey('direction'), isFalse);
    expect(find.text('Marko Petrović'), findsOneWidget);
  });

  testWidgets('filter iznosa: "do" manje od "od" ne prolazi, a ispravan raspon ide u upit', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions');

    await tester.tap(find.text('Iznos'));
    await tester.pumpAndSettle();
    await tester.enterText(field('Od'), '200');
    await tester.enterText(field('Do'), '50');
    await tester.tap(find.text('Primijeni'));
    await tester.pumpAndSettle();
    expect(find.text('Iznos do ne može biti manji od iznosa od.'), findsOneWidget);

    await tester.enterText(field('Do'), '300');
    await tester.tap(find.text('Primijeni'));
    await tester.pumpAndSettle();
    expect(lastQuery(fake)['amountFrom'], '200.0');
    expect(lastQuery(fake)['amountTo'], '300.0');
    expect(find.text('200,00 – 300,00'), findsOneWidget);

    await tester.tap(find.byTooltip('Ukloni filter'));
    await tester.pumpAndSettle();
    expect(lastQuery(fake).containsKey('amountFrom'), isFalse);
  });

  testWidgets('dodir na red otvara detalje transakcije', (tester) async {
    await pumpAt(tester, api(), '/transactions');

    await tester.tap(find.text('Marko Petrović'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Detalji transakcije'), findsOneWidget);
    expect(find.text('Večera'), findsOneWidget);
  });
}
