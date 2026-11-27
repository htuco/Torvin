import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';
import 'package:torvin_mobile/features/cards/presentation/card_actions.dart';

void main() {
  Set<CardAction> enabled(String status, {bool blockedByAdmin = false}) => {
        for (final entry in cardActionsFor(status, blockedByAdmin: blockedByAdmin).entries)
          if (entry.value.enabled) entry.key,
      };

  Set<String> reasons(String status, {bool blockedByAdmin = false}) =>
      {for (final state in cardActionsFor(status, blockedByAdmin: blockedByAdmin).values) ?state.reason};

  test('kartica na čekanju se samo aktivira, a ostalo čeka kod', () {
    expect(enabled(CardStatusCodes.pending), {CardAction.activate});
    expect(reasons(CardStatusCodes.pending), {pendingReason});
  });

  test('aktivna se blokira, prijavljuje i prima novac', () {
    expect(enabled(CardStatusCodes.active), {CardAction.block, CardAction.report, CardAction.receive});
    expect(reasons(CardStatusCodes.active), isEmpty);
  });

  test('blokirana se deblokira i prijavljuje, ali ne prima novac', () {
    expect(enabled(CardStatusCodes.blocked), {CardAction.unblock, CardAction.report});
    expect(reasons(CardStatusCodes.blocked), {receiveNeedsActiveReason});
  });

  test('blokadu administratora vlasnik ne poništava, ali karticu i dalje može prijaviti', () {
    expect(enabled(CardStatusCodes.blocked, blockedByAdmin: true), {CardAction.report});
    expect(reasons(CardStatusCodes.blocked, blockedByAdmin: true), {blockedByAdminReason, receiveNeedsActiveReason});
  });

  test('izgubljena, ukradena i zatvorena nemaju nijednu akciju, uz razlog iz state machine-a', () {
    expect(enabled(CardStatusCodes.lost), isEmpty);
    expect(reasons(CardStatusCodes.lost), {lostReason});
    expect(enabled(CardStatusCodes.stolen), isEmpty);
    expect(reasons(CardStatusCodes.stolen), {stolenReason});
    expect(enabled(CardStatusCodes.closed), isEmpty);
    expect(reasons(CardStatusCodes.closed), {closedReason});
  });
}
