import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';
import '../api/mufutu_api_client.dart';
import '../core/config.dart';
import '../offline/campo_data_service.dart';
import '../offline/campo_local_store.dart';
import '../offline/campo_sync_engine.dart';

class AppState extends ChangeNotifier {
  AppState() {
    _init();
  }

  final api = MufutuApiClient();
  late final CampoLocalStore store;
  late final CampoSyncEngine syncEngine;
  late final CampoDataService data;

  bool ready = false;
  bool online = true;
  bool loggedIn = false;
  bool mustChangePin = false;
  Map<String, dynamic>? user;
  int pending = 0;
  SyncUiState syncUi = SyncUiState.synced;
  String? syncError;
  Timer? _timer;
  StreamSubscription? _connSub;

  Future<void> _init() async {
    store = CampoLocalStore();
    await store.db;
    syncEngine = CampoSyncEngine(
      store: store,
      getAccessToken: () => api.accessToken,
      apiBase: () => api.apiBase,
      siteCode: () => api.siteCode,
    );
    data = CampoDataService(store: store, sync: syncEngine, api: api);

    final token = await api.accessToken;
    loggedIn = token != null && token.isNotEmpty;

    _connSub = Connectivity().onConnectivityChanged.listen((r) {
      online = !r.contains(ConnectivityResult.none);
      notifyListeners();
      if (online) unawaited(runSync());
    });
    final initial = await Connectivity().checkConnectivity();
    online = !initial.contains(ConnectivityResult.none);

    _timer = Timer.periodic(const Duration(seconds: 30), (_) {
      if (online && loggedIn) unawaited(runSync());
    });

    pending = await store.pendingCount();
    ready = true;
    notifyListeners();
  }

  Future<void> loginEmail(String email, String password) async {
    final s = await api.loginEmail(email, password);
    loggedIn = true;
    user = s.user;
    mustChangePin = s.mustChangePin;
    notifyListeners();
    await runSync();
  }

  Future<void> loginPin(String identifier, String pin) async {
    final s = await api.loginPin(identifier, pin);
    loggedIn = true;
    user = s.user;
    mustChangePin = s.mustChangePin;
    notifyListeners();
    await runSync();
  }

  Future<void> changePin(String current, String next) async {
    await api.changePin(current, next);
    mustChangePin = false;
    notifyListeners();
  }

  Future<void> logout() async {
    await api.clearSession();
    loggedIn = false;
    user = null;
    notifyListeners();
  }

  Future<void> runSync() async {
    await syncEngine.syncAll(online: online);
    syncUi = syncEngine.state;
    syncError = syncEngine.lastError;
    pending = await store.pendingCount();
    notifyListeners();
  }

  void setApiBase(String url) {
    api.apiBase = url.replaceAll(RegExp(r'/+$'), '');
    if (!api.apiBase.endsWith('/api')) {
      // allow either origin or /api
    }
    notifyListeners();
  }

  String get syncLabel {
    if (!online) return 'Offline';
    switch (syncUi) {
      case SyncUiState.syncing:
        return 'A sincronizar';
      case SyncUiState.error:
        return 'Erro sync';
      case SyncUiState.offline:
        return 'Offline';
      case SyncUiState.synced:
        return pending > 0 ? '$pending pendente(s)' : 'Sincronizado';
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    _connSub?.cancel();
    super.dispose();
  }
}
