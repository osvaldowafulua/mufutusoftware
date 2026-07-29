import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'state/app_state.dart';
import 'ui/campo_hub_page.dart';
import 'ui/login_page.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const MufutuCampoApp());
}

class MufutuCampoApp extends StatelessWidget {
  const MufutuCampoApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => AppState(),
      child: MaterialApp(
        title: 'MUFUTU Campo',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(
            seedColor: const Color(0xFFEB5E28),
            brightness: Brightness.dark,
          ),
          useMaterial3: true,
        ),
        home: Consumer<AppState>(
          builder: (context, app, _) {
            if (!app.ready) {
              return const Scaffold(
                backgroundColor: Color(0xFF0F1419),
                body: Center(child: CircularProgressIndicator()),
              );
            }
            return app.loggedIn ? const CampoHubPage() : const LoginPage();
          },
        ),
      ),
    );
  }
}
