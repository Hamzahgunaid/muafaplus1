import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/constants/app_strings.dart';
import '../providers/auth_provider.dart';
import '../../../provider/providers/physician_auth_provider.dart';

class PatientLoginScreen extends ConsumerStatefulWidget {
  const PatientLoginScreen({super.key});
  @override
  ConsumerState<PatientLoginScreen> createState() => _PatientLoginScreenState();
}

class _PatientLoginScreenState extends ConsumerState<PatientLoginScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  // Patient tab
  final _phoneController = TextEditingController();
  final _codeController  = TextEditingController();

  // Provider tab
  final _emailController    = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    _phoneController.dispose();
    _codeController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _patientLogin() async {
    final phone = _phoneController.text.trim();
    final code  = _codeController.text.trim();
    if (phone.isEmpty || code.length != 4) return;
    final success = await ref.read(authProvider.notifier).login(phone, code);
    if (success && mounted) context.go('/home');
  }

  Future<void> _providerLogin() async {
    final success = await ref
        .read(physicianAuthProvider.notifier)
        .login(_emailController.text.trim(), _passwordController.text);
    if (success && mounted) context.go('/provider/dashboard');
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.navy800,
        body: Column(
          children: [

            // ── Navy hero ────────────────────────────────────────────────
            Container(
              height: 260,
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
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Image.asset(
                          'assets/images/muafa-logo-white.png',
                          height: 48,
                          fit: BoxFit.contain,
                          errorBuilder: (context, error, stackTrace) => const Text(
                            'معافى+',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 28,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 20),
                      Text(AppStrings.loginTitle,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 24, fontWeight: FontWeight.w700,
                          color: AppColors.white, height: 1.3)),
                      const SizedBox(height: 8),
                      Text(AppStrings.loginSubtitle,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, height: 1.6,
                          color: AppColors.white.withOpacity(0.65))),
                    ],
                  ),
                ),
              ),
            ),

            // ── White card with tabs ─────────────────────────────────────
            Expanded(
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: AppColors.ink50,
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(24)),
                ),
                child: Column(
                  children: [
                    // Tab bar
                    Container(
                      margin: const EdgeInsets.fromLTRB(24, 20, 24, 0),
                      decoration: BoxDecoration(
                        color: AppColors.ink100,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: TabBar(
                        controller: _tabController,
                        indicatorSize: TabBarIndicatorSize.tab,
                        indicator: BoxDecoration(
                          color: AppColors.navy600,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        labelColor: AppColors.white,
                        unselectedLabelColor: AppColors.ink700,
                        labelStyle: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, fontWeight: FontWeight.w600),
                        unselectedLabelStyle: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13),
                        dividerColor: Colors.transparent,
                        tabs: const [
                          Tab(text: 'دخول المريض'),
                          Tab(text: 'دخول مزود الخدمة'),
                        ],
                      ),
                    ),

                    // Tab content
                    Expanded(
                      child: TabBarView(
                        controller: _tabController,
                        children: [
                          _PatientTab(
                            phoneController: _phoneController,
                            codeController: _codeController,
                            onLogin: _patientLogin,
                          ),
                          _ProviderTab(
                            emailController: _emailController,
                            passwordController: _passwordController,
                            obscurePassword: _obscurePassword,
                            onToggleObscure: () => setState(
                              () => _obscurePassword = !_obscurePassword),
                            onLogin: _providerLogin,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Patient tab ───────────────────────────────────────────────────────────────

class _PatientTab extends ConsumerWidget {
  final TextEditingController phoneController;
  final TextEditingController codeController;
  final VoidCallback onLogin;

  const _PatientTab({
    required this.phoneController,
    required this.codeController,
    required this.onLogin,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final patientState = ref.watch(authProvider);
    final isLoading = patientState.status == AuthStatus.loading;
    final hasError  = patientState.status == AuthStatus.error;

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 20, 24, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [

          Text(AppStrings.phoneLabel,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 13, fontWeight: FontWeight.w600,
              color: AppColors.ink700)),
          const SizedBox(height: 8),
          TextFormField(
            controller: phoneController,
            keyboardType: TextInputType.phone,
            textDirection: TextDirection.ltr,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 15, color: AppColors.ink900),
            decoration: InputDecoration(
              hintText: AppStrings.phoneHint,
              hintStyle: GoogleFonts.ibmPlexSansArabic(
                color: AppColors.ink400),
              filled: true,
              fillColor: AppColors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(
                  color: AppColors.navy600, width: 1.5)),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 16, vertical: 14),
            ),
          ),

          const SizedBox(height: 20),

          Text(AppStrings.codeLabel,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 13, fontWeight: FontWeight.w600,
              color: AppColors.ink700)),
          const SizedBox(height: 8),
          TextFormField(
            controller: codeController,
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            maxLength: 4,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 22, fontWeight: FontWeight.w700,
              color: AppColors.navy600,
              letterSpacing: 12),
            decoration: InputDecoration(
              counterText: '',
              hintText: '• • • •',
              hintStyle: GoogleFonts.ibmPlexSansArabic(
                fontSize: 22, color: AppColors.ink300,
                letterSpacing: 12),
              filled: true,
              fillColor: AppColors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(
                  color: AppColors.navy600, width: 1.5)),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 16, vertical: 16),
            ),
          ),

          if (hasError) ...[
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.riskCritBg,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: AppColors.riskCritText.withOpacity(0.3)),
              ),
              child: Text(patientState.errorMessage ?? AppStrings.loginError,
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 13, color: AppColors.riskCritText),
                textAlign: TextAlign.center),
            ),
          ],

          const SizedBox(height: 24),

          SizedBox(
            height: 52,
            child: ElevatedButton(
              onPressed: isLoading ? null : onLogin,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.navy600,
                foregroundColor: AppColors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
                elevation: 0,
              ),
              child: isLoading
                ? const SizedBox(width: 20, height: 20,
                    child: CircularProgressIndicator(
                      color: Colors.white, strokeWidth: 2))
                : Text(AppStrings.loginButton,
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 16, fontWeight: FontWeight.w700)),
            ),
          ),

          const SizedBox(height: 20),

          Text(AppStrings.disclaimer,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 11, color: AppColors.ink400, height: 1.5),
            textAlign: TextAlign.center),
        ],
      ),
    );
  }
}

