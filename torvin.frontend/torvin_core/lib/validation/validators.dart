/// Validatori polja forme. Pravila i poruke su isti kao u `Torvin.Model/Requests`, da klijent i server
/// govore istim riječima. Klijent provjerava samo format, a zauzetost (korisničko ime, email) javlja backend.
abstract final class Validators {
  static const int passwordMinLength = 8;
  static const int passwordMaxLength = 100;

  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final RegExp _phone = RegExp(r'^\+?(?:[0-9] ?){5,14}[0-9]$');
  static final RegExp _username = RegExp(r'^[a-zA-Z0-9._]{3,50}$');
  static final RegExp _code = RegExp(r'^[0-9]{6}$');

  static const String emailMessage = 'Email nije u ispravnom formatu (npr. ime@domena.ba).';
  static const String phoneMessage =
      'Telefon ima 6–15 cifara, s razmacima između i + na početku po želji (npr. +387 61 234 567).';
  static const String usernameMessage = 'Korisničko ime ima 3–50 znakova: slova, cifre, tačka i donja crta.';
  static const String strongPasswordMessage =
      'Lozinka mora imati 8–100 znakova, veliko i malo slovo, broj i specijalni znak.';
  static const String codeMessage = 'Kod ima tačno 6 cifara.';
  static const String passwordsMismatchMessage = 'Lozinke se ne poklapaju.';

  static String? Function(String?) required(String message) =>
      (value) => value == null || value.trim().isEmpty ? message : null;

  static String? Function(String?) maxLength(int max, String message) =>
      (value) => value != null && value.length > max ? message : null;

  static String? email(String? value) => _matchesOrEmpty(_email, value) ? null : emailMessage;

  /// Telefon nije obavezan, pa prazno polje prolazi.
  static String? phone(String? value) => _matchesOrEmpty(_phone, value) ? null : phoneMessage;

  static String? username(String? value) => _matchesOrEmpty(_username, value) ? null : usernameMessage;

  static String? code(String? value) => _matchesOrEmpty(_code, value) ? null : codeMessage;

  static String? strongPassword(String? value) {
    if (value == null || value.isEmpty) {
      return null;
    }

    // Unicode klase kao `char.IsUpper` na backendu, pa i "Č" ili "Š" broje kao veliko slovo
    final valid = value.length >= passwordMinLength &&
        value.length <= passwordMaxLength &&
        value.contains(_upper) &&
        value.contains(_lower) &&
        value.contains(_digit) &&
        value.contains(_special);
    return valid ? null : strongPasswordMessage;
  }

  static final RegExp _upper = RegExp(r'\p{Lu}', unicode: true);
  static final RegExp _lower = RegExp(r'\p{Ll}', unicode: true);
  static final RegExp _digit = RegExp(r'\p{Nd}', unicode: true);
  static final RegExp _special = RegExp(r'[^\p{L}\p{Nd}]', unicode: true);

  /// `other` se čita pri validaciji, pa poređenje vidi trenutnu vrijednost drugog polja.
  static String? Function(String?) matches(String Function() other) =>
      (value) => value == other() ? null : passwordsMismatchMessage;

  /// Vraća prvu grešku, redom kojim su validatori navedeni.
  static String? Function(String?) combine(List<String? Function(String?)> validators) => (value) {
        for (final validator in validators) {
          final error = validator(value);
          if (error != null) {
            return error;
          }
        }
        return null;
      };

  // Prazno polje je posao validatora `required`, da korisnik ne dobije dvije poruke odjednom
  static bool _matchesOrEmpty(RegExp pattern, String? value) =>
      value == null || value.isEmpty || pattern.hasMatch(value);
}
