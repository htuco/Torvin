import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  Future<void> pump(WidgetTester tester, ThemeData theme, StatusTone tone) => tester.pumpWidget(
    MaterialApp(
      theme: theme,
      home: Scaffold(
        body: Center(
          child: StatusChip(label: 'Aktivna', tone: tone),
        ),
      ),
    ),
  );

  Color textColor(WidgetTester tester) => tester.widget<Text>(find.text('Aktivna')).style!.color!;

  Color dotColor(WidgetTester tester) {
    final dot = tester
        .widgetList<Container>(find.byType(Container))
        .firstWhere((c) => (c.decoration as BoxDecoration?)?.shape == BoxShape.circle);
    return (dot.decoration! as BoxDecoration).color!;
  }

  testWidgets('prikazuje riječ i tačku u boji tona (svijetla tema)', (tester) async {
    await pump(tester, AppTheme.lightTheme, StatusTone.success);

    expect(find.text('Aktivna'), findsOneWidget);
    expect(textColor(tester), TorvinColors.light.successChip);
    expect(dotColor(tester), TorvinColors.light.successDot);
  });

  testWidgets('svaki ton ima svoju boju i u tamnoj temi', (tester) async {
    const dark = TorvinColors.dark;
    final expected = {
      StatusTone.success: dark.successChip,
      StatusTone.warning: dark.warningChip,
      StatusTone.error: dark.errorChip,
      StatusTone.neutral: dark.neutralChip,
    };

    for (final entry in expected.entries) {
      await pump(tester, AppTheme.darkTheme, entry.key);
      expect(textColor(tester), entry.value, reason: entry.key.name);
    }
  });
}
