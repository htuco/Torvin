import 'package:flutter_test/flutter_test.dart';

import 'package:torvin_mobile/main.dart';

void main() {
  testWidgets('shows dashboard placeholder by default', (WidgetTester tester) async {
    await tester.pumpWidget(const TorvinApp());

    expect(find.text('dashboard'), findsOneWidget);
  });
}
