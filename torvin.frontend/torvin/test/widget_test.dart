import 'package:flutter_test/flutter_test.dart';

import 'package:torvin/main.dart';

void main() {
  testWidgets('renders bootstrap screen', (WidgetTester tester) async {
    await tester.pumpWidget(const TorvinApp());

    expect(find.text('Torvin'), findsOneWidget);
    expect(find.text('Project bootstrap is ready'), findsOneWidget);
  });
}
