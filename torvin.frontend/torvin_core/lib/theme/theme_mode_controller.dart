import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'theme_mode_storage.dart';

final themeModeStorageProvider = Provider<ThemeModeStorage>((ref) => ThemeModeStorage());

final themeModeControllerProvider = AsyncNotifierProvider<ThemeModeController, ThemeMode>(ThemeModeController.new);

/// Tema aplikacije. Dok korisnik ne klikne prekidač, prati sistem, a poslije pamti izbor na uređaju.
/// Izbor nije vezan za korisnika, pa ostaje i poslije odjave.
class ThemeModeController extends AsyncNotifier<ThemeMode> {
  @override
  Future<ThemeMode> build() async {
    try {
      return await ref.read(themeModeStorageProvider).read() ?? ThemeMode.system;
    } on Object catch (error) {
      // Skladište nije dostupno: tema prati sistem umjesto da aplikacija stane
      debugPrint('Izbor teme nije pročitan: $error');
      return ThemeMode.system;
    }
  }

  /// Prebacuje na suprotno od onoga što je sada na ekranu (i kad je tema bila sistemska).
  Future<void> toggle(Brightness current) async {
    final next = current == Brightness.dark ? ThemeMode.light : ThemeMode.dark;
    state = AsyncData(next);
    try {
      await ref.read(themeModeStorageProvider).write(next);
    } on Object catch (error) {
      // Izbor važi do zatvaranja aplikacije i kad upis ne uspije; tema nije razlog za poruku greške
      debugPrint('Izbor teme nije sačuvan: $error');
    }
  }
}
