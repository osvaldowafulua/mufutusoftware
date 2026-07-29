import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class PresencaPage extends StatefulWidget {
  const PresencaPage({super.key});

  @override
  State<PresencaPage> createState() => _PresencaPageState();
}

class _PresencaPageState extends State<PresencaPage> {
  String? msg;

  Future<void> _checkIn() async {
    final app = context.read<AppState>();
    final id = await app.data.checkIn();
    await app.runSync();
    setState(() => msg = 'Presença enfileirada ($id).');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(backgroundColor: const Color(0xFF1A2332), title: const Text('Presença')),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            FilledButton(
              onPressed: _checkIn,
              style: FilledButton.styleFrom(
                backgroundColor: const Color(0xFFEB5E28),
                minimumSize: const Size(220, 52),
              ),
              child: const Text('Confirmar presença'),
            ),
            if (msg != null) ...[
              const SizedBox(height: 16),
              Text(msg!, style: const TextStyle(color: Colors.greenAccent)),
            ],
          ],
        ),
      ),
    );
  }
}
