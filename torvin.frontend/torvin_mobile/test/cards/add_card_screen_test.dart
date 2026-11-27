import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

import '../support/fake_api.dart';

void main() {
  // Godina iz liste uvijek važi: tri godine od danas
  final year = DateTime.now().year + 3;
  final yy = (year % 100).toString().padLeft(2, '0');

  FakeApi api({required int createStatus, required Object createBody}) => FakeApi({
        'GET /cards': (_) => json(200, page([])),
        'GET /card-statuses': (_) => json(200, page(cardStatusesJson)),
        'GET /card-types': (_) => json(200, page([
              {'id': 1, 'name': 'Debitna'},
            ])),
        'GET /currencies': (_) => json(200, page([
              {'id': 1, 'code': 'BAM', 'name': 'Konvertibilna marka'},
            ])),
        'POST /cards': (_) => json(createStatus, createBody),
        'GET /cards/7': (_) => json(200, cardJson(status: 'Pending', statusName: 'Na čekanju')),
        'GET /transactions': (_) => json(200, page([])),
      });

  Finder field(String label) => find.widgetWithText(TorvinTextField, label);

  Future<void> choose(WidgetTester tester, String hint, String item) async {
    await tester.ensureVisible(find.text(hint));
    await tester.tap(find.text(hint));
    await tester.pumpAndSettle();
    await tester.tap(find.text(item).last);
    await tester.pumpAndSettle();
  }

  Future<void> fill(WidgetTester tester, {required String number}) async {
    await choose(tester, 'Izaberite tip', 'Debitna');
    await choose(tester, 'Izaberite valutu', 'BAM · Konvertibilna marka');
    await tester.enterText(field('Broj kartice'), number);
    await choose(tester, 'MM', '03');
    await choose(tester, 'GGGG', '$year');
  }

  Future<void> submit(WidgetTester tester) async {
    await tester.ensureVisible(find.text('Dodaj karticu'));
    await tester.tap(find.text('Dodaj karticu'));
    await tester.pumpAndSettle();
  }

  testWidgets('prazna forma pokazuje poruke obaveznih polja i ne šalje zahtjev', (tester) async {
    final fake = api(createStatus: 201, createBody: cardJson());
    await pumpAt(tester, fake, '/cards/new');

    await submit(tester);

    expect(find.text('Tip kartice je obavezan.'), findsOneWidget);
    expect(find.text('Valuta je obavezna.'), findsOneWidget);
    expect(find.text('Broj kartice je obavezan.'), findsOneWidget);
    expect(find.text('Mjesec isteka je obavezan.'), findsOneWidget);
    expect(find.text('Godina isteka je obavezna.'), findsOneWidget);
    expect(fake.sent('POST', '/cards'), isEmpty);
  });

  testWidgets('izbor jednog polja ne javlja greške polja koja još nisu dirana', (tester) async {
    await pumpAt(tester, api(createStatus: 201, createBody: cardJson()), '/cards/new');

    await choose(tester, 'Izaberite tip', 'Debitna');
    await tester.enterText(field('Broj kartice'), '4111');
    await tester.pump();

    expect(find.text(Validators.cardNumberMessage), findsOneWidget);
    expect(find.text('Valuta je obavezna.'), findsNothing);
    expect(find.text('Mjesec isteka je obavezan.'), findsNothing);
    expect(find.text('Godina isteka je obavezna.'), findsNothing);
  });

  testWidgets('broj s pogrešnom kontrolnom cifrom pada na klijentu', (tester) async {
    final fake = api(createStatus: 201, createBody: cardJson());
    await pumpAt(tester, fake, '/cards/new');

    await fill(tester, number: '4111111111111112');
    await submit(tester);

    expect(find.text(Validators.cardNumberMessage), findsOneWidget);
    expect(fake.sent('POST', '/cards'), isEmpty);
  });

  testWidgets('greška datuma s backenda stoji ispod godine isteka', (tester) async {
    final fake = api(createStatus: 400, createBody: {
      'message': 'Podaci nisu ispravni.',
      'errors': {
        'expiry': ['Kartica je istekla. Unesite karticu koja još važi.'],
      },
    });
    await pumpAt(tester, fake, '/cards/new');

    await fill(tester, number: '4111111111111111');
    await submit(tester);

    expect(find.text('Kartica je istekla. Unesite karticu koja još važi.'), findsOneWidget);
    expect(find.text('Nova kartica'), findsOneWidget);
  });

  testWidgets('snimljena kartica vodi na unos koda, a broj ide samo u tijelo zahtjeva', (tester) async {
    final fake = api(createStatus: 201, createBody: cardJson(status: 'Pending', statusName: 'Na čekanju'));
    await pumpAt(tester, fake, '/cards/new');

    await fill(tester, number: '4111111111111111');
    await submit(tester);

    final request = fake.sent('POST', '/cards').single;
    expect(jsonDecode(request.body), {
      'cardTypeId': 1,
      'currencyId': 1,
      'name': null,
      'cardNumber': '4111111111111111',
      'expiry': '03/$yy',
    });
    expect(request.url.hasQuery, isFalse);
    expect(find.text('Aktivacija kartice'), findsOneWidget);
  });
}
