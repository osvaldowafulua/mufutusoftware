import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';
import 'avaria_page.dart';
import 'change_pin_page.dart';
import 'checklist_page.dart';
import 'ots_page.dart';
import 'presenca_page.dart';
import 'sync_page.dart';

class CampoHubPage extends StatelessWidget {
  const CampoHubPage({super.key});

  @override
  Widget build(BuildContext context) {
    final app = context.watch<AppState>();
    if (app.mustChangePin) {
      return const ChangePinPage();
    }
    final name = [
      app.user?['firstName'],
      app.user?['lastName'],
    ].whereType<String>().where((s) => s.isNotEmpty).join(' ');

    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1A2332),
        title: const Text('MUFUTU Campo'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: Center(
              child: _SyncChip(label: app.syncLabel, online: app.online),
            ),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            name.isEmpty ? 'Técnico' : name,
            style: const TextStyle(color: Color(0xFFE8EDF4), fontSize: 18, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 4),
          Text(
            app.online ? 'Com rede' : 'Sem rede — a trabalhar offline',
            style: const TextStyle(color: Color(0xFF8B9CB3), fontSize: 13),
          ),
          const SizedBox(height: 20),
          _tile(context, Icons.assignment, 'Minhas OTs', const OtsPage()),
          _tile(context, Icons.warning_amber, 'Reportar avaria', const AvariaPage()),
          _tile(context, Icons.checklist, 'Checklist', const ChecklistPage()),
          _tile(context, Icons.fingerprint, 'Presença', const PresencaPage()),
          _tile(context, Icons.sync, 'Enviar dados', const SyncPage()),
          const SizedBox(height: 24),
          TextButton(
            onPressed: () => app.logout(),
            child: const Text('Sair', style: TextStyle(color: Color(0xFF8B9CB3))),
          ),
        ],
      ),
    );
  }

  Widget _tile(BuildContext context, IconData icon, String label, Widget page) {
    return Card(
      color: const Color(0xFF1A2332),
      child: ListTile(
        leading: Icon(icon, color: const Color(0xFFEB5E28)),
        title: Text(label, style: const TextStyle(color: Color(0xFFE8EDF4))),
        trailing: const Icon(Icons.chevron_right, color: Color(0xFF8B9CB3)),
        onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => page)),
      ),
    );
  }
}

class _SyncChip extends StatelessWidget {
  const _SyncChip({required this.label, required this.online});
  final String label;
  final bool online;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: online ? const Color(0xFF0D3B2E) : const Color(0xFF3B1D0D),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(label, style: TextStyle(fontSize: 12, color: online ? Colors.greenAccent : Colors.orangeAccent)),
    );
  }
}
