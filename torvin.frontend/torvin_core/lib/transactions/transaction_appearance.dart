import '../widgets/status_chip.dart';
import 'transaction_status_codes.dart';

/// Kako se status transakcije prikazuje. Naziv statusa stiže s API-ja, a ovdje je samo ton.
abstract final class TransactionAppearance {
  /// Izvršena je uspjeh, neuspjela greška, a poništena i nepoznat status su neutralni.
  static StatusTone tone(String statusCode) => switch (statusCode) {
        TransactionStatusCodes.completed => StatusTone.success,
        TransactionStatusCodes.failed => StatusTone.error,
        TransactionStatusCodes.reversed => StatusTone.neutral,
        _ => StatusTone.neutral,
      };
}
