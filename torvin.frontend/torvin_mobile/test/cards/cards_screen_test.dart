import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:torvin_core/torvin_core.dart';
import 'package:torvin_mobile/app/router/app_router.dart';

import '../support/fake_api.dart';

void main() {
  FakeApi api(http.Response Function(http.Request request) cards) => FakeApi({
        'GET /cards': cards,
        'GET /card-statuses': (_) => json(200, page(cardStatusesJson)),
      });

  testWidgets('bez kartica prikazuje prazno stanje s dodavanjem', (tester) async {
    await pumpAt(tester, api((_) => json(200, page([]))), '/cards');

    expect(find.text('Nemate nijednu karticu'), findsOneWidget);
    expect(find.text('Dodaj virtuelnu karticu'), findsOneWidget);
  });

  testWidgets('kartice su na listi s brojem i statusom, a filter šalje ID statusa', (tester) async {
    final fake = api((request) => json(200, page([cardJson(), cardJson(id: 8, last4: '1290', name: null)])));
    await pumpAt(tester, fake, '/cards');

    expect(find.text('2 kartice'), findsOneWidget);
    expect(find.text('Glavna · Debitna'), findsOneWidget);

    await tester.tap(find.widgetWithText(ChoiceChip, 'Blokirana'));
    await tester.pumpAndSettle();
    final filtered = fake.sent('GET', '/cards').where((r) => r.url.queryParameters['page'] == '1').last;
    expect(filtered.url.queryParameters['cardStatusId'], '3');
  });

  testWidgets('filter čip je visok 36', (tester) async {
    await pumpAt(tester, api((_) => json(200, page([]))), '/cards');

    expect(tester.getSize(find.widgetWithText(ChoiceChip, 'Sve')).height, AppConstants.filterChipHeight);
  });

  testWidgets('greška servera pokazuje poruku backenda, a "Pokušaj ponovo" traži listu opet', (tester) async {
    var fail = true;
    final fake = api((request) {
      if (request.url.queryParameters['page'] != '1') {
        return json(200, page([]));
      }
      return fail ? json(503, {'message': 'Server trenutno ne odgovara.'}) : json(200, page([cardJson()]));
    });
    await pumpAt(tester, fake, '/cards');

    expect(find.text('Kartice se nisu učitale'), findsOneWidget);
    expect(find.text('Server trenutno ne odgovara.'), findsOneWidget);

    fail = false;
    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pumpAndSettle();
    expect(find.text('1 kartica'), findsOneWidget);
  });

  testWidgets('poslije odjave i prijave drugog korisnika nema kartica prethodnog', (tester) async {
    var cardName = 'Glavna';
    final fake = FakeApi({
      // Lista ima parametar page, a brojanje otvorenih kartica nema
      'GET /cards': (request) => json(200, page([
            if (request.url.queryParameters['page'] == '1') cardJson(name: cardName),
          ])),
      'GET /card-statuses': (_) => json(200, page(cardStatusesJson)),
      'POST /auth/logout': (_) => http.Response('', 204),
      'POST /auth/login': (_) => json(200, {
            'token': 'token-456',
            'expiresAt': DateTime.now().toUtc().add(const Duration(hours: 1)).toIso8601String(),
            'role': 'User',
            'firstName': 'Tarik',
            'lastName': 'Hadžić',
          }),
    });
    final container = await pumpAt(tester, fake, '/cards');
    expect(find.text('Glavna · Debitna'), findsOneWidget);

    await container.read(authControllerProvider.notifier).signOut();
    await tester.pumpAndSettle();
    cardName = 'Posao';
    await container
        .read(authControllerProvider.notifier)
        .signIn(login: 'tarik', password: 'Lozinka1!', app: ClientApp.mobile);
    await tester.pumpAndSettle();
    container.read(routerProvider).go('/cards');
    await tester.pumpAndSettle();

    expect(find.text('Glavna · Debitna'), findsNothing);
    expect(find.text('Posao · Debitna'), findsOneWidget);
  });

  testWidgets('pet otvorenih kartica onemogućava "Nova" uz razlog', (tester) async {
    final fake = api((request) {
      final status = request.url.queryParameters['cardStatusId'];
      // Broj po statusu: 3 aktivne i 2 blokirane
      final total = switch (status) { '2' => 3, '3' => 2, _ => 0 };
      return json(200, {...page([]), 'totalCount': total});
    });
    await pumpAt(tester, fake, '/cards');

    final button = tester.widget<TextButton>(find.ancestor(of: find.text('Nova'), matching: find.byType(TextButton)));
    expect(button.onPressed, isNull);
    expect(find.text('Možete imati najviše 5 kartica koje nisu zatvorene ni prijavljene.'), findsOneWidget);
  });
}
