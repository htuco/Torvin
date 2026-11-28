import '../formatting/money_formatter.dart';

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

  static final RegExp _cardNumber = RegExp(r'^[0-9]{16}$');

  static const String cardNumberMessage = 'Broj kartice nije ispravan. Unesite 16 cifara bez razmaka.';
  static const String cardNetworkMessage = 'Podržane su samo Visa i Mastercard kartice.';

  /// Isto kao `CardNumberAttribute` na backendu: 16 cifara, kontrolna cifra (Luhn) i mreža Visa ili Mastercard.
  static String? cardNumber(String? value) {
    if (value == null || value.isEmpty) {
      return null;
    }
    if (!_cardNumber.hasMatch(value) || !_isValidLuhn(value)) {
      return cardNumberMessage;
    }
    return _isSupportedNetwork(value) ? null : cardNetworkMessage;
  }

  // Luhn: svaka druga cifra zdesna se udvostruči (od dvocifrenog rezultata se oduzme 9), a zbir mora biti djeljiv s 10
  static bool _isValidLuhn(String number) {
    var sum = 0;
    for (var i = 0; i < number.length; i++) {
      var digit = int.parse(number[number.length - 1 - i]);
      if (i.isOdd) {
        digit *= 2;
        if (digit > 9) {
          digit -= 9;
        }
      }
      sum += digit;
    }
    return sum % 10 == 0;
  }

  // Visa počinje s 4, a Mastercard s 51–55 ili 2221–2720
  static bool _isSupportedNetwork(String number) {
    final firstTwo = int.parse(number.substring(0, 2));
    final firstFour = int.parse(number.substring(0, 4));
    return number.startsWith('4') || (firstTwo >= 51 && firstTwo <= 55) || (firstFour >= 2221 && firstFour <= 2720);
  }

  /// Granice iznosa u feninzima, iste kao `Money.MinAmount` i `Money.MaxAmount` na backendu.
  static const int amountMinCents = 1;
  static const int amountMaxCents = 10000000;

  static const String amountMessage = 'Iznos mora biti između 0,01 i 100.000,00, s najviše dvije decimale.';
  static const String amountFormatMessage = 'Iznos upišite u obliku 1.245,00: tačka za hiljade, zarez za decimale.';

  // Broj u našem obliku, i s minusom ili više decimala: takav je iznos van pravila, a ne pogrešno upisan
  static final RegExp _amountShape = RegExp(r'^-?([1-9]\d{0,2}(\.\d{3})+|\d+)(,\d+)?$');

  /// Isto pravilo kao `Money.IsValidAmount`: od 0,01 do 100.000,00, najviše dvije decimale, bez zaokruživanja.
  static String? amount(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) {
      return null;
    }
    if (!_amountShape.hasMatch(text)) {
      return amountFormatMessage;
    }

    final cents = MoneyFormatter.parseCents(text);
    return cents == null || cents < amountMinCents || cents > amountMaxCents ? amountMessage : null;
  }

  /// Iznos veći od `maxCents` (npr. balans kartice) dobija `message`. Prazan ili neispravan iznos je posao `amount`.
  static String? Function(String?) amountAtMost(int maxCents, String message) => (value) {
        final cents = MoneyFormatter.parseCents(value ?? '');
        return cents != null && cents > maxCents ? message : null;
      };

  /// Napomena uz transakciju ili prijavu kartice, isto ograničenje kao u requestima na backendu.
  static const int noteMaxLength = 200;
  static const String noteMessage = 'Napomena može imati najviše $noteMaxLength znakova.';

  static String? note(String? value) => maxLength(noteMaxLength, noteMessage)(value);

  /// Granica "do" ne smije biti ispod granice "od". `minCents` se čita pri validaciji, pa vidi trenutnu vrijednost
  /// polja "od", a prazan ili neispravan iznos je posao `amount`.
  static String? Function(String?) amountAtLeast(int? Function() minCents, String message) => (value) {
        final min = minCents();
        final cents = MoneyFormatter.parseCents(value ?? '');
        return min != null && cents != null && cents < min ? message : null;
      };

  /// Isto kao `TransactionService.AmountRangeInvalid` na backendu.
  static const String amountRangeMessage = 'Iznos do ne može biti manji od iznosa od.';

  /// Nadimak omiljenog primaoca, isto kao `FavoriteRecipientInsertRequest.NicknameMaxLength` na backendu.
  static const int nicknameMaxLength = 50;
  static const String nicknameMessage = 'Nadimak može imati najviše $nicknameMaxLength znakova.';

  static String? nickname(String? value) => maxLength(nicknameMaxLength, nicknameMessage)(value);

  /// Isto kao `TransactionService.InsufficientFunds` na backendu.
  static String insufficientFundsMessage(String last4) => 'Nedovoljno sredstava na kartici •••• $last4.';

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
