import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';
import '../../patient/auth/providers/auth_provider.dart';

class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _fadeAnim;
  late Animation<double> _scaleAnim;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    );
    _fadeAnim  = CurvedAnimation(parent: _controller, curve: Curves.easeIn);
    _scaleAnim = Tween<double>(begin: 0.85, end: 1.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOutBack));
    _controller.forward();
    _navigate();
  }

  Future<void> _navigate() async {
    await Future.delayed(const Duration(milliseconds: 2000));
    if (!mounted) return;
    final authState = ref.read(authProvider);
    if (authState.isInitializing) {
      await Future.delayed(const Duration(milliseconds: 500));
    }
    if (!mounted) return;
    final isAuth = ref.read(authProvider).status == AuthStatus.authenticated;
    context.go(isAuth ? '/home' : '/login');
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        body: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [AppColors.navy800, AppColors.navy600],
            ),
          ),
          child: Center(
            child: FadeTransition(
              opacity: _fadeAnim,
              child: ScaleTransition(
                scale: _scaleAnim,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 100, height: 100,
                      decoration: BoxDecoration(
                        color: AppColors.white,
                        borderRadius: BorderRadius.circular(24),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.navy800.withOpacity(0.4),
                            blurRadius: 30,
                            offset: const Offset(0, 10)),
                        ],
                      ),
                      child: Center(
                        child: Text('م+',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 40,
                            fontWeight: FontWeight.w800,
                            color: AppColors.navy600)),
                      ),
                    ),
                    const SizedBox(height: 28),
                    Text('معافى+',
                      style: GoogleFonts.ibmPlexSansArabic(
                        fontSize: 36,
                        fontWeight: FontWeight.w800,
                        color: AppColors.white,
                        letterSpacing: 1)),
                    const SizedBox(height: 8),
                    Text('التثقيف الصحي الذكي',
                      style: GoogleFonts.ibmPlexSansArabic(
                        fontSize: 16,
                        color: AppColors.white.withOpacity(0.65))),
                    const SizedBox(height: 60),
                    SizedBox(
                      width: 28, height: 28,
                      child: CircularProgressIndicator(
                        color: AppColors.white.withOpacity(0.5),
                        strokeWidth: 2,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
