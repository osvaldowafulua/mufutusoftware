import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class SyncPage extends StatelessWidget {
  const SyncPage({super.key});

  @override
  Widget build(BuildContext context) {
    final app = context.watch<AppState>();
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(backgroundColor: const Color(0xFF1A2332), title: const Text('Sincronização')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Estado: ${app.syncLabel}',
                style: const TextStyle(color: Color(0xFFE8EDF4), fontSize: 18)),
            const SizedBox(height: 8),
            Text('Pendentes: ${app.pending}',
                style: const TextStyle(color: Color(0xFF8B9CB3))),
            if (app.syncError != null) ...[
              const SizedBox(height: 8),
              Text(app.syncError!, style: const TextStyle(color: Colors.redAccent)),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: app.online ? () => app.runSync() : null,
              style: FilledButton.styleFrom(backgroundColor: const Color(0xFFEB5E28)),
              child: const Text('Enviar agora'),
            ),
          ],
        ),
      ),
    );
  }
}
