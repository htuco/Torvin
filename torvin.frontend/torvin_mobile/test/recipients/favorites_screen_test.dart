import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:torvin_core/torvin_core.dart';

import '../support/fake_api.dart';

void main() {
  FakeApi api({List<Map<String, dynamic>>? favorites}) => FakeApi({
        'GET /favorite-recipients': (_) => json(200, page(favorites ?? [favoriteJson()])),
        'PUT /favorite-recipients/3': (request) => json(200, {
              ...favoriteJson(),
              'nickname': (jsonDecode(request.body) as Map<String, dynamic>)['nickname'],
            }),
        'DELETE /favorite-recipients/3': (_) => http.Response('', 204),
        'GET /card-statuses': (_) => json(200, page(cardStatusesJson)),
        'GET /cards': (_) => json(200, page([cardJson()])),
      });

  testWidgets('lista pokazuje nadimak, a izmjena nadimka šalje PUT', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/favorites');

    expect(find.text('Cimerka'), findsOneWidget);
    expect(find.text('Selma Hadžić · @selma'), findsOneWidget);

    await tester.tap(find.byTooltip('Izmijeni nadimak'));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TorvinTextField, 'Nadimak (opciono)'), 'Selma');
    await tester.tap(find.text('Sačuvaj'));
    await tester.pumpAndSettle();

    expect(jsonDecode(fake.sent('PUT', '/favorite-recipients/3').single.body), {'nickname': 'Selma'});
    expect(find.text('Nadimak je sačuvan.'), findsOneWidget);
  });

  testWidgets('prazan nadimak briše nadimak, uz tačnu poruku', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/favorites');

    await tester.tap(find.byTooltip('Izmijeni nadimak'));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TorvinTextField, 'Nadimak (opciono)'), '');
    await tester.tap(find.text('Sačuvaj'));
    await tester.pumpAndSettle();

    expect(jsonDecode(fake.sent('PUT', '/favorite-recipients/3').single.body), {'nickname': null});
    expect(find.text('Nadimak je uklonjen.'), findsOneWidget);
  });

  testWidgets('nepromijenjen nadimak se ne šalje', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/favorites');

    await tester.tap(find.byTooltip('Izmijeni nadimak'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Sačuvaj'));
    await tester.pumpAndSettle();

    expect(fake.sent('PUT', '/favorite-recipients/3'), isEmpty);
    expect(find.text('Nadimak je sačuvan.'), findsNothing);
  });

  testWidgets('uklanjanje traži potvrdu, a "Odustani" ne šalje zahtjev', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/favorites');

    await tester.tap(find.byTooltip('Ukloni iz omiljenih'));
    await tester.pumpAndSettle();
    expect(find.text('Ukloniti iz omiljenih?'), findsOneWidget);
    await tester.tap(find.text('Odustani'));
    await tester.pumpAndSettle();
    expect(fake.sent('DELETE', '/favorite-recipients/3'), isEmpty);

    await tester.tap(find.byTooltip('Ukloni iz omiljenih'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Ukloni'));
    await tester.pumpAndSettle();
    expect(fake.sent('DELETE', '/favorite-recipients/3'), hasLength(1));
    expect(find.text('Uklonjeno iz omiljenih: Selma Hadžić.'), findsOneWidget);
  });

  testWidgets('dodir na primaoca otvara novu transakciju s njim', (tester) async {
    await pumpAt(tester, api(), '/favorites');

    await tester.tap(find.text('Cimerka'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Nova transakcija'), findsOneWidget);
    expect(find.text('@selma · Cimerka'), findsOneWidget);
    expect(find.text('Promijeni'), findsOneWidget);
  });

  testWidgets('bez omiljenih pokazuje kako se dodaju', (tester) async {
    await pumpAt(tester, api(favorites: []), '/favorites');

    expect(find.text('Nemate omiljenih primalaca'), findsOneWidget);
    expect(find.text('Primaoca dodajete iz detalja transakcije.'), findsOneWidget);
  });
}
