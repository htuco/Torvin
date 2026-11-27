import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  /// Otvara potvrdni dijalog dugmetom i vraća ono što je dijalog vratio, kad se zatvori.
  Future<List<bool>> pump(WidgetTester tester) async {
    final results = <bool>[];
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.lightTheme,
      home: Scaffold(
        body: Builder(
          builder: (context) => TextButton(
            onPressed: () async => results.add(await showConfirmDialog(
              context,
              title: 'Blokirati karticu •••• 4821?',
              message: 'Blokirana kartica ne može slati ni primati novac.',
              confirmLabel: 'Blokiraj',
            )),
            child: const Text('Otvori'),
          ),
        ),
      ),
    ));
    await tester.tap(find.text('Otvori'));
    await tester.pumpAndSettle();
    return results;
  }

  testWidgets('prikazuje naslov i poruku, a potvrda vraća true', (tester) async {
    final results = await pump(tester);

    expect(find.text('Blokirati karticu •••• 4821?'), findsOneWidget);
    expect(find.text('Blokirana kartica ne može slati ni primati novac.'), findsOneWidget);

    await tester.tap(find.text('Blokiraj'));
    await tester.pumpAndSettle();
    expect(results, [true]);
    expect(find.text('Blokirati karticu •••• 4821?'), findsNothing);
  });

  testWidgets('"Odustani" i X vraćaju false', (tester) async {
    final results = await pump(tester);
    await tester.tap(find.text('Odustani'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Otvori'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(results, [false, false]);
  });

  testWidgets('ekran iza se zatamni filterom svjetline, bez providnog sloja', (tester) async {
    await pump(tester);

    final filter = tester.widget<BackdropFilter>(find.byType(BackdropFilter));
    expect(filter.filter, isA<ColorFilter>());
    // Zatamnjenje nije prozirna boja pozadine dijaloga, nego filter iznad
    for (final barrier in tester.widgetList<ModalBarrier>(find.byType(ModalBarrier))) {
      expect(barrier.color, anyOf(isNull, Colors.transparent));
    }
  });
}
