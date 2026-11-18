import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:http/http.dart' as http;

import '../api/api_client.dart';
import 'auth_controller.dart';
import 'auth_repository.dart';
import 'auth_session.dart';
import 'session_storage.dart';

final sessionStorageProvider = Provider<SessionStorage>((ref) => SessionStorage());

final httpClientProvider = Provider<http.Client>((ref) {
  final client = http.Client();
  ref.onDispose(client.close);
  return client;
});

final apiClientProvider = Provider<ApiClient>((ref) {
  final storage = ref.watch(sessionStorageProvider);
  return ApiClient(
    httpClient: ref.watch(httpClientProvider),
    readToken: () async => (await storage.read())?.token,
    onUnauthorized: () => ref.read(authControllerProvider.notifier).expire(),
  );
});

final authRepositoryProvider = Provider<AuthRepository>((ref) => AuthRepository(ref.watch(apiClientProvider)));

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthSession?>(AuthController.new);

final sessionNoticeProvider = Provider<SessionNotice>((ref) => SessionNotice());

/// Poruka za ekran prijave kad korisnik tamo dođe sam od sebe (npr. istekla prijava).
/// Nije reaktivna: ekran je pročita jednom pri otvaranju i time je potroši.
class SessionNotice {
  String? _message;

  void show(String message) => _message = message;

  String? consume() {
    final message = _message;
    _message = null;
    return message;
  }
}
