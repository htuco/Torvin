import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';

import 'package:torvin_desktop/main.dart';

class _EmptySessionStorage implements SessionStorage {
  @override
  Future<AuthSession?> read() async => null;

  @override
  Future<void> write(AuthSession session) async {}

  @override
  Future<void> clear() async {}
}

void main() {
  testWidgets('bez sesije prikazuje prijavu za administratore', (WidgetTester tester) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [
        sessionStorageProvider.overrideWithValue(_EmptySessionStorage()),
        httpClientProvider.overrideWithValue(MockClient((_) async => http.Response('', 204))),
      ],
      child: const TorvinAdminApp(),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Prijava za administratore'), findsOneWidget);
    expect(find.text('Prijavi se'), findsOneWidget);
  });
}
