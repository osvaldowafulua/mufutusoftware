import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class ChecklistPage extends StatefulWidget {
  const ChecklistPage({super.key});

  @override
  State<ChecklistPage> createState() => _ChecklistPageState();
}

class _ChecklistPageState extends State<ChecklistPage> {
  final items = {
    'Óleo motor': false,
    'Filtros': false,
    'Travões': false,
    'Pneus / lagartas': false,
    'Luzes': false,
  };
  String? msg;

  Future<void> _save() async {
    final app = context.read<AppState>();
    await app.data.saveChecklist({
      'type': 'general',
      'items': items.entries.map((e) => {'label': e.key, 'ok': e.value}).toList(),
      'at': DateTime.now().toIso8601String(),
    });
    await app.runSync();
    setState(() => msg = 'Checklist guardado na fila offline.');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(backgroundColor: const Color(0xFF1A2332), title: const Text('Checklist')),
      body: ListView(
        children: [
          ...items.keys.map((k) => CheckboxListTile(
                title: Text(k, style: const TextStyle(color: Color(0xFFE8EDF4))),
                value: items[k],
                activeColor: const Color(0xFFEB5E28),
                onChanged: (v) => setState(() => items[k] = v ?? false),
              )),
          Padding(
            padding: const EdgeInsets.all(16),
            child: FilledButton(
              onPressed: _save,
              style: FilledButton.styleFrom(backgroundColor: const Color(0xFFEB5E28)),
              child: const Text('Guardar'),
            ),
          ),
          if (msg != null)
            Padding(
              padding: const EdgeInsets.all(16),
              child: Text(msg!, style: const TextStyle(color: Colors.greenAccent)),
            ),
        ],
      ),
    );
  }
}
