import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../state/app_state.dart';

class ChangePinPage extends StatefulWidget {
  const ChangePinPage({super.key});

  @override
  State<ChangePinPage> createState() => _ChangePinPageState();
}

class _ChangePinPageState extends State<ChangePinPage> {
  final current = TextEditingController();
  final next = TextEditingController();
  final confirm = TextEditingController();
  String? error;
  bool busy = false;

  @override
  void dispose() {
    current.dispose();
    next.dispose();
    confirm.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (next.text != confirm.text) {
      setState(() => error = 'Confirmação não coincide');
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await context.read<AppState>().changePin(current.text, next.text);
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1A2332),
        title: const Text('Alterar PIN'),
        automaticallyImplyLeading: false,
      ),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const Text('Por segurança, altere o PIN inicial antes de continuar.',
              style: TextStyle(color: Color(0xFF8B9CB3))),
          const SizedBox(height: 16),
          TextField(
            controller: current,
            obscureText: true,
            keyboardType: TextInputType.number,
            style: const TextStyle(color: Color(0xFFE8EDF4)),
            decoration: const InputDecoration(labelText: 'PIN actual'),
          ),
          TextField(
            controller: next,
            obscureText: true,
            keyboardType: TextInputType.number,
            style: const TextStyle(color: Color(0xFFE8EDF4)),
            decoration: const InputDecoration(labelText: 'Novo PIN'),
          ),
          TextField(
            controller: confirm,
            obscureText: true,
            keyboardType: TextInputType.number,
            style: const TextStyle(color: Color(0xFFE8EDF4)),
            decoration: const InputDecoration(labelText: 'Confirmar'),
          ),
          if (error != null) Text(error!, style: const TextStyle(color: Colors.redAccent)),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: busy ? null : _save,
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFEB5E28)),
            child: Text(busy ? 'A guardar…' : 'Guardar PIN'),
          ),
        ],
      ),
    );
  }
}
