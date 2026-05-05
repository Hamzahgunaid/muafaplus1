import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../patient/auth/providers/auth_provider.dart';
import '../../provider/providers/physician_auth_provider.dart';

class SplashDeciderScreen extends ConsumerStatefulWidget {
  const SplashDeciderScreen({super.key});

  @override
  ConsumerState<SplashDeciderScreen> createState() =>
      _SplashDeciderScreenState();
}

class _SplashDeciderScreenState extends ConsumerState<SplashDeciderScreen> {
  bool _navigated = false;

  void _tryNavigate() {
    if (_navigated || !mounted) return;
    final auth = ref.read(authProvider);
    final physician = ref.read(physicianAuthProvider);
    if (auth.isInitializing || physician.isInitializing) return;
    _navigated = true;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      if (auth.token != null) {
        context.go('/home');
      } else if (physician.token != null) {
        context.go('/provider/dashboard');
      } else {
        context.go('/login');
      }
    });
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _tryNavigate());
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(authProvider, (_, __) => _tryNavigate());
    ref.listen(physicianAuthProvider, (_, __) => _tryNavigate());
    return const Scaffold(
      backgroundColor: Color(0xFF1E3A72),
      body: Center(
        child: CircularProgressIndicator(color: Colors.white),
      ),
    );
  }
}
