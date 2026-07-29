import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/config.dart';
import '../state/app_state.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  bool pinMode = true;
  final apiCtrl = TextEditingController(text: AppConfig.defaultApiBase);
  final idCtrl = TextEditingController();
  final pinCtrl = TextEditingController();
  final emailCtrl = TextEditingController();
  final passCtrl = TextEditingController();
  String? error;
  bool busy = false;

  @override
  void dispose() {
    apiCtrl.dispose();
    idCtrl.dispose();
    pinCtrl.dispose();
    emailCtrl.dispose();
    passCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final app = context.read<AppState>();
    setState(() {
      busy = true;
      error = null;
    });
    try {
      app.setApiBase(apiCtrl.text.trim());
      if (pinMode) {
        await app.loginPin(idCtrl.text.trim(), pinCtrl.text.trim());
      } else {
        await app.loginEmail(emailCtrl.text.trim(), passCtrl.text);
      }
    } catch (e) {
      setState(() => error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    const brand = Color(0xFFEB5E28);
    return Scaffold(
      backgroundColor: const Color(0xFF0F1419),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const SizedBox(height: 24),
            Center(
              child: Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: brand,
                  borderRadius: BorderRadius.circular(12),
                ),
                alignment: Alignment.center,
                child: const Text('M',
                    style: TextStyle(
                        color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold)),
              ),
            ),
            const SizedBox(height: 16),
            const Text('MUFUTU Campo',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xFFE8EDF4), fontSize: 22, fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            const Text('Offline-first · técnicos',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xFF8B9CB3), fontSize: 13)),
            const SizedBox(height: 24),
            SegmentedButton<bool>(
              segments: const [
                ButtonSegment(value: true, label: Text('PIN')),
                ButtonSegment(value: false, label: Text('Email')),
              ],
              selected: {pinMode},
              onSelectionChanged: (s) => setState(() => pinMode = s.first),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: apiCtrl,
              style: const TextStyle(color: Color(0xFFE8EDF4)),
              decoration: _dec('API'),
            ),
            const SizedBox(height: 12),
            if (pinMode) ...[
              TextField(
                controller: idCtrl,
                style: const TextStyle(color: Color(0xFFE8EDF4)),
                decoration: _dec('Telemóvel ou alias'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: pinCtrl,
                obscureText: true,
                keyboardType: TextInputType.number,
                style: const TextStyle(color: Color(0xFFE8EDF4)),
                decoration: _dec('PIN (4–6 dígitos)'),
              ),
            ] else ...[
              TextField(
                controller: emailCtrl,
                style: const TextStyle(color: Color(0xFFE8EDF4)),
                decoration: _dec('Email'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: passCtrl,
                obscureText: true,
                style: const TextStyle(color: Color(0xFFE8EDF4)),
                decoration: _dec('Palavra-passe'),
              ),
            ],
            if (error != null) ...[
              const SizedBox(height: 12),
              Text(error!, style: const TextStyle(color: Colors.redAccent)),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: busy ? null : _submit,
              style: FilledButton.styleFrom(
                backgroundColor: brand,
                minimumSize: const Size.fromHeight(48),
              ),
              child: Text(busy ? 'A entrar…' : 'Entrar'),
            ),
          ],
        ),
      ),
    );
  }

  InputDecoration _dec(String label) => InputDecoration(
        labelText: label,
        labelStyle: const TextStyle(color: Color(0xFF8B9CB3)),
        filled: true,
        fillColor: const Color(0xFF1A2332),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
      );
}
