import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../patient/auth/providers/auth_provider.dart';

class SplashDeciderScreen extends ConsumerStatefulWidget {
  const SplashDeciderScreen({super.key});

  @override
  ConsumerState<SplashDeciderScreen> createState() =>
      _SplashDeciderScreenState();
}

class _SplashDeciderScreenState extends ConsumerState<SplashDeciderScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final auth = ref.read(authProvider);
      if (auth.token != null) {
        context.go('/home');
      } else {
        context.go('/login');
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Color(0xFF283481),
      body: Center(
        child: CircularProgressIndicator(color: Colors.white),
      ),
    );
  }
}
