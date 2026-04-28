import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/physician_auth_provider.dart';

class ProviderLoginScreen extends ConsumerStatefulWidget {
  const ProviderLoginScreen({super.key});

  @override
  ConsumerState<ProviderLoginScreen> createState() =>
      _ProviderLoginScreenState();
}

class _ProviderLoginScreenState extends ConsumerState<ProviderLoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    final success = await ref
        .read(physicianAuthProvider.notifier)
        .login(_emailController.text.trim(), _passwordController.text);
    if (success && mounted) {
      context.go('/provider/dashboard');
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(physicianAuthProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFF283481),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 48),
                // Logo / wordmark
                const Center(
                  child: Text('معافى+',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 36,
                      fontWeight: FontWeight.bold)),
                ),
                const SizedBox(height: 8),
                const Center(
                  child: Text('لوحة تحكم مزود الخدمة الصحية',
                    style: TextStyle(color: Colors.white70, fontSize: 14)),
                ),
                const SizedBox(height: 48),
                // Login card
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Text('تسجيل الدخول',
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF0E1726)),
                        textAlign: TextAlign.right),
                      const SizedBox(height: 24),
                      // Email field
                      TextField(
                        controller: _emailController,
                        keyboardType: TextInputType.emailAddress,
                        textDirection: TextDirection.ltr,
                        textAlign: TextAlign.left,
                        decoration: InputDecoration(
                          labelText: 'البريد الإلكتروني',
                          hintText: 'doctor@hospital.ye',
                          hintStyle: const TextStyle(color: Color(0xFF8A93A6)),
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: Color(0xFF283481), width: 2)),
                          prefixIcon: const Icon(Icons.email_outlined),
                        ),
                      ),
                      const SizedBox(height: 16),
                      // Password field
                      TextField(
                        controller: _passwordController,
                        obscureText: _obscurePassword,
                        textDirection: TextDirection.ltr,
                        onSubmitted: (_) => _login(),
                        decoration: InputDecoration(
                          labelText: 'كلمة المرور',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: Color(0xFF283481), width: 2)),
                          prefixIcon: const Icon(Icons.lock_outlined),
                          suffixIcon: IconButton(
                            icon: Icon(_obscurePassword
                              ? Icons.visibility_outlined
                              : Icons.visibility_off_outlined),
                            onPressed: () => setState(
                              () => _obscurePassword = !_obscurePassword),
                          ),
                        ),
                      ),
                      // Error banner
                      if (authState.error != null) ...[
                        const SizedBox(height: 12),
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFBE5E5),
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: const Color(0xFFF5B8B8))),
                          child: Text(authState.error!,
                            style: const TextStyle(
                              color: Color(0xFFD64545)),
                            textAlign: TextAlign.right),
                        ),
                      ],
                      const SizedBox(height: 24),
                      // Login button
                      ElevatedButton(
                        onPressed: authState.isLoading ? null : _login,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF283481),
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                        ),
                        child: authState.isLoading
                          ? const SizedBox(height: 20, width: 20,
                              child: CircularProgressIndicator(
                                color: Colors.white, strokeWidth: 2))
                          : const Text('دخول',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: Colors.white)),
                      ),
                      const SizedBox(height: 16),
                      // Link to patient login
                      GestureDetector(
                        onTap: () => context.go('/login'),
                        child: const Text('تسجيل دخول المرضى',
                          style: TextStyle(
                            color: Color(0xFF283481),
                            fontWeight: FontWeight.w600,
                            fontSize: 13),
                          textAlign: TextAlign.center),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
