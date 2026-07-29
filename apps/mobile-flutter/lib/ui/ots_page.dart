import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class OtsPage extends StatefulWidget {
  const OtsPage({super.key});

  @override
  State<OtsPage> createState() => _OtsPageState();
}

class _OtsPageState extends State<OtsPage> {
  List<Map<String, dynamic>> items = [];
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final app = context.read<AppState>();
    final list = await app.data.listOts();
    setState(() {
      items = list;
      loading = false;
    });
  }

  Future<void> _setStatus(String id, String status) async {
    await context.read<AppState>().data.changeWoStatus(id, status);
    await context.read<AppState>().runSync();
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1A2332),
        title: const Text('Minhas OTs'),
        actions: [
          IconButton(onPressed: _load, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : items.isEmpty
              ? const Center(
                  child: Text('Sem OTs em cache.\nSincronize com rede.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Color(0xFF8B9CB3))))
              : ListView.builder(
                  itemCount: items.length,
                  itemBuilder: (_, i) {
                    final wo = items[i];
                    final id = wo['id']?.toString() ?? '';
                    final status = wo['status']?.toString() ?? '';
                    return Card(
                      color: const Color(0xFF1A2332),
                      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      child: ListTile(
                        title: Text(wo['code']?.toString() ?? id,
                            style: const TextStyle(color: Color(0xFFE8EDF4))),
                        subtitle: Text('${wo['title'] ?? ''} · $status',
                            style: const TextStyle(color: Color(0xFF8B9CB3))),
                        trailing: Wrap(
                          spacing: 4,
                          children: [
                            if (status != 'in_progress')
                              TextButton(
                                onPressed: () => _setStatus(id, 'in_progress'),
                                child: const Text('Começar'),
                              ),
                            if (status == 'in_progress')
                              TextButton(
                                onPressed: () => _setStatus(id, 'completed'),
                                child: const Text('Terminei'),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
    );
  }
}
