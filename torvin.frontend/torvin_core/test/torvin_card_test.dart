import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';
import 'package:u_credit_card/u_credit_card.dart';

void main() {
  Future<void> pump(WidgetTester tester, {bool blocked = false}) => tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.lightTheme,
      home: Scaffold(
        body: SingleChildScrollView(
          child: TorvinCard(
            last4Digits: '4821',
            holderName: 'Amra Hodžić',
            expiresAt: DateTime(2028, 5, 31),
            color: TorvinColors.light.cardPrimary,
            typeName: 'Debitna',
            statusLabel: blocked ? 'Blokirana' : 'Aktivna',
            statusTone: blocked ? StatusTone.error : StatusTone.success,
            balance: 1245,
            blocked: blocked,
          ),
        ),
      ),
    ),
  );

  Iterable<String> texts(WidgetTester tester) =>
      tester.widgetList<Text>(find.byType(Text)).map((t) => t.data ?? t.textSpan?.toPlainText() ?? '');

  testWidgets('prikazuje zadnje 4 cifre, tip, status i balans u formatu aplikacije', (tester) async {
    await pump(tester);

    final all = texts(tester).join(' ');
    expect(all, contains('4821'));
    expect(find.text('Debitna'), findsOneWidget);
    expect(find.text('Aktivna'), findsOneWidget);
    expect(find.text('1.245,00 BAM'), findsOneWidget);
    expect(all, contains('05/28'));
    // Paket ne ispisuje svoj balans ("$1245.00") ni engleski naziv tipa
    expect(all, isNot(contains(r'$')));
    expect(all, isNot(contains('Credit')));
  });

  testWidgets('nijedan tekst nema više od 4 uzastopne cifre broja kartice', (tester) async {
    await pump(tester);

    for (final text in texts(tester)) {
      expect(RegExp(r'\d{5,}').hasMatch(text.replaceAll(' ', '')), isFalse, reason: text);
    }
  });

  testWidgets('blokirana kartica ima katanac i sivoplavu boju', (tester) async {
    await pump(tester, blocked: true);

    expect(find.byIcon(LucideIcons.lock), findsOneWidget);
    expect(find.text('Blokirana'), findsOneWidget);
    final card = tester.widget<CreditCardUi>(find.byType(CreditCardUi));
    expect(card.topLeftColor, TorvinColors.light.cardBlocked);
    expect(card.bottomRightColor, TorvinColors.light.cardBlocked);
  });

  testWidgets('mreža kartice daje logo paketa, a bez mreže logo ne postoji', (tester) async {
    Future<CreditCardType?> typeFor(CardNetwork? network) async {
      await tester.pumpWidget(MaterialApp(
        theme: AppTheme.lightTheme,
        home: Scaffold(
          body: SingleChildScrollView(
            child: TorvinCard(
              last4Digits: '4821',
              holderName: 'Amra Hodžić',
              expiresAt: DateTime(2028, 5, 31),
              color: TorvinColors.light.cardPrimary,
              typeName: 'Debitna',
              statusLabel: 'Aktivna',
              statusTone: StatusTone.success,
              balance: 1245,
              network: network,
            ),
          ),
        ),
      ));
      return tester.widget<CreditCardUi>(find.byType(CreditCardUi)).creditCardType;
    }

    expect(await typeFor(CardNetwork.visa), CreditCardType.visa);
    expect(await typeFor(CardNetwork.mastercard), CreditCardType.mastercard);
    expect(await typeFor(null), CreditCardType.none);
  });

  test('maskirani broj ima 16 znakova i završava zadnjim ciframa', () {
    expect(TorvinCard.maskedNumber('4821'), '************4821');
  });
}
