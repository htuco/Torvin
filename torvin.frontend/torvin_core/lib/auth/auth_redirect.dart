import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_session.dart';

/// Pravila preusmjeravanja koja dijele oba routera. Vraća rutu na koju treba ići, ili `null` za ostanak.
String? authRedirect({
  required AsyncValue<AuthSession?> auth,
  required String location,
  required Set<String> publicRoutes,
  required String splash,
  required String login,
  required String home,
}) {
  // Dok se sesija čita iz skladišta, ne prikazuje se ni prijava ni zaštićeni ekran
  if (auth.isLoading && !auth.hasValue) {
    return location == splash ? null : splash;
  }

  if (auth.value == null) {
    return publicRoutes.contains(location) ? null : login;
  }

  if (location == splash || publicRoutes.contains(location)) {
    return home;
  }

  return null;
}
