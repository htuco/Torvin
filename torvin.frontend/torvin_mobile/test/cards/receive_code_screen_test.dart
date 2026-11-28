import 'package:flutter_test/flutter_test.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:torvin_core/torvin_core.dart';

import '../support/fake_api.dart';

void main() {
  const expiresAt = '2026-11-20T09:30:00Z';

  FakeApi api() => FakeApi({
        'GET /cards/7': (_) => json(200, cardJson()),
        'GET /cards/7/receive-code': (request) {
          final amount = request.url.queryParameters['amount'];
          return json(200, {
            'code': 'torvin:v1:kod-${amount ?? 'bez'}',
            'amount': amount == null ? null : double.parse(amount),
            'expiresAt': expiresAt,
          });
        },
      });

  Future<void> newCode(WidgetTester tester) async {
    await tester.ensureVisible(find.text('Novi kod'));
    await tester.tap(find.text('Novi kod'));
    await tester.pumpAndSettle();
  }

  List<String?> amounts(FakeApi fake) =>
      [for (final request in fake.sent('GET', '/cards/7/receive-code')) request.url.queryParameters['amount']];

  testWidgets('pokazuje kod i rok, a "Novi kod" traži novi kod od servera', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/cards/7/receive');

    expect(tester.widget<QrImageView>(find.byType(QrImageView)).semanticsLabel, 'QR kod za primanje novca');
    final time = DateFormatter.time(DateTime.parse(expiresAt));
    expect(find.text('Pokažite kod osobi koja vam šalje novac. Kod važi do $time.'), findsOneWidget);

    await newCode(tester);
    expect(amounts(fake), [null, null]);
  });

  testWidgets('iznos ide u kod, a neispravan iznos ne šalje zahtjev', (tester) async {
    final fake = api();
    await pumpAt(tester, fake, '/cards/7/receive');

    await tester.enterText(find.widgetWithText(TorvinTextField, 'Iznos (opciono)'), '12.50');
    await newCode(tester);
    expect(find.text(Validators.amountFormatMessage), findsOneWidget);
    expect(amounts(fake), [null]);

    await tester.enterText(find.widgetWithText(TorvinTextField, 'Iznos (opciono)'), '50');
    await newCode(tester);
    expect(amounts(fake), [null, '50.0']);
    expect(find.text('Kod traži 50,00 BAM.'), findsOneWidget);
  });
}
