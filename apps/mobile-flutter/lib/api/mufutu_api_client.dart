import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../core/config.dart';

class AuthSession {
  AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
    this.mustChangePin = false,
  });

  final String accessToken;
  final String refreshToken;
  final Map<String, dynamic> user;
  final bool mustChangePin;
}

class MufutuApiClient {
  MufutuApiClient({String? apiBase}) : apiBase = apiBase ?? AppConfig.defaultApiBase;

  String apiBase;
  String siteCode = AppConfig.defaultSiteCode;
  final _storage = const FlutterSecureStorage();

  Future<String?> get accessToken => _storage.read(key: 'access_token');

  Future<void> _persist(AuthSession s) async {
    await _storage.write(key: 'access_token', value: s.accessToken);
    await _storage.write(key: 'refresh_token', value: s.refreshToken);
    await _storage.write(key: 'user_json', value: jsonEncode(s.user));
  }

  Future<void> clearSession() async {
    await _storage.deleteAll();
  }

  Future<AuthSession> loginEmail(String email, String password) async {
    final res = await http.post(
      Uri.parse('$apiBase/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    return _parseAuth(res);
  }

  Future<AuthSession> loginPin(String identifier, String pin) async {
    final res = await http.post(
      Uri.parse('$apiBase/auth/login-pin'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'identifier': identifier, 'pin': pin}),
    );
    return _parseAuth(res);
  }

  Future<void> changePin(String currentPin, String newPin) async {
    final token = await accessToken;
    final res = await http.post(
      Uri.parse('$apiBase/auth/change-pin'),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
      body: jsonEncode({'currentPin': currentPin, 'newPin': newPin}),
    );
    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw Exception(_err(res));
    }
  }

  AuthSession _parseAuth(http.Response res) {
    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw Exception(_err(res));
    }
    final data = jsonDecode(res.body) as Map<String, dynamic>;
    final user = (data['user'] as Map<String, dynamic>?) ?? {};
    final session = AuthSession(
      accessToken: data['accessToken'] as String? ?? '',
      refreshToken: data['refreshToken'] as String? ?? '',
      user: user,
      mustChangePin: user['mustChangePin'] == true,
    );
    // fire-and-forget persist
    _persist(session);
    return session;
  }

  String _err(http.Response res) {
    try {
      final m = jsonDecode(res.body);
      if (m is Map && m['message'] != null) {
        final msg = m['message'];
        if (msg is List) return msg.join(', ');
        return msg.toString();
      }
    } catch (_) {}
    return 'Erro HTTP ${res.statusCode}';
  }

  Map<String, String> authHeaders(String? token) => {
        'Content-Type': 'application/json',
        'X-Site-Id': siteCode,
        if (token != null) 'Authorization': 'Bearer $token',
      };
}
