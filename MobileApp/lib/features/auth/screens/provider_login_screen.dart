import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';
import '../../provider/providers/physician_auth_provider.dart';

// Provider (physician/admin) login — Phase 4B
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

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.navy800,
        body: Column(
          children: [
            Expanded(
              flex: 35,
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [AppColors.navy800, AppColors.navy600],
                  ),
                ),
                child: SafeArea(
                  bottom: false,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 28),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 14, vertical: 8),
                          decoration: BoxDecoration(
                            color: AppColors.white,
                            borderRadius: BorderRadius.circular(12)),
                          child: Text('معافى+',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 18,
                              fontWeight: FontWeight.w700,
                              color: AppColors.navy600)),
                        ),
                        const SizedBox(height: 20),
                        Text('دخول مزود الخدمة',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 24,
                            fontWeight: FontWeight.w700,
                            color: AppColors.white)),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            Expanded(
              flex: 65,
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: AppColors.ink50,
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(24))),
                child: SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(28, 32, 28, 28),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text('البريد الإلكتروني',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink700)),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _emailController,
                        keyboardType: TextInputType.emailAddress,
                        textDirection: TextDirection.ltr,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 15, color: AppColors.ink900),
                        decoration: InputDecoration(
                          hintText: 'doctor@hospital.ye',
                          hintStyle: GoogleFonts.ibmPlexSansArabic(
                            color: AppColors.ink400),
                          filled: true,
                          fillColor: AppColors.white,
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.ink100)),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.ink100)),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.navy600, width: 1.5)),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 14),
                        ),
                      ),
                      const SizedBox(height: 20),
                      Text('كلمة المرور',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink700)),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _passwordController,
                        obscureText: _obscurePassword,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 15, color: AppColors.ink900),
                        decoration: InputDecoration(
                          hintText: '••••••••',
                          hintStyle: GoogleFonts.ibmPlexSansArabic(
                            color: AppColors.ink400),
                          filled: true,
                          fillColor: AppColors.white,
                          suffixIcon: IconButton(
                            icon: Icon(
                              _obscurePassword
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                              color: AppColors.ink400),
                            onPressed: () => setState(
                              () => _obscurePassword = !_obscurePassword)),
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.ink100)),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.ink100)),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: AppColors.navy600, width: 1.5)),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 14),
                        ),
                      ),
                      const SizedBox(height: 32),
                      SizedBox(
                        height: 52,
                        child: ElevatedButton(
                          onPressed: ref.watch(physicianAuthProvider).isLoading
                            ? null
                            : () async {
                                final email = _emailController.text.trim();
                                final password = _passwordController.text.trim();
                                if (email.isEmpty || password.isEmpty) return;
                                await ref.read(physicianAuthProvider.notifier)
                                    .login(email, password);
                                final state = ref.read(physicianAuthProvider);
                                if (state.token != null && mounted) {
                                  context.go('/provider/dashboard');
                                }
                              },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.navy600,
                            foregroundColor: AppColors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12)),
                            elevation: 0),
                          child: ref.watch(physicianAuthProvider).isLoading
                            ? const SizedBox(width: 20, height: 20,
                                child: CircularProgressIndicator(
                                  color: Colors.white, strokeWidth: 2))
                            : Text('دخول',
                                style: GoogleFonts.ibmPlexSansArabic(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700)),
                        ),
                      ),
                      ref.watch(physicianAuthProvider).error != null
                        ? Padding(
                            padding: const EdgeInsets.only(top: 8),
                            child: Text(
                              ref.watch(physicianAuthProvider).error!,
                              style: const TextStyle(
                                color: Colors.red, fontSize: 12),
                              textAlign: TextAlign.center,
                            ),
                          )
                        : const SizedBox.shrink(),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
