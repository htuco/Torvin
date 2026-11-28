import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';
import 'package:torvin_mobile/features/recipients/data/recipient_providers.dart';

import '../support/fake_api.dart';

void main() {
  const insufficient = 'Nedovoljno sredstava na kartici •••• 4821.';

  FakeApi api({
    List<Map<String, dynamic>>? activeCards,
    int searchStatus = 200,
    Object? searchBody,
    int favoritesStatus = 200,
    int createStatus = 201,
    Object? createBody,
    String expectedCode = 'torvin:v1:dobar',
  }) =>
      FakeApi({
        'GET /card-statuses': (_) => json(200, page(cardStatusesJson)),
        'GET /cards': (_) => json(200, page(activeCards ?? [cardJson()])),
        'GET /favorite-recipients': (_) => favoritesStatus == 200
            ? json(200, page([favoriteJson()]))
            : json(favoritesStatus, {'message': 'Došlo je do greške. Pokušajte ponovo.'}),
        'GET /recipients/search': (_) => json(searchStatus, searchBody ?? page([recipientJson()])),
        'POST /transactions': (_) => json(createStatus, createBody ?? transactionJson()),
        'POST /recipients/resolve-code': (request) {
          final code = (jsonDecode(request.body) as Map<String, dynamic>)['code'];
          return code == expectedCode
              ? json(200, {
                  'userId': 6,
                  'firstName': 'Selma',
                  'lastName': 'Hadžić',
                  'username': 'selma',
                  'network': 'Visa',
                  'last4Digits': '9876',
                  'currencyCode': 'BAM',
                  'amount': 50.0,
                })
              : json(400, {
                  'message': 'Neispravni podaci.',
                  'errors': {
                    'code': ['QR kod je istekao. Zatražite novi od primaoca.'],
                  },
                });
        },
      });

  Finder field(String label) => find.widgetWithText(TorvinTextField, label);

  String fieldText(WidgetTester tester, String label) =>
      tester.widget<TextField>(find.descendant(of: field(label), matching: find.byType(TextField))).controller!.text;

  Future<void> tapButton(WidgetTester tester, String label) async {
    await tester.ensureVisible(find.text(label).last);
    await tester.tap(find.text(label).last);
    await tester.pumpAndSettle();
  }

  Future<void> chooseMarko(WidgetTester tester) async {
    await tester.enterText(field('Primalac'), 'mar');
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Marko Petrović'));
    await tester.pumpAndSettle();
  }

  testWidgets('iznos: nula, tri decimale i više od balansa padaju bez zahtjeva, a minus se ne može ukucati',
      (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions/new');
    await chooseMarko(tester);

    await tester.enterText(field('Iznos'), '0');
    await tapButton(tester, 'Nastavi');
    expect(find.text(Validators.amountMessage), findsOneWidget);

    await tester.enterText(field('Iznos'), '1,234');
    await tester.pumpAndSettle();
    expect(find.text(Validators.amountMessage), findsOneWidget);

    await tester.enterText(field('Iznos'), '1.500,00');
    await tester.pumpAndSettle();
    expect(find.text(insufficient), findsOneWidget);

    await tester.enterText(field('Iznos'), '-5');
    await tester.pumpAndSettle();
    expect(fieldText(tester, 'Iznos'), '5');

    await tapButton(tester, 'Nastavi');
    await tapButton(tester, 'Odustani');
    expect(fake.sent('POST', '/transactions'), isEmpty);
  });

  testWidgets('primalac iz pretrage, potvrda u dijalogu i slanje s mojom karticom', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/transactions/new');

    expect(find.text('Dostupno 1.245,00 BAM'), findsOneWidget);
    await tapButton(tester, 'Nastavi');
    expect(find.text('Izaberite primaoca iz rezultata, omiljenih ili QR koda.'), findsOneWidget);
    expect(find.text('Iznos je obavezan.'), findsOneWidget);

    await chooseMarko(tester);
    expect(fake.sent('GET', '/recipients/search').single.url.queryParameters['fts'], 'mar');
    expect(find.text('Promijeni'), findsOneWidget);

    await tester.tap(find.text('50'));
    await tester.pumpAndSettle();
    expect(fieldText(tester, 'Iznos'), '50,00');
    await tester.enterText(field('Iznos'), '120');
    await tester.enterText(field('Napomena (opciono)'), 'Večera');

    await tapButton(tester, 'Nastavi');
    expect(find.text('Poslati 120,00 BAM?'), findsOneWidget);
    expect(find.text('Marko Petrović (@marko.p)'), findsOneWidget);
    await tapButton(tester, 'Odustani');
    expect(fake.sent('POST', '/transactions'), isEmpty);

    await tapButton(tester, 'Nastavi');
    await tester.tap(find.widgetWithText(FilledButton, 'Pošalji'));
    await tester.pumpAndSettle();

    final body = jsonDecode(fake.sent('POST', '/transactions').single.body) as Map<String, dynamic>;
    expect(body, {'fromCardId': 7, 'toUserId': 5, 'amount': 120, 'note': 'Večera'});
    expect(find.text('Poslali ste 120,00 BAM'), findsOneWidget);
    expect(find.text('Primalac: Marko Petrović'), findsOneWidget);
    expect(find.text('TRV-261120-0001'), findsOneWidget);
  });

  testWidgets('greška iznosa sa servera stoji ispod polja, a forma ostaje', (tester) async {
    final fake = api(createStatus: 400, createBody: {
      'message': 'Neispravni podaci.',
      'errors': {
        'amount': [insufficient],
      },
    });
    await pumpAt(tester, fake, '/transactions/new');
    await chooseMarko(tester);
    await tester.enterText(field('Iznos'), '100');

    await tapButton(tester, 'Nastavi');
    await tester.tap(find.widgetWithText(FilledButton, 'Pošalji'));
    await tester.pumpAndSettle();

    expect(fake.sent('POST', '/transactions'), hasLength(1));
    expect(find.text(insufficient), findsOneWidget);
    expect(find.text('Nastavi'), findsOneWidget);
  });

  testWidgets('QR: istekao kod javlja grešku ispod primaoca, a ispravan popuni primaoca i iznos', (tester) async {
    var scanned = 'torvin:v1:istekao';
    final fake = api();
    await pumpAt(
      tester,
      fake,
      '/transactions/new',
      overrides: [scanCodeProvider.overrideWithValue((_) async => scanned)],
    );

    await tester.tap(find.byTooltip('Skeniraj QR kod'));
    await tester.pumpAndSettle();
    expect(find.text('QR kod je istekao. Zatražite novi od primaoca.'), findsOneWidget);

    scanned = 'torvin:v1:dobar';
    await tester.tap(find.byTooltip('Skeniraj QR kod'));
    await tester.pumpAndSettle();
    expect(find.text('QR kod je istekao. Zatražite novi od primaoca.'), findsNothing);
    expect(find.text('Selma Hadžić'), findsOneWidget);
    expect(find.text('@selma · Visa •••• 9876'), findsOneWidget);
    expect(fieldText(tester, 'Iznos'), '50,00');
  });

  testWidgets('omiljeni iz reda se biraju jednim dodirom, s nadimkom ispod imena', (tester) async {
    await pumpAt(tester, api(), '/transactions/new');

    await tester.tap(find.text('Cimerka'));
    await tester.pumpAndSettle();
    expect(find.text('Selma Hadžić'), findsOneWidget);
    expect(find.text('@selma · Cimerka'), findsOneWidget);
    expect(find.text('Promijeni'), findsOneWidget);
  });

  testWidgets('greška pretrage s backenda stoji ispod polja s porukom polja', (tester) async {
    const tooLong = 'Pretraga može imati najviše 100 znakova.';
    await pumpAt(
      tester,
      api(searchStatus: 400, searchBody: {
        'message': 'Podaci nisu ispravni.',
        'errors': {
          'fts': [tooLong],
        },
      }),
      '/transactions/new',
    );

    await tester.enterText(field('Primalac'), 'marko');
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    expect(find.text(tooLong), findsOneWidget);
    expect(find.text('Podaci nisu ispravni.'), findsNothing);
  });

  testWidgets('pretraga prima najviše 100 znakova, kako piše ispod polja', (tester) async {
    await pumpAt(tester, api(), '/transactions/new');

    expect(find.text('Unesite od 3 do 100 znakova.'), findsOneWidget);
    await tester.enterText(field('Primalac'), 'a' * 150);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    expect(fieldText(tester, 'Primalac'), hasLength(100));
  });

  testWidgets('omiljeni koji se nisu učitali ne ruše formu, a ispod piše zašto', (tester) async {
    await pumpAt(tester, api(favoritesStatus: 500), '/transactions/new');

    expect(find.text('Omiljeni primaoci se nisu učitali. Došlo je do greške. Pokušajte ponovo.'), findsOneWidget);
    await chooseMarko(tester);
    expect(find.text('Promijeni'), findsOneWidget);
  });

  testWidgets('bez aktivne kartice forma se ne otvara, a nudi se dodavanje kartice', (tester) async {
    await pumpAt(tester, api(activeCards: []), '/transactions/new');

    expect(find.text('Za slanje je potrebna kartica'), findsOneWidget);
    expect(find.text('Dodaj virtuelnu karticu'), findsOneWidget);
    expect(find.text('Nastavi'), findsNothing);
  });
}
