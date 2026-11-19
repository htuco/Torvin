import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  group('email', () {
    test('ispravan i prazan prolaze', () {
      expect(Validators.email('ime@domena.ba'), isNull);
      expect(Validators.email(''), isNull);
    });

    test('bez @ ili domene pada s porukom o formatu', () {
      expect(Validators.email('imedomena.ba'), Validators.emailMessage);
      expect(Validators.email('ime@domena'), Validators.emailMessage);
      expect(Validators.email('ime @domena.ba'), Validators.emailMessage);
    });
  });

  group('telefon', () {
    test('prazan nije obavezan', () => expect(Validators.phone(''), isNull));

    test('cifre s razmacima i + prolaze', () {
      expect(Validators.phone('+387 61 234 567'), isNull);
      expect(Validators.phone('061234567'), isNull);
      expect(Validators.phone('123456'), isNull);
      expect(Validators.phone('123456789012345'), isNull);
    });

    test('previše kratak, predug, slova ili dupli razmak padaju', () {
      expect(Validators.phone('12345'), Validators.phoneMessage);
      expect(Validators.phone('1234567890123456'), Validators.phoneMessage);
      expect(Validators.phone('061-234-567'), Validators.phoneMessage);
      expect(Validators.phone('061  234567'), Validators.phoneMessage);
    });
  });

  group('korisničko ime', () {
    test('3–50 znakova, slova, cifre, tačka i donja crta', () {
      expect(Validators.username('amra.h_1'), isNull);
      expect(Validators.username('ab'), Validators.usernameMessage);
      expect(Validators.username('a' * 51), Validators.usernameMessage);
      expect(Validators.username('amra h'), Validators.usernameMessage);
    });
  });

  group('jaka lozinka', () {
    test('sve klase znakova i granice dužine', () {
      expect(Validators.strongPassword('Lozinka1!'), isNull);
      expect(Validators.strongPassword('Lozin1!'), Validators.strongPasswordMessage);
      expect(Validators.strongPassword('Ab1!${'x' * 96}'), isNull);
      expect(Validators.strongPassword('Ab1!${'x' * 97}'), Validators.strongPasswordMessage);
    });

    test('nedostaje jedna klasa znakova', () {
      expect(Validators.strongPassword('lozinka1!'), Validators.strongPasswordMessage);
      expect(Validators.strongPassword('LOZINKA1!'), Validators.strongPasswordMessage);
      expect(Validators.strongPassword('Lozinka!!'), Validators.strongPasswordMessage);
      expect(Validators.strongPassword('Lozinka12'), Validators.strongPasswordMessage);
    });

    // Backend broji "Č" kao veliko slovo (char.IsUpper), pa klijent ne smije javiti lažnu grešku
    test('naša slova se broje kao velika i mala', () {
      expect(Validators.strongPassword('Čćžšđ12!'), isNull);
    });
  });

  test('kod ima tačno 6 cifara', () {
    expect(Validators.code('123456'), isNull);
    expect(Validators.code('12345'), Validators.codeMessage);
    expect(Validators.code('1234567'), Validators.codeMessage);
    expect(Validators.code('12345a'), Validators.codeMessage);
  });

  test('potvrda lozinke poredi trenutnu vrijednost drugog polja', () {
    var password = 'Lozinka1!';
    final validator = Validators.matches(() => password);

    expect(validator('Lozinka1!'), isNull);
    password = 'Druga1!';
    expect(validator('Lozinka1!'), Validators.passwordsMismatchMessage);
  });

  test('combine vraća prvu grešku, a prazno ne javlja format', () {
    final validator = Validators.combine([Validators.required('Email je obavezan.'), Validators.email]);

    expect(validator(''), 'Email je obavezan.');
    expect(validator('   '), 'Email je obavezan.');
    expect(validator('pogresno'), Validators.emailMessage);
    expect(validator('ime@domena.ba'), isNull);
  });

  test('maxLength', () {
    final validator = Validators.maxLength(3, 'Najviše 3 znaka.');
    expect(validator('abc'), isNull);
    expect(validator('abcd'), 'Najviše 3 znaka.');
  });
}
