import 'package:flutter_test/flutter_test.dart';

import 'package:torvin_desktop/main.dart';

void main() {
  testWidgets('shows dashboard placeholder by default', (WidgetTester tester) async {
    await tester.pumpWidget(const TorvinAdminApp());

    expect(find.text('dashboard'), findsOneWidget);
  });
}
