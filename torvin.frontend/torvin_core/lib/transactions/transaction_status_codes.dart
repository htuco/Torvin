/// Stalni kodovi statusa transakcije, isti kao `TransactionStatusCodes` na backendu. Naziv za prikaz stiže s API-ja.
abstract final class TransactionStatusCodes {
  static const String completed = 'Completed';
  static const String failed = 'Failed';
  static const String reversed = 'Reversed';
}
