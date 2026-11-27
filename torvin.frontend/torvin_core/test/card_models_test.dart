import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

Map<String, dynamic> cardJson({
  String network = 'Visa',
  String? name = 'Glavna',
  String status = 'Active',
  bool blockedByAdmin = false,
}) =>
    {
      'id': 7,
      'userId': 3,
      'userFirstName': 'Amra',
      'userLastName': 'Hodžić',
      'name': name,
      'cardTypeId': 2,
      'cardTypeName': 'Prepaid',
      'cardStatusId': 2,
      'cardStatusCode': status,
      'cardStatusName': 'Aktivna',
      'currencyId': 1,
      'currencyCode': 'BAM',
      'network': network,
      'last4Digits': '4821',
      'expiresAt': '2029-08-31',
      'balance': 1245.0000,
      'isDefault': true,
      'createdAt': '2026-11-20T09:15:00Z',
      'isBlockedByAdmin': blockedByAdmin,
    };

void main() {
  test('kartica iz JSON-a: mreža, istek bez vremena, balans s 4 decimale i nazivi', () {
    final card = CardResponse.fromJson(cardJson());

    expect(card.network, CardNetwork.visa);
    expect(card.expiresAt, DateTime(2029, 8, 31));
    expect(DateFormatter.cardExpiry(card.expiresAt), '08/29');
    expect(card.balance, 1245);
    expect(card.holderName, 'Amra Hodžić');
    expect(card.displayName, 'Glavna');
    expect(card.maskedNumber, '•••• 4821');
    expect(card.createdAt.isUtc, isTrue);
    expect(card.isBlockedByAdmin, isFalse);
    expect(CardResponse.fromJson(cardJson(status: 'Blocked', blockedByAdmin: true)).isBlockedByAdmin, isTrue);
  });

  test('kartica bez naziva se zove po tipu, a nepoznata mreža je greška formata', () {
    expect(CardResponse.fromJson(cardJson(name: null)).displayName, 'Prepaid');
    expect(CardResponse.fromJson(cardJson(network: 'Mastercard')).network, CardNetwork.mastercard);
    expect(() => CardResponse.fromJson(cardJson(network: 'Amex')), throwsFormatException);
  });

  test('transakcija: poslano je odliv, primljeno priliv', () {
    Map<String, dynamic> json(String direction, String status) => {
          'id': 1,
          'direction': direction,
          'amount': 120,
          'currencyCode': 'BAM',
          'transactionStatusCode': status,
          'transactionStatusName': status == 'Completed' ? 'Izvršena' : 'Poništena',
          'counterpartyFirstName': 'Marko',
          'counterpartyLastName': 'Petrović',
          'createdAt': '2026-11-20T09:15:00Z',
        };

    final sent = TransactionResponse.fromJson(json('Sent', 'Completed'));
    final received = TransactionResponse.fromJson(json('Received', 'Reversed'));

    expect(sent.signedAmount, -120);
    expect(sent.isCompleted, isTrue);
    expect(received.signedAmount, 120);
    expect(received.isCompleted, isFalse);
    expect(received.counterpartyName, 'Marko Petrović');
  });

  test('stranica zna ima li još redova', () {
    PagedResult<int> page(int page, int total) =>
        PagedResult.fromJson({
          'items': [
            {'id': 1},
          ],
          'totalCount': total,
          'page': page,
          'pageSize': 20,
        }, (json) => json['id'] as int);

    expect(page(1, 21).items, [1]);
    expect(page(1, 21).hasMore, isTrue);
    expect(page(2, 21).hasMore, isFalse);
    expect(page(1, 20).hasMore, isFalse);
  });

  test('izgled kartice: boja po tipu u krugu, ton i katanac po statusu', () {
    const colors = TorvinColors.light;

    expect(CardAppearance.color(colors, 1), colors.cardPrimary);
    expect(CardAppearance.color(colors, 2), colors.cardDark);
    expect(CardAppearance.color(colors, 3), colors.cardSlate);
    expect(CardAppearance.color(colors, 4), colors.cardPrimary);

    expect(CardAppearance.tone(CardStatusCodes.active), StatusTone.success);
    expect(CardAppearance.tone(CardStatusCodes.pending), StatusTone.warning);
    expect(CardAppearance.tone(CardStatusCodes.stolen), StatusTone.error);
    expect(CardAppearance.tone(CardStatusCodes.closed), StatusTone.neutral);

    expect(CardAppearance.isLocked(CardStatusCodes.active), isFalse);
    expect(CardAppearance.isLocked(CardStatusCodes.pending), isFalse);
    expect(CardAppearance.isLocked(CardStatusCodes.blocked), isTrue);
    expect(CardAppearance.isLocked(CardStatusCodes.lost), isTrue);
  });
}
