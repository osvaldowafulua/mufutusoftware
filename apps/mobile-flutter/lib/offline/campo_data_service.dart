import 'package:uuid/uuid.dart';
import '../api/mufutu_api_client.dart';
import 'campo_local_store.dart';
import 'campo_sync_engine.dart';

/// Write-local-first para OTs / PT / presença / checklist.
class CampoDataService {
  CampoDataService({
    required this.store,
    required this.sync,
    required this.api,
  });

  final CampoLocalStore store;
  final CampoSyncEngine sync;
  final MufutuApiClient api;
  final _uuid = const Uuid();

  Future<List<Map<String, dynamic>>> listOts() => store.listWorkOrders();

  Future<void> changeWoStatus(String id, String status) async {
    final list = await store.listWorkOrders();
    final existing = list.cast<Map<String, dynamic>?>().firstWhere(
          (w) => w?['id'] == id,
          orElse: () => {'id': id},
        )!;
    existing['status'] = status;
    existing['updatedAt'] = DateTime.now().toIso8601String();
    await store.upsertWorkOrder(id, existing, syncStatus: 'pending');
    await store.enqueue(
      entityType: 'workOrder',
      entityId: id,
      operation: 'update',
      payload: {'id': id, 'status': status},
      priority: 1,
    );
  }

  Future<String> reportAvaria({
    required String title,
    required String priority,
    String? assetId,
    List<String> photos = const [],
  }) async {
    final tempId = 'mr-offline-${DateTime.now().millisecondsSinceEpoch}-${_uuid.v4().substring(0, 8)}';
    final payload = {
      'id': tempId,
      'title': title,
      'priority': priority,
      if (assetId != null) 'assetId': assetId,
      'photos': photos,
    };
    await store.enqueue(
      entityType: 'maintenanceRequest',
      entityId: tempId,
      operation: 'create',
      payload: payload,
      priority: 1,
    );
    return tempId;
  }

  Future<String> checkIn({
    double? lat,
    double? lng,
    String? photoPath,
  }) async {
    final tempId = 'ci-offline-${DateTime.now().millisecondsSinceEpoch}';
    await store.enqueue(
      entityType: 'attendanceCheckIn',
      entityId: tempId,
      operation: 'create',
      payload: {
        'id': tempId,
        'lat': lat?.toString(),
        'lng': lng?.toString(),
        'photoPath': photoPath,
        'at': DateTime.now().toIso8601String(),
      },
      priority: 1,
    );
    return tempId;
  }

  Future<void> saveChecklist(Map<String, dynamic> checklist) async {
    final id = checklist['id']?.toString() ??
        'cl-offline-${DateTime.now().millisecondsSinceEpoch}';
    checklist['id'] = id;
    await store.saveChecklist(id, checklist);
    await store.enqueue(
      entityType: 'fieldChecklist',
      entityId: id,
      operation: 'create',
      payload: checklist,
      priority: 2,
    );
  }
}
