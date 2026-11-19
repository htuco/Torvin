/// Prijavljen korisnik: odgovor `POST /auth/login`, isti oblik kao `LoginResponse` na backendu.
class AuthSession {
  const AuthSession({
    required this.token,
    required this.expiresAt,
    required this.role,
    required this.firstName,
    required this.lastName,
  });

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String).toUtc(),
        role: json['role'] as String,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
      );

  final String token;
  final DateTime expiresAt;
  final String role;
  final String firstName;
  final String lastName;

  bool get isExpired => !DateTime.now().toUtc().isBefore(expiresAt);

  Map<String, dynamic> toJson() => {
        'token': token,
        'expiresAt': expiresAt.toIso8601String(),
        'role': role,
        'firstName': firstName,
        'lastName': lastName,
      };
}
