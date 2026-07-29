import 'dart:convert';
import 'package:http/http.dart' as http;
import '../core/config.dart';
import 'campo_local_store.dart';

enum SyncUiState { offline, syncing, synced, error }

/// Motor de sync: push batch + backoff + remapeamento de IDs.
class CampoSyncEngine {
  CampoSyncEngine({
    required this.store,
    required this.getAccessToken,
    required this.apiBase,
    required this.siteCode,
  });

  final CampoLocalStore store;
  final Future<String?> Function() getAccessToken;
  final String Function() apiBase;
  final String Function() siteCode;

  SyncUiState state = SyncUiState.synced;
  String? lastError;

  Future<void> syncAll({required bool online}) async {
    if (!online) {
      state = SyncUiState.offline;
      return;
    }
    state = SyncUiState.syncing;
    lastError = null;
    try {
      await _push();
      await _pullWorkOrders();
      state = SyncUiState.synced;
    } catch (e) {
      state = SyncUiState.error;
      lastError = e.toString();
    }
  }

  Future<void> _push() async {
    final token = await getAccessToken();
    if (token == null) return;

    final items = await store.peekQueue(limit: AppConfig.syncBatchSize);
    if (items.isEmpty) return;

    final body = {
      'items': items.map((row) {
        return {
          'operation': row['operation'],
          'entityType': row['entity_type'],
          'entityId': row['entity_id'],
          'payload': jsonDecode(row['payload_json'] as String),
        };
      }).toList(),
    };

    final uri = Uri.parse('${apiBase()}/sync/push');
    final res = await http.post(
      uri,
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
        'X-Site-Id': siteCode(),
      },
      body: jsonEncode(body),
    );

    if (res.statusCode >= 200 && res.statusCode < 300) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      final synced = (data['syncedIds'] as List?)?.cast<String>() ?? [];
      final remapped = (data['remappedIds'] as Map?)?.cast<String, dynamic>() ?? {};

      for (final row in items) {
        final eid = row['entity_id'] as String;
        final qid = row['id'] as int;
        if (synced.contains(eid) || remapped.containsKey(eid)) {
          await store.removeQueueItem(qid);
          final realId = remapped[eid]?.toString() ?? eid;
          if (row['entity_type'] == 'workOrder') {
            final payload = jsonDecode(row['payload_json'] as String) as Map<String, dynamic>;
            payload['id'] = realId;
            await store.upsertWorkOrder(realId, payload, syncStatus: 'synced');
          }
        } else {
          final retry = (row['retry_count'] as int) + 1;
          final next = DateTime.now().millisecondsSinceEpoch + AppConfig.backoffMs(retry);
          await store.bumpRetry(qid, retry, next, 'not in syncedIds');
        }
      }
      return;
    }

    // Fallback por-item em falha de batch
    for (final row in items) {
      await _pushOne(row, token);
    }
  }

  Future<void> _pushOne(Map<String, dynamic> row, String token) async {
    final qid = row['id'] as int;
    final body = {
      'items': [
        {
          'operation': row['operation'],
          'entityType': row['entity_type'],
          'entityId': row['entity_id'],
          'payload': jsonDecode(row['payload_json'] as String),
        }
      ],
    };
    try {
      final res = await http.post(
        Uri.parse('${apiBase()}/sync/push'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
          'X-Site-Id': siteCode(),
        },
        body: jsonEncode(body),
      );
      if (res.statusCode >= 200 && res.statusCode < 300) {
        await store.removeQueueItem(qid);
      } else {
        final retry = (row['retry_count'] as int) + 1;
        final next = DateTime.now().millisecondsSinceEpoch + AppConfig.backoffMs(retry);
        await store.bumpRetry(qid, retry, next, 'HTTP ${res.statusCode}');
      }
    } catch (e) {
      final retry = (row['retry_count'] as int) + 1;
      final next = DateTime.now().millisecondsSinceEpoch + AppConfig.backoffMs(retry);
      await store.bumpRetry(qid, retry, next, e.toString());
    }
  }

  Future<void> _pullWorkOrders() async {
    final token = await getAccessToken();
    if (token == null) return;
    final uri = Uri.parse('${apiBase()}/work-orders').replace(queryParameters: {
      'limit': '50',
      'page': '1',
    });
    final res = await http.get(
      uri,
      headers: {
        'Authorization': 'Bearer $token',
        'X-Site-Id': siteCode(),
      },
    );
    if (res.statusCode < 200 || res.statusCode >= 300) return;
    final data = jsonDecode(res.body);
    final list = (data is Map && data['data'] is List)
        ? data['data'] as List
        : (data is List ? data : <dynamic>[]);
    for (final item in list) {
      if (item is Map<String, dynamic> && item['id'] != null) {
        // LWW: só aplica se local não estiver pending
        await store.upsertWorkOrder(item['id'].toString(), item, syncStatus: 'synced');
      }
    }
  }
}
