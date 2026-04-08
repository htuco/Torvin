import 'package:flutter_test/flutter_test.dart';

import 'package:torvin/main.dart';

void main() {
  testWidgets('renders dashboard placeholder by default', (WidgetTester tester) async {
    await tester.pumpWidget(const TorvinApp());

    expect(find.text('dashboard'), findsOneWidget);
  });
}
