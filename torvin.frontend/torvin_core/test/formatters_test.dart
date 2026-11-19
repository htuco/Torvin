import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  const minus = MoneyFormatter.minus;

  group('MoneyFormatter', () {
    test('tačka za hiljade, zarez za decimale i valuta iza iznosa', () {
      expect(MoneyFormatter.format(1245), '1.245,00 BAM');
      expect(MoneyFormatter.format(1234567.891), '1.234.567,89 BAM');
      expect(MoneyFormatter.format(0), '0,00 BAM');
      expect(MoneyFormatter.format(12.5, currency: 'EUR'), '12,50 EUR');
    });

    test('negativan iznos ima pravi minus, a uz predznak pozitivan ima plus', () {
      expect(MoneyFormatter.format(-120), '${minus}120,00 BAM');
      expect(MoneyFormatter.format(-1245.5, signed: true), '${minus}1.245,50 BAM');
      expect(MoneyFormatter.format(500, signed: true), '+500,00 BAM');
      expect(MoneyFormatter.format(0, signed: true), '0,00 BAM');
    });

    test('zaokružuje na pola dalje od nule, i kad double nije tačan', () {
      expect(MoneyFormatter.format(0.005), '0,01 BAM');
      expect(MoneyFormatter.format(-0.005), '${minus}0,01 BAM');
      expect(MoneyFormatter.format(2.675), '2,68 BAM');
      expect(MoneyFormatter.format(1.0049), '1,00 BAM');
    });

    test('iznos koji se zaokruži na nulu nema minus', () {
      expect(MoneyFormatter.format(-0.001), '0,00 BAM');
      expect(MoneyFormatter.format(-0.004, signed: true), '0,00 BAM');
    });
  });

  group('DateFormatter', () {
    final local = DateTime(2026, 5, 5, 14, 22);

    test('datum, vrijeme i oboje kao u redu transakcije', () {
      expect(DateFormatter.date(local), '05.05.2026');
      expect(DateFormatter.time(local), '14:22');
      expect(DateFormatter.dateTime(local), '05.05.2026 · 14:22');
    });

    test('UTC vrijeme s API-ja prikazuje se u lokalnoj zoni', () {
      final utc = local.toUtc();
      expect(DateFormatter.time(utc), '14:22');
      expect(DateFormatter.date(utc), '05.05.2026');
    });

    test('rok važenja kartice je mjesec/godina', () {
      expect(DateFormatter.cardExpiry(DateTime(2028, 5, 31)), '05/28');
    });
  });
}
