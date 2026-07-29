import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class AvariaPage extends StatefulWidget {
  const AvariaPage({super.key});

  @override
  State<AvariaPage> createState() => _AvariaPageState();
}

class _AvariaPageState extends State<AvariaPage> {
  final titleCtrl = TextEditingController();
  String priority = 'high';
  String? msg;

  @override
  void dispose() {
    titleCtrl.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final app = context.read<AppState>();
    final id = await app.data.reportAvaria(
      title: titleCtrl.text.trim().isEmpty ? 'Avaria campo' : titleCtrl.text.trim(),
      priority: priority,
    );
    await app.runSync();
    setState(() => msg = 'Guardado offline ($id). Envia quando houver rede.');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(backgroundColor: const Color(0xFF1A2332), title: const Text('Avaria')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(
              controller: titleCtrl,
              style: const TextStyle(color: Color(0xFFE8EDF4)),
              decoration: const InputDecoration(
                labelText: 'Descrição',
                labelStyle: TextStyle(color: Color(0xFF8B9CB3)),
              ),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              value: priority,
              dropdownColor: const Color(0xFF1A2332),
              decoration: const InputDecoration(labelText: 'Prioridade'),
              items: const [
                DropdownMenuItem(value: 'critical', child: Text('Crítica')),
                DropdownMenuItem(value: 'high', child: Text('Alta')),
                DropdownMenuItem(value: 'medium', child: Text('Média')),
                DropdownMenuItem(value: 'low', child: Text('Baixa')),
              ],
              onChanged: (v) => setState(() => priority = v ?? 'high'),
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _send,
              style: FilledButton.styleFrom(backgroundColor: const Color(0xFFEB5E28)),
              child: const Text('Guardar (offline-first)'),
            ),
            if (msg != null) ...[
              const SizedBox(height: 12),
              Text(msg!, style: const TextStyle(color: Colors.greenAccent)),
            ],
          ],
        ),
      ),
    );
  }
}
