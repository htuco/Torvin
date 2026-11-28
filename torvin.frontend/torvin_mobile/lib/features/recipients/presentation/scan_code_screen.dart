import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:torvin_core/torvin_core.dart';

/// Kamera koja čita QR kod primaoca i vraća pročitan tekst prethodnom ekranu. Kod ovdje ne provjerava, to radi
/// server (`resolve-code`).
class ScanCodeScreen extends StatefulWidget {
  const ScanCodeScreen({super.key});

  @override
  State<ScanCodeScreen> createState() => _ScanCodeScreenState();
}

class _ScanCodeScreenState extends State<ScanCodeScreen> {
  final _controller = MobileScannerController(formats: const [BarcodeFormat.qrCode]);

  // Kamera javlja isti kod više puta u sekundi, a ekran se zatvara samo jednom
  bool _done = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    final code = capture.barcodes.map((barcode) => barcode.rawValue).nonNulls.firstOrNull;
    if (_done || code == null) {
      return;
    }
    _done = true;
    context.pop(code);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Skeniraj QR kod')),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: MobileScanner(controller: _controller, onDetect: _onDetect, errorBuilder: _error),
          ),
          Padding(
            padding: const EdgeInsets.all(AppConstants.spacingLG),
            child: Text(
              'Usmjerite kameru na QR kod koji vam pokazuje primalac.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ),
        ],
      ),
    );
  }

  Widget _error(BuildContext context, MobileScannerException error) {
    if (error.errorCode == MobileScannerErrorCode.permissionDenied) {
      return EmptyState(
        icon: LucideIcons.cameraOff,
        title: 'Nema pristupa kameri',
        message: 'Dozvolite kameru za Torvin u postavkama telefona, pa pokušajte ponovo. '
            'Primaoca možete naći i pretragom.',
        actionLabel: 'Pokušaj ponovo',
        onAction: _controller.start,
      );
    }

    return ErrorState(
      title: 'Kamera nije dostupna',
      message: 'Kamera se nije pokrenula. Pokušajte ponovo ili nađite primaoca pretragom.',
      onRetry: _controller.start,
    );
  }
}
