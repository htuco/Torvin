import 'package:flutter_test/flutter_test.dart';
import 'package:torvin_core/torvin_core.dart';

void main() {
  test('validate baca StateError kad API_BASE_URL nije zadan', () {
    expect(AppConfig.validate, throwsStateError);
  });
}
