import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  Future<GlobalKey<FormState>> pump(WidgetTester tester, {String? helper, String? forceErrorText}) async {
    final formKey = GlobalKey<FormState>();
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.lightTheme,
      home: Scaffold(
        body: Form(
          key: formKey,
          child: TorvinTextField(
            label: 'Email',
            hint: 'ime@domena.ba',
            helper: helper,
            controller: TextEditingController(),
            forceErrorText: forceErrorText,
            validator: Validators.required('Email je obavezan.'),
          ),
        ),
      ),
    ));
    return formKey;
  }

  testWidgets('labela stoji iznad polja, a ne u njemu', (tester) async {
    await pump(tester);

    final label = tester.getRect(find.text('Email'));
    final field = tester.getRect(find.byType(TextField));
    expect(label.bottom, lessThanOrEqualTo(field.top));
    expect(tester.widget<TextField>(find.byType(TextField)).decoration!.labelText, isNull);
    expect(find.text('ime@domena.ba'), findsOneWidget);
  });

  testWidgets('greška ide ispod polja s ikonom i zamjenjuje objašnjenje formata', (tester) async {
    final formKey = await pump(tester, helper: 'Do 256 znakova');
    expect(find.text('Do 256 znakova'), findsOneWidget);

    formKey.currentState!.validate();
    await tester.pump();

    expect(find.text('Email je obavezan.'), findsOneWidget);
    expect(find.byIcon(LucideIcons.circleAlert), findsOneWidget);
    expect(find.text('Do 256 znakova'), findsNothing);
    expect(
      tester.getRect(find.text('Email je obavezan.')).top,
      greaterThan(tester.getRect(find.byType(TextField)).bottom),
    );
  });

  testWidgets('greška s backenda se prikazuje bez validacije forme', (tester) async {
    await pump(tester, forceErrorText: 'Email je već registrovan.');

    expect(find.text('Email je već registrovan.'), findsOneWidget);
  });

  testWidgets('validator čita tekst iz controllera i kad ga ekran sam upiše', (tester) async {
    final controller = TextEditingController();
    final formKey = GlobalKey<FormState>();
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Form(
          key: formKey,
          child: TorvinTextField(
            label: 'Email',
            controller: controller,
            validator: Validators.required('Email je obavezan.'),
          ),
        ),
      ),
    ));

    controller.text = 'amra@domena.ba';
    expect(formKey.currentState!.validate(), isTrue);
  });
}
