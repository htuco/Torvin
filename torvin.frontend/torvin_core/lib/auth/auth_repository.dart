import '../api/api_client.dart';
import 'auth_session.dart';
import 'client_app.dart';

/// Pozivi `/auth/*` ruta. Polja odgovaraju requestima iz `Torvin.Model/Requests`.
class AuthRepository {
  const AuthRepository(this._api);

  final ApiClient _api;

  Future<AuthSession> login({required String login, required String password, required ClientApp app}) async {
    final json = await _api.post(
      '/auth/login',
      body: {'login': login, 'password': password, 'app': app.apiName},
      authorized: false,
    );
    return AuthSession.fromJson(json!);
  }

  Future<void> logout() => _api.post('/auth/logout');

  /// Vraća poruku uspjeha s backenda.
  Future<String> register({
    required String firstName,
    required String lastName,
    required String username,
    required String email,
    required String? phoneNumber,
    required String password,
    required String confirmPassword,
  }) async {
    final json = await _api.post(
      '/auth/register',
      body: {
        'firstName': firstName,
        'lastName': lastName,
        'username': username,
        'email': email,
        'phoneNumber': phoneNumber,
        'password': password,
        'confirmPassword': confirmPassword,
      },
      authorized: false,
    );
    return json!['message'] as String;
  }

  Future<void> verifyEmail({required String email, required String code}) =>
      _api.post('/auth/verify-email', body: {'email': email, 'code': code}, authorized: false);

  Future<void> forgotPassword({required String email}) =>
      _api.post('/auth/forgot-password', body: {'email': email}, authorized: false);

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
    required String confirmPassword,
  }) =>
      _api.post(
        '/auth/reset-password',
        body: {'email': email, 'code': code, 'newPassword': newPassword, 'confirmPassword': confirmPassword},
        authorized: false,
      );
}
