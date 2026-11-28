import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../support/fake_api.dart';

void main() {
  FakeApi api({List<Map<String, dynamic>> favorites = const [], int status = 200, bool canReceive = true}) =>
      FakeApi({
        'GET /transactions/1': (_) => status == 200
            ? json(200, transactionJson(note: 'Večera'))
            : json(status, {'message': 'Transakcija nije pronađena.'}),
        'GET /favorite-recipients': (_) => json(200, page(favorites)),
        'POST /favorite-recipients': (_) => canReceive
            ? json(201, favoriteJson(userId: 5, nickname: null))
            : json(400, {
                'message': 'Podaci nisu ispravni.',
                'errors': {
                  'recipientUserId': ['Primalac nije pronađen.'],
                },
              }),
      });

  OutlinedButton addButton(WidgetTester tester) => tester.widget<OutlinedButton>(
        find.ancestor(of: find.text('Dodaj u omiljene'), matching: find.byType(OutlinedButton)),
      );

  testWidgets('pokazuje podatke transakcije, a nijedan ID', (tester) async {
    await pumpAt(tester, api(), '/transactions/1');

    expect(find.text('Marko Petrović'), findsOneWidget);
    expect(find.text('−120,00 BAM'), findsOneWidget);
    expect(find.text('Izvršena'), findsOneWidget);
    expect(find.text('Prenos'), findsOneWidget);
    expect(find.text('•••• 4821'), findsOneWidget);
    expect(find.text('•••• 1234'), findsOneWidget);
    expect(find.text('Večera'), findsOneWidget);
    expect(find.text('TRV-261120-0001'), findsOneWidget);
    // ID transakcije (1), kartice (7) i druge strane (5) se ne prikazuju
    final texts = [for (final text in tester.widgetList<Text>(find.byType(Text))) text.data];
    expect(texts, isNot(contains(anyOf('1', '5', '7'))));
  });

  testWidgets('primalac koji je već omiljen: dodavanje je onemogućeno uz razlog', (tester) async {
    final fake = api(favorites: [favoriteJson(userId: 5)]);
    await pumpAt(tester, fake, '/transactions/1');
    await tester.scrollUntilVisible(find.text('Dodaj u omiljene'), 200, scrollable: find.byType(Scrollable).first);

    expect(addButton(tester).onPressed, isNull);
    expect(find.text('Primalac je već među omiljenim.'), findsOneWidget);
    final query = fake.sent('GET', '/favorite-recipients').single.url.queryParameters;
    expect(query['fts'], 'Petrović');
    expect(query['pageSize'], '100');
  });

  testWidgets('dodavanje u omiljene šalje drugu stranu i javlja uspjeh', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions/1');
    await tester.ensureVisible(find.text('Dodaj u omiljene'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Dodaj u omiljene'));
    await tester.pumpAndSettle();
    expect(jsonDecode(fake.sent('POST', '/favorite-recipients').single.body), {'recipientUserId': 5});
    expect(find.text('Dodano u omiljene: Marko Petrović.'), findsOneWidget);
  });

  testWidgets('primalac koji više ne može primati novac: poruka polja s backenda, ne opšta', (tester) async {
    await pumpAt(tester, api(canReceive: false), '/transactions/1');
    await tester.ensureVisible(find.text('Dodaj u omiljene'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Dodaj u omiljene'));
    await tester.pumpAndSettle();
    expect(find.text('Primalac nije pronađen.'), findsOneWidget);
    expect(find.text('Podaci nisu ispravni.'), findsNothing);
  });

  testWidgets('tuđa ili nepostojeća transakcija pokazuje poruku backenda', (tester) async {
    await pumpAt(tester, api(status: 404), '/transactions/1');

    expect(find.text('Transakcija se nije učitala'), findsOneWidget);
    expect(find.text('Transakcija nije pronađena.'), findsOneWidget);
  });
}
