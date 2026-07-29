import 'dart:convert';
import 'package:path/path.dart' as p;
import 'package:sqflite/sqflite.dart';
import 'package:path_provider/path_provider.dart';

/// Store SQLite offline-first (paridade Web Dexie / antigo MAUI CampoOfflineStore).
class CampoLocalStore {
  static const _dbName = 'mufutu_campo.db';
  Database? _db;

  Future<Database> get db async {
    if (_db != null) return _db!;
    final dir = await getApplicationDocumentsDirectory();
    final path = p.join(dir.path, _dbName);
    _db = await openDatabase(
      path,
      version: 1,
      onCreate: (db, version) async {
        await db.execute('''
          CREATE TABLE work_orders (
            id TEXT PRIMARY KEY,
            json TEXT NOT NULL,
            sync_status TEXT NOT NULL DEFAULT 'synced',
            last_modified INTEGER NOT NULL
          )
        ''');
        await db.execute('''
          CREATE TABLE sync_queue (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            entity_type TEXT NOT NULL,
            entity_id TEXT NOT NULL,
            operation TEXT NOT NULL,
            payload_json TEXT NOT NULL,
            priority INTEGER NOT NULL DEFAULT 2,
            retry_count INTEGER NOT NULL DEFAULT 0,
            last_error TEXT,
            created_at INTEGER NOT NULL,
            next_attempt_at INTEGER NOT NULL DEFAULT 0
          )
        ''');
        await db.execute('''
          CREATE TABLE checklists (
            id TEXT PRIMARY KEY,
            json TEXT NOT NULL,
            sync_status TEXT NOT NULL DEFAULT 'pending',
            last_modified INTEGER NOT NULL
          )
        ''');
        await db.execute('''
          CREATE TABLE sync_meta (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
          )
        ''');
      },
    );
    return _db!;
  }

  Future<void> upsertWorkOrder(
    String id,
    Map<String, dynamic> json, {
    String syncStatus = 'synced',
  }) async {
    final database = await db;
    final existing = await database.query(
      'work_orders',
      where: 'id = ?',
      whereArgs: [id],
    );
    // LWW local-wins: não sobrescrever pending com pull remoto
    if (existing.isNotEmpty && existing.first['sync_status'] == 'pending') {
      return;
    }
    final now = DateTime.now().millisecondsSinceEpoch;
    await database.insert(
      'work_orders',
      {
        'id': id,
        'json': jsonEncode(json),
        'sync_status': syncStatus,
        'last_modified': now,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<List<Map<String, dynamic>>> listWorkOrders() async {
    final database = await db;
    final rows = await database.query('work_orders', orderBy: 'last_modified DESC');
    return rows.map((r) {
      final map = jsonDecode(r['json'] as String) as Map<String, dynamic>;
      map['_syncStatus'] = r['sync_status'];
      map['_lastModified'] = r['last_modified'];
      return map;
    }).toList();
  }

  Future<void> enqueue({
    required String entityType,
    required String entityId,
    required String operation,
    required Map<String, dynamic> payload,
    int priority = 2,
  }) async {
    final database = await db;
    final now = DateTime.now().millisecondsSinceEpoch;
    await database.insert('sync_queue', {
      'entity_type': entityType,
      'entity_id': entityId,
      'operation': operation,
      'payload_json': jsonEncode(payload),
      'priority': priority,
      'retry_count': 0,
      'created_at': now,
      'next_attempt_at': now,
    });
  }

  Future<List<Map<String, dynamic>>> peekQueue({int limit = 10}) async {
    final database = await db;
    final now = DateTime.now().millisecondsSinceEpoch;
    return database.query(
      'sync_queue',
      where: 'next_attempt_at <= ? AND retry_count < 5',
      whereArgs: [now],
      orderBy: 'priority ASC, created_at ASC',
      limit: limit,
    );
  }

  Future<int> pendingCount() async {
    final database = await db;
    final r = await database.rawQuery('SELECT COUNT(*) AS c FROM sync_queue');
    return Sqflite.firstIntValue(r) ?? 0;
  }

  Future<void> removeQueueItem(int id) async {
    final database = await db;
    await database.delete('sync_queue', where: 'id = ?', whereArgs: [id]);
  }

  Future<void> bumpRetry(int id, int retryCount, int nextAttemptAt, String? error) async {
    final database = await db;
    await database.update(
      'sync_queue',
      {
        'retry_count': retryCount,
        'next_attempt_at': nextAttemptAt,
        'last_error': error,
      },
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  Future<void> saveChecklist(String id, Map<String, dynamic> json) async {
    final database = await db;
    final now = DateTime.now().millisecondsSinceEpoch;
    await database.insert(
      'checklists',
      {
        'id': id,
        'json': jsonEncode(json),
        'sync_status': 'pending',
        'last_modified': now,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
}
