import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_exception.dart';
import 'auth_providers.dart';
import 'auth_session.dart';
import 'client_app.dart';

/// Stanje prijave cijele aplikacije: `null` znači odjavljen. Router prati ovo stanje i sam vodi na prijavu.
class AuthController extends AsyncNotifier<AuthSession?> {
  static const String expiredMessage = 'Prijava je istekla. Prijavite se ponovo.';

  @override
  Future<AuthSession?> build() async {
    final storage = ref.read(sessionStorageProvider);
    final AuthSession? session;
    try {
      session = await storage.read();
    } on Object {
      // Skladište sistema nije dostupno: korisnik se prijavljuje ponovo umjesto da aplikacija stane
      return null;
    }

    if (session == null) {
      return null;
    }

    if (session.isExpired) {
      await storage.clear();
      return null;
    }

    return session;
  }

  /// Greška (pogrešni podaci, pogrešna aplikacija) ide pozivaocu, da je forma prikaže.
  Future<void> signIn({required String login, required String password, required ClientApp app}) async {
    final session = await ref.read(authRepositoryProvider).login(login: login, password: password, app: app);
    await ref.read(sessionStorageProvider).write(session);
    state = AsyncData(session);
  }

  /// Korisnik odmah ide na prijavu. Token se opoziva na serveru, a lokalno se briše i kad poziv ne uspije.
  Future<void> signOut() async {
    state = const AsyncData(null);
    try {
      await ref.read(authRepositoryProvider).logout();
    } on ApiException {
      // Bez mreže ili s već opozvanim tokenom odjava je i dalje uspješna za korisnika
    } finally {
      await ref.read(sessionStorageProvider).clear();
    }
  }

  /// Poziva ga `ApiClient` na 401: token je istekao ili je opozvan.
  Future<void> expire() async {
    if (state.value == null) {
      return;
    }

    ref.read(sessionNoticeProvider).show(expiredMessage);
    state = const AsyncData(null);
    await ref.read(sessionStorageProvider).clear();
  }
}
