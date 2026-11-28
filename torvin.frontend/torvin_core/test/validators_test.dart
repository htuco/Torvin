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

  group('broj kartice', () {
    test('Visa i Mastercard (obje serije) s ispravnom kontrolnom cifrom prolaze, a prazno je posao required', () {
      expect(Validators.cardNumber('4111111111111111'), isNull);
      expect(Validators.cardNumber('5555555555554444'), isNull);
      expect(Validators.cardNumber('2223003122003222'), isNull);
      expect(Validators.cardNumber(''), isNull);
    });

    test('pogrešna dužina, slova ili kontrolna cifra daju poruku o broju', () {
      expect(Validators.cardNumber('411111111111111'), Validators.cardNumberMessage);
      expect(Validators.cardNumber('41111111111111111'), Validators.cardNumberMessage);
      expect(Validators.cardNumber('4111 1111 1111 1111'), Validators.cardNumberMessage);
      expect(Validators.cardNumber('411111111111111a'), Validators.cardNumberMessage);
      expect(Validators.cardNumber('4111111111111112'), Validators.cardNumberMessage);
    });

    test('ispravan broj druge mreže daje poruku o mreži', () {
      expect(Validators.cardNumber('6011111111111117'), Validators.cardNetworkMessage);
    });
  });

  group('napomena', () {
    test('do 200 znakova prolazi, a duža dobija poruku s ograničenjem', () {
      expect(Validators.note(''), isNull);
      expect(Validators.note('a' * 200), isNull);
      expect(Validators.note('a' * 201), 'Napomena može imati najviše 200 znakova.');
    });
  });

  group('iznos', () {
    test('ispravan oblik i granice prolaze, prazno je posao required', () {
      expect(Validators.amount('0,01'), isNull);
      expect(Validators.amount('1.245,00'), isNull);
      expect(Validators.amount('1245'), isNull);
      expect(Validators.amount('100.000,00'), isNull);
      expect(Validators.amount(''), isNull);
    });

    test('nula, negativan, treća decimala i preko granice su van pravila', () {
      expect(Validators.amount('0'), Validators.amountMessage);
      expect(Validators.amount('0,00'), Validators.amountMessage);
      expect(Validators.amount('-5'), Validators.amountMessage);
      expect(Validators.amount('1,234'), Validators.amountMessage);
      expect(Validators.amount('100.000,01'), Validators.amountMessage);
    });

    test('tačka kao decimalni znak, nula ispred hiljada i slova su pogrešan oblik', () {
      expect(Validators.amount('12.50'), Validators.amountFormatMessage);
      expect(Validators.amount('0.500'), Validators.amountFormatMessage);
      expect(Validators.amount('1,2,3'), Validators.amountFormatMessage);
      expect(Validators.amount('abc'), Validators.amountFormatMessage);
    });

    test('najviše do granice, a neispravan iznos ostavlja validatoru iznosa', () {
      final atMost = Validators.amountAtMost(124500, Validators.insufficientFundsMessage('4821'));
      expect(atMost('1.245,00'), isNull);
      expect(atMost('1.245,01'), 'Nedovoljno sredstava na kartici •••• 4821.');
      expect(atMost('abc'), isNull);
      expect(atMost(''), isNull);
    });
  });
}
