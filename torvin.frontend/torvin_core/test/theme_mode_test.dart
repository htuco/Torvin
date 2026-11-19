import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

import 'support/fakes.dart';

void main() {
  ProviderContainer containerWith(FakeThemeModeStorage storage) {
    final container = ProviderContainer(overrides: [themeModeStorageProvider.overrideWithValue(storage)]);
    addTearDown(container.dispose);
    return container;
  }

  test('bez sačuvanog izbora tema prati sistem', () async {
    final container = containerWith(FakeThemeModeStorage());

    expect(await container.read(themeModeControllerProvider.future), ThemeMode.system);
  });

  test('sačuvan izbor se učitava pri startu', () async {
    final container = containerWith(FakeThemeModeStorage(ThemeMode.dark));

    expect(await container.read(themeModeControllerProvider.future), ThemeMode.dark);
  });

  test('nedostupno skladište ne ruši aplikaciju, tema prati sistem', () async {
    final container = containerWith(FakeThemeModeStorage()..failRead = true);

    expect(await container.read(themeModeControllerProvider.future), ThemeMode.system);
  });

  test('prekidač prebacuje na suprotno od prikazanog i pamti izbor', () async {
    final storage = FakeThemeModeStorage();
    final container = containerWith(storage);
    await container.read(themeModeControllerProvider.future);

    await container.read(themeModeControllerProvider.notifier).toggle(Brightness.light);
    expect(container.read(themeModeControllerProvider).value, ThemeMode.dark);
    expect(storage.mode, ThemeMode.dark);

    await container.read(themeModeControllerProvider.notifier).toggle(Brightness.dark);
    expect(container.read(themeModeControllerProvider).value, ThemeMode.light);
    expect(storage.mode, ThemeMode.light);
  });

  testWidgets('dugme pokazuje mjesec u svijetloj temi, a klik prebacuje aplikaciju u tamnu', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [themeModeStorageProvider.overrideWithValue(FakeThemeModeStorage(ThemeMode.light))],
        child: Consumer(
          builder: (context, ref, _) => MaterialApp(
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: ref.watch(themeModeControllerProvider).value ?? ThemeMode.system,
            home: const Scaffold(body: Center(child: ThemeModeButton())),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byIcon(LucideIcons.moon), findsOneWidget);

    await tester.tap(find.byType(ThemeModeButton));
    await tester.pumpAndSettle();

    expect(find.byIcon(LucideIcons.sun), findsOneWidget);
    expect(find.byTooltip('Svijetla tema'), findsOneWidget);
  });
}
