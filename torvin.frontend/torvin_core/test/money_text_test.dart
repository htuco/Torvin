import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  Future<Text> pump(WidgetTester tester, MoneyText money) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.lightTheme,
        home: Scaffold(body: money),
      ),
    );
    return tester.widget<Text>(find.byType(Text));
  }

  testWidgets('balans je formatiran i u boji teksta', (tester) async {
    final text = await pump(tester, const MoneyText(1245));

    expect(text.data, '1.245,00 BAM');
    expect(text.style!.color, AppTheme.lightScheme().onSurface);
  });

  testWidgets('uz predznak je priliv zelen s plusom, a odliv crven s minusom', (tester) async {
    final income = await pump(tester, const MoneyText(500, signed: true));
    expect(income.data, '+500,00 BAM');
    expect(income.style!.color, TorvinColors.light.successText);

    final expense = await pump(tester, const MoneyText(-120, signed: true));
    expect(expense.data, '${MoneyFormatter.minus}120,00 BAM');
    expect(expense.style!.color, TorvinColors.light.errorText);
  });

  testWidgets('iznos koji se zaokruži na nulu nije obojen', (tester) async {
    final text = await pump(tester, const MoneyText(-0.001, signed: true));

    expect(text.data, '0,00 BAM');
    expect(text.style!.color, AppTheme.lightScheme().onSurface);
  });

  testWidgets('negativan balans bez predznaka ima minus, ali boju teksta', (tester) async {
    final text = await pump(tester, const MoneyText(-55));

    expect(text.data, '${MoneyFormatter.minus}55,00 BAM');
    expect(text.style!.color, AppTheme.lightScheme().onSurface);
  });
}
