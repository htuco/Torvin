import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  late TextEditingController controller;
  late GlobalKey<FormState> formKey;

  Future<void> pump(WidgetTester tester) async {
    controller = TextEditingController();
    formKey = GlobalKey<FormState>();
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.lightTheme,
      home: Scaffold(
        body: Form(
          key: formKey,
          child: OtpField(
            label: 'Kod iz emaila',
            controller: controller,
            validator: Validators.combine([Validators.required('Kod je obavezan.'), Validators.code]),
          ),
        ),
      ),
    ));
  }

  /// Cifre u kućicama (Text), bez nevidljivog polja za unos (EditableText).
  Iterable<String> boxDigits(WidgetTester tester) => tester
      .widgetList<Text>(find.descendant(of: find.byType(Row), matching: find.byType(Text)))
      .map((t) => t.data!);

  testWidgets('prikazuje šest kućica i svaku cifru u svojoj kućici', (tester) async {
    await pump(tester);

    await tester.enterText(find.byType(TextField), '482');
    await tester.pump();

    expect(boxDigits(tester), ['4', '8', '2']);
    expect(controller.text, '482');
  });

  testWidgets('prihvata samo cifre i najviše šest (lijepljenje koda)', (tester) async {
    await pump(tester);

    await tester.enterText(find.byType(TextField), '12a34567');
    await tester.pump();

    expect(controller.text, '123456');
    expect(boxDigits(tester), ['1', '2', '3', '4', '5', '6']);
  });

  testWidgets('nepotpun kod prikazuje grešku ispod kućica', (tester) async {
    await pump(tester);

    await tester.enterText(find.byType(TextField), '123');
    formKey.currentState!.validate();
    await tester.pump();

    expect(find.text(Validators.codeMessage), findsOneWidget);
    expect(find.byIcon(LucideIcons.circleAlert), findsOneWidget);
  });
}
