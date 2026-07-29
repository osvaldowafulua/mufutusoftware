import 'package:flutter_test/flutter_test.dart';
import 'package:mufutu_campo/core/config.dart';

void main() {
  group('AppConfig.backoffMs', () {
    test('exponential with cap 30s', () {
      expect(AppConfig.backoffMs(0), 1000);
      expect(AppConfig.backoffMs(1), 2000);
      expect(AppConfig.backoffMs(2), 4000);
      expect(AppConfig.backoffMs(10), 30000);
    });
  });
}
