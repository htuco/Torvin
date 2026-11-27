import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_mobile/features/cards/presentation/card_actions.dart';

import '../support/fake_api.dart';

void main() {
  FakeApi api(Map<String, dynamic> card) => FakeApi({
        'GET /cards/7': (_) => json(200, card),
        'GET /transactions': (request) => json(200, page([
              if (request.url.queryParameters['direction'] != 'Received') transactionJson(),
            ])),
        'POST /cards/7/block': (_) => json(200, cardJson(status: 'Blocked', statusName: 'Blokirana')),
      });

  /// Pločica je onemogućena kad njen `InkWell` nema `onTap`.
  bool tileEnabled(WidgetTester tester, String label) =>
      tester.widget<InkWell>(find.ancestor(of: find.text(label), matching: find.byType(InkWell)).first).onTap != null;

  testWidgets('ukradena kartica: sve akcije su onemogućene, uz razlog', (tester) async {
    final fake = api(cardJson(status: 'Stolen', statusName: 'Ukradena'));
    await pumpAt(tester, fake, '/cards/7');

    expect(tileEnabled(tester, 'Blokiraj'), isFalse);
    expect(tileEnabled(tester, 'Prijavi'), isFalse);
    expect(tileEnabled(tester, 'QR za primanje'), isFalse);
    expect(find.text(stolenReason), findsOneWidget);
    expect(find.text('Deblokiraj'), findsNothing);
    expect(find.text('Aktiviraj karticu'), findsNothing);

    await tester.tap(find.text('Blokiraj'));
    await tester.pumpAndSettle();
    expect(find.text('Blokirati karticu •••• 4821?'), findsNothing);
    expect(fake.requests.where((r) => r.method == 'POST'), isEmpty);
  });

  testWidgets('aktivna kartica: blokiranje traži potvrdu, a "Odustani" ne šalje zahtjev', (tester) async {
    final fake = api(cardJson());
    await pumpAt(tester, fake, '/cards/7');

    expect(tileEnabled(tester, 'Blokiraj'), isTrue);
    await tester.scrollUntilVisible(find.text('Marko Petrović'), 200, scrollable: find.byType(Scrollable).first);
    expect(find.text('−120,00 BAM'), findsOneWidget);
    await tester.scrollUntilVisible(find.text('Blokiraj'), -200, scrollable: find.byType(Scrollable).first);

    await tester.tap(find.text('Blokiraj'));
    await tester.pumpAndSettle();
    expect(find.text('Blokirati karticu •••• 4821?'), findsOneWidget);
    await tester.tap(find.text('Odustani'));
    await tester.pumpAndSettle();
    expect(fake.sent('POST', '/cards/7/block'), isEmpty);

    await tester.tap(find.text('Blokiraj'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Blokiraj'));
    await tester.pumpAndSettle();
    expect(fake.sent('POST', '/cards/7/block'), hasLength(1));
    expect(find.text('Kartica •••• 4821 je blokirana.'), findsOneWidget);
  });

  testWidgets('kartica koju je blokirao administrator: Deblokiraj je onemogućeno, a Prijavi nije', (tester) async {
    final fake = api(cardJson(status: 'Blocked', statusName: 'Blokirana', blockedByAdmin: true));
    await pumpAt(tester, fake, '/cards/7');

    expect(tileEnabled(tester, 'Deblokiraj'), isFalse);
    expect(tileEnabled(tester, 'Prijavi'), isTrue);
    expect(find.text(blockedByAdminReason), findsOneWidget);

    await tester.tap(find.text('Deblokiraj'));
    await tester.pumpAndSettle();
    expect(fake.sent('POST', '/cards/7/unblock'), isEmpty);
  });

  testWidgets('historija je historija te kartice, a filter smjera šalje direction', (tester) async {
    final fake = api(cardJson());
    await pumpAt(tester, fake, '/cards/7');

    expect(fake.sent('GET', '/transactions').first.url.queryParameters['cardId'], '7');

    final received = find.widgetWithText(ChoiceChip, 'Primljeno');
    await tester.scrollUntilVisible(received, 200, scrollable: find.byType(Scrollable).first);
    await tester.tap(received);
    await tester.pumpAndSettle();

    expect(fake.sent('GET', '/transactions').last.url.queryParameters['direction'], 'Received');
    expect(find.text('Nema primljenih transakcija'), findsOneWidget);
  });

  testWidgets('prijava bez izabranog razloga ne zatvara dijalog ni ne šalje zahtjev', (tester) async {
    final fake = api(cardJson());
    await pumpAt(tester, fake, '/cards/7');

    await tester.tap(find.text('Prijavi'));
    await tester.pumpAndSettle();
    expect(find.text('Prijaviti karticu •••• 4821?'), findsOneWidget);
    expect(find.text('Ako ste je samo zagubili, možete je privremeno blokirati.'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Prijavi'));
    await tester.pumpAndSettle();
    expect(find.text('Razlog prijave je obavezan.'), findsOneWidget);
    expect(fake.sent('POST', '/cards/7/report'), isEmpty);
  });
}