// ── Provider tab ──────────────────────────────────────────────────────────────

class _ProviderTab extends ConsumerWidget {
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final bool obscurePassword;
  final VoidCallback onToggleObscure;
  final VoidCallback onLogin;

  const _ProviderTab({
    required this.emailController,
    required this.passwordController,
    required this.obscurePassword,
    required this.onToggleObscure,
    required this.onLogin,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(physicianAuthProvider);

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 20, 24, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [

          Text('البريد الإلكتروني',
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 13, fontWeight: FontWeight.w600,
              color: AppColors.ink700)),
          const SizedBox(height: 8),
          TextFormField(
            controller: emailController,
            keyboardType: TextInputType.emailAddress,
            textDirection: TextDirection.ltr,
            textAlign: TextAlign.left,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 15, color: AppColors.ink900),
            decoration: InputDecoration(
              hintText: 'doctor@hospital.ye',
              hintStyle: GoogleFonts.ibmPlexSansArabic(
                color: AppColors.ink400),
              filled: true,
              fillColor: AppColors.white,
              prefixIcon: const Icon(Icons.email_outlined,
                color: AppColors.ink400),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
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
              fontSize: 13, fontWeight: FontWeight.w600,
              color: AppColors.ink700)),
          const SizedBox(height: 8),
          TextFormField(
            controller: passwordController,
            obscureText: obscurePassword,
            textDirection: TextDirection.ltr,
            onFieldSubmitted: (_) => onLogin(),
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 15, color: AppColors.ink900),
            decoration: InputDecoration(
              hintText: '••••••••',
              hintStyle: GoogleFonts.ibmPlexSansArabic(
                color: AppColors.ink400),
              filled: true,
              fillColor: AppColors.white,
              prefixIcon: const Icon(Icons.lock_outlined,
                color: AppColors.ink400),
              suffixIcon: IconButton(
                icon: Icon(obscurePassword
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
                  color: AppColors.ink400),
                onPressed: onToggleObscure,
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppColors.ink100)),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(
                  color: AppColors.navy600, width: 1.5)),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 16, vertical: 14),
            ),
          ),

          if (authState.error != null) ...[
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.riskCritBg,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: AppColors.riskCritText.withOpacity(0.3)),
              ),
              child: Text(authState.error!,
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 13, color: AppColors.riskCritText),
                textAlign: TextAlign.center),
            ),
          ],

          const SizedBox(height: 24),

          SizedBox(
            height: 52,
            child: ElevatedButton(
              onPressed: authState.isLoading ? null : onLogin,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.navy600,
                foregroundColor: AppColors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
                elevation: 0,
              ),
              child: authState.isLoading
                ? const SizedBox(width: 20, height: 20,
                    child: CircularProgressIndicator(
                      color: Colors.white, strokeWidth: 2))
                : Text('دخول',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 16, fontWeight: FontWeight.w700)),
            ),
          ),
        ],
      ),
    );
  }
}
