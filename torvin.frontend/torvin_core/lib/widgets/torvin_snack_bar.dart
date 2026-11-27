import 'package:flutter/material.dart';

/// Kratka poruka poslije akcije ("Kartica •••• 4821 je blokirana.") ili poruka greške s backenda.
/// Nova poruka odmah zamjenjuje prethodnu, da se poruke ne nižu jedna za drugom.
void showTorvinSnackBar(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}
