import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:torvin_core/torvin_core.dart';

import 'support/fakes.dart';

void main() {
  group('AuthSession', () {
    test('JSON tamo i nazad čuva sva polja, vrijeme u UTC', () {
      final session = AuthSession.fromJson({
        'token': 'abc',
        'expiresAt': '2026-11-19T12:00:00Z',
        'role': 'Admin',
        'firstName': 'Amra',
        'lastName': 'Hodžić',
      });
      final copy = AuthSession.fromJson(session.toJson());

      expect(copy.token, 'abc');
      expect(copy.expiresAt, DateTime.utc(2026, 11, 19, 12));
      expect(copy.role, 'Admin');
      expect(copy.lastName, 'Hodžić');
    });

    test('isExpired', () {
      expect(sessionExpiringIn(const Duration(minutes: 5)).isExpired, isFalse);
      expect(sessionExpiringIn(const Duration(minutes: -1)).isExpired, isTrue);
    });
  });

  group('AuthController', () {
    late FakeSessionStorage storage;
    late List<http.Request> requests;

    ProviderContainer containerWith(http.Response Function(http.Request) handler) {
      requests = [];
      final container = ProviderContainer(overrides: [
        sessionStorageProvider.overrideWithValue(storage),
        httpClientProvider.overrideWithValue(MockClient((request) async {
          requests.add(request);
          return handler(request);
        })),
      ]);
      addTearDown(container.dispose);
      return container;
    }

    test('važeća sesija iz skladišta znači prijavljen', () async {
      storage = FakeSessionStorage(sessionExpiringIn(const Duration(minutes: 30)));
      final container = containerWith((_) => http.Response('', 204));

      expect(await container.read(authControllerProvider.future), isNotNull);
    });

    test('istekla sesija se briše pri startu', () async {
      storage = FakeSessionStorage(sessionExpiringIn(const Duration(minutes: -1)));
      final container = containerWith((_) => http.Response('', 204));

      expect(await container.read(authControllerProvider.future), isNull);
      expect(storage.session, isNull);
    });

    test('prijava snima sesiju i šalje aplikaciju', () async {
      storage = FakeSessionStorage();
      final container = containerWith((_) => jsonResponse(200, {
            'token': 'novi',
            'expiresAt': '2099-01-01T00:00:00Z',
            'role': 'User',
            'firstName': 'Amra',
            'lastName': 'Hodžić',
          }));
      await container.read(authControllerProvider.future);

      await container
          .read(authControllerProvider.notifier)
          .signIn(login: 'mobile', password: 'test', app: ClientApp.mobile);

      expect(container.read(authControllerProvider).value?.token, 'novi');
      expect(storage.session?.token, 'novi');
      expect(requests.single.body, contains('"app":"Mobile"'));
    });

    test('odjava briše lokalno i kad server ne odgovori', () async {
      storage = FakeSessionStorage(sessionExpiringIn(const Duration(minutes: 30)));
      final container = containerWith((_) => throw http.ClientException('Connection refused'));
      await container.read(authControllerProvider.future);

      await container.read(authControllerProvider.notifier).signOut();

      expect(container.read(authControllerProvider).value, isNull);
      expect(storage.session, isNull);
      expect(requests.single.url.path, '/auth/logout');
    });

    test('odjava s već opozvanim tokenom ne javlja da je prijava istekla', () async {
      storage = FakeSessionStorage(sessionExpiringIn(const Duration(minutes: 30)));
      final container = containerWith((_) => jsonResponse(401, {'message': 'Prijava je istekla.'}));
      await container.read(authControllerProvider.future);

      await container.read(authControllerProvider.notifier).signOut();

      expect(storage.session, isNull);
      expect(container.read(sessionNoticeProvider).consume(), isNull);
    });

    test('401 na zaštićenom pozivu odjavljuje i ostavlja poruku za ekran prijave', () async {
      storage = FakeSessionStorage(sessionExpiringIn(const Duration(minutes: 30)));
      final container = containerWith((_) => jsonResponse(401, {'message': 'Prijava je istekla.'}));
      await container.read(authControllerProvider.future);

      await expectLater(container.read(apiClientProvider).post('/cards'), throwsA(isA<ApiException>()));
      await Future<void>.delayed(Duration.zero);

      expect(container.read(authControllerProvider).value, isNull);
      expect(storage.session, isNull);
      final notice = container.read(sessionNoticeProvider);
      expect(notice.consume(), AuthController.expiredMessage);
      expect(notice.consume(), isNull);
    });
  });

  group('authRedirect', () {
    const publicRoutes = {'/login', '/register'};

    String? redirect(AsyncValue<AuthSession?> auth, String location) => authRedirect(
          auth: auth,
          location: location,
          publicRoutes: publicRoutes,
          splash: '/',
          login: '/login',
          home: '/dashboard',
        );

    final signedIn = AsyncData<AuthSession?>(sessionExpiringIn(const Duration(minutes: 30)));
    const signedOut = AsyncData<AuthSession?>(null);

    test('dok se sesija učitava, ostaje se na splashu', () {
      expect(redirect(const AsyncLoading(), '/dashboard'), '/');
      expect(redirect(const AsyncLoading(), '/'), isNull);
    });

    test('odjavljen ide na prijavu, osim na javnim rutama', () {
      expect(redirect(signedOut, '/dashboard'), '/login');
      expect(redirect(signedOut, '/'), '/login');
      expect(redirect(signedOut, '/register'), isNull);
    });

    test('greška pri čitanju sesije znači odjavljen', () {
      expect(redirect(AsyncError(Exception('keychain'), StackTrace.empty), '/dashboard'), '/login');
    });

    test('prijavljen ne vidi javne rute ni splash', () {
      expect(redirect(signedIn, '/login'), '/dashboard');
      expect(redirect(signedIn, '/'), '/dashboard');
      expect(redirect(signedIn, '/dashboard'), isNull);
    });
  });
}
