/// Configuração do cliente Campo.
class AppConfig {
  /// API default Luachimo (SML). Holding: https://api.mufutu.ao/api
  static const defaultApiBase = 'https://sml.api.mufutu.ao/api';
  static const defaultSiteCode = 'LUC';
  static const maxRetries = 5;
  static const syncBatchSize = 10;

  /// Backoff: min(1000 * 2^n, 30000) ms
  static int backoffMs(int retryCount) {
    final ms = 1000 * (1 << retryCount.clamp(0, 10));
    return ms > 30000 ? 30000 : ms;
  }
}
