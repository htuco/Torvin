import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

import '../support/fake_api.dart';

void main() {
  FakeApi api({required int verifyStatus, required Object verifyBody}) => FakeApi({
        'GET /cards/7': (_) => json(200, cardJson(status: 'Pending', statusName: 'Na čekanju')),
        'GET /transactions': (_) => json(200, page([])),
        'POST /cards/7/verify': (_) => json(verifyStatus, verifyBody),
      });

  Finder codeInput() => find.descendant(of: find.byType(OtpField), matching: find.byType(TextField));

  testWidgets('prazan i kratak kod pokazuju poruku ispod polja i ne šalju zahtjev', (tester) async {
    final fake = api(verifyStatus: 200, verifyBody: cardJson());
    await pumpAt(tester, fake, '/cards/7/activate');

    await tester.tap(find.text('Aktiviraj'));
    await tester.pumpAndSettle();
    expect(find.text('Kod je obavezan.'), findsOneWidget);

    await tester.enterText(codeInput(), '12345');
    await tester.tap(find.text('Aktiviraj'));
    await tester.pumpAndSettle();
    expect(find.text(Validators.codeMessage), findsOneWidget);
    expect(fake.sent('POST', '/cards/7/verify'), isEmpty);
  });

  testWidgets('ispravan kod aktivira karticu i vraća na detalje s porukom', (tester) async {
    final fake = api(verifyStatus: 200, verifyBody: cardJson());
    await pumpAt(tester, fake, '/cards/7/activate');

    await tester.enterText(codeInput(), '482193');
    await tester.tap(find.text('Aktiviraj'));
    await tester.pumpAndSettle();

    final request = fake.sent('POST', '/cards/7/verify').single;
    expect(jsonDecode(request.body), {'code': '482193'});
    expect(find.text('Kartica •••• 4821 je aktivirana.'), findsOneWidget);
    expect(find.text('Aktivacija kartice'), findsNothing);
  });

  testWidgets('pogrešan kod ostaje na ekranu s porukom backenda', (tester) async {
    final fake = api(
      verifyStatus: 400,
      verifyBody: {'message': 'Kod nije ispravan ili je istekao. Zatražite novi kod.'},
    );
    await pumpAt(tester, fake, '/cards/7/activate');

    await tester.enterText(codeInput(), '000000');
    await tester.tap(find.text('Aktiviraj'));
    await tester.pumpAndSettle();

    expect(find.text('Kod nije ispravan ili je istekao. Zatražite novi kod.'), findsOneWidget);
    expect(find.text('Aktivacija kartice'), findsOneWidget);
  });
}
