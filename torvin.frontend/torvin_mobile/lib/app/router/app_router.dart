import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:torvin_core/torvin_core.dart';

import '../../features/auth/presentation/forgot_password_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/reset_password_screen.dart';
import '../../features/auth/presentation/verify_email_screen.dart';
import '../../features/cards/presentation/activate_card_screen.dart';
import '../../features/cards/presentation/add_card_screen.dart';
import '../../features/cards/presentation/card_details_screen.dart';
import '../../features/cards/presentation/cards_screen.dart';
import '../../features/cards/presentation/receive_code_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/recipients/data/recipient_models.dart';
import '../../features/recipients/presentation/favorites_screen.dart';
import '../../features/recipients/presentation/scan_code_screen.dart';
import '../../features/transactions/presentation/new_transaction_screen.dart';
import '../../features/transactions/presentation/transaction_details_screen.dart';
import '../../features/transactions/presentation/transaction_sent_screen.dart';
import '../../features/transactions/presentation/transactions_screen.dart';
import 'app_routes.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    initialLocation: AppRoutes.root,
    redirect: (_, state) => authRedirect(
      auth: ref.read(authControllerProvider),
      location: state.matchedLocation,
      publicRoutes: AppRoutes.publicRoutes,
      splash: AppRoutes.root,
      login: AppRoutes.login,
      home: AppRoutes.dashboard,
    ),
    routes: [
      GoRoute(path: AppRoutes.root, builder: (_, _) => const SplashPage()),
      GoRoute(path: AppRoutes.login, builder: (_, state) => LoginScreen(notice: state.extra as String?)),
      GoRoute(path: AppRoutes.register, builder: (_, _) => const RegisterScreen()),
      GoRoute(
        path: AppRoutes.verifyEmail,
        builder: (_, state) => VerifyEmailScreen(step: state.extra as EmailStep? ?? const EmailStep()),
      ),
      GoRoute(path: AppRoutes.forgotPassword, builder: (_, _) => const ForgotPasswordScreen()),
      GoRoute(
        path: AppRoutes.resetPassword,
        builder: (_, state) => ResetPasswordScreen(step: state.extra as EmailStep? ?? const EmailStep()),
      ),
      GoRoute(path: AppRoutes.dashboard, builder: (_, _) => const DashboardScreen()),
      GoRoute(
        path: AppRoutes.cards,
        builder: (_, _) => const CardsScreen(),
        routes: [
          // "new" je prije ":id", da se ne pročita kao ID kartice
          GoRoute(path: 'new', builder: (_, _) => const AddCardScreen()),
          GoRoute(
            path: ':id',
            builder: (context, state) => _withId(context, state, (id) => CardDetailsScreen(cardId: id)),
            routes: [
              GoRoute(
                path: 'activate',
                builder: (context, state) => _withId(context, state, (id) => ActivateCardScreen(cardId: id)),
              ),
              GoRoute(
                path: 'receive',
                builder: (context, state) => _withId(context, state, (id) => ReceiveCodeScreen(cardId: id)),
              ),
            ],
          ),
        ],
      ),
      GoRoute(path: AppRoutes.transactions, builder: (_, _) => const TransactionsScreen()),
      GoRoute(
        path: AppRoutes.newTransaction,
        builder: (_, state) => NewTransactionScreen(recipient: state.extra as Recipient?),
      ),
      // Potvrda dobija poslanu transakciju od forme. Bez nje (npr. otvorena adresa) nema šta prikazati
      GoRoute(
        path: AppRoutes.transactionSent,
        builder: (context, state) => switch (state.extra) {
          final TransactionResponse transaction => TransactionSentScreen(transaction: transaction),
          _ => NotFoundPage(onBack: () => context.go(AppRoutes.root)),
        },
      ),
      // Poslije "new" i "sent", da se ne pročitaju kao ID transakcije
      GoRoute(
        path: '${AppRoutes.transactions}/:id',
        builder: (context, state) => _withId(context, state, (id) => TransactionDetailsScreen(transactionId: id)),
      ),
      GoRoute(path: AppRoutes.scanCode, builder: (_, _) => const ScanCodeScreen()),
      GoRoute(path: AppRoutes.favorites, builder: (_, _) => const FavoritesScreen()),
    ],
    errorBuilder: (context, _) => NotFoundPage(onBack: () => context.go(AppRoutes.root)),
  );

  // Prijava, odjava i istekla sesija mijenjaju auth stanje, pa router ponovo provjeri redirect
  ref.listen(authControllerProvider, (_, _) => router.refresh());
  ref.onDispose(router.dispose);
  return router;
});

/// Ekran zapisa iz rute s `:id` (kartica, transakcija). ID koji nije broj je nepostojeća stranica.
Widget _withId(BuildContext context, GoRouterState state, Widget Function(int id) screen) {
  final id = int.tryParse(state.pathParameters['id'] ?? '');
  return id == null ? NotFoundPage(onBack: () => context.go(AppRoutes.root)) : screen(id);
}
