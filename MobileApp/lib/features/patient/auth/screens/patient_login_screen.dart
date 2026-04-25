import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/constants/app_strings.dart';
import '../providers/auth_provider.dart';

class PatientLoginScreen extends ConsumerStatefulWidget {
  const PatientLoginScreen({super.key});
  @override
  ConsumerState<PatientLoginScreen> createState() => _PatientLoginScreenState();
}

class _PatientLoginScreenState extends ConsumerState<PatientLoginScreen> {
  final _phoneController = TextEditingController();
  final _otpControllers  = List.generate(4, (_) => TextEditingController());
  final _otpFocusNodes   = List.generate(4, (_) => FocusNode());

  @override
  void dispose() {
    _phoneController.dispose();
    for (final c in _otpControllers) c.dispose();
    for (final f in _otpFocusNodes)  f.dispose();
    super.dispose();
  }

  String get _fullCode => _otpControllers.map((c) => c.text).join();

  Future<void> _handleLogin() async {
    final phone = _phoneController.text.trim();
    final code  = _fullCode;
    if (phone.isEmpty || code.length != 4) return;
    final success = await ref.read(authProvider.notifier).login(phone, code);
    if (success && mounted) context.go('/home');
  }

  void _onOtpChanged(String value, int index) {
    if (value.length == 1 && index < 3) {
      _otpFocusNodes[index + 1].requestFocus();
    }
    if (value.isEmpty && index > 0) {
      _otpFocusNodes[index - 1].requestFocus();
    }
    if (_fullCode.length == 4) _handleLogin();
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);
    final isLoading = authState.status == AuthStatus.loading;
    final hasError  = authState.status == AuthStatus.error;

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.navy800,
        body: Column(
          children: [

            // ── Navy hero — top 45% ──
            Expanded(
              flex: 45,
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
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text('معافى+',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 20, fontWeight: FontWeight.w700,
                              color: AppColors.navy600)),
                        ),
                        const SizedBox(height: 24),
                        Text(AppStrings.loginTitle,
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 26, fontWeight: FontWeight.w700,
                            color: AppColors.white, height: 1.3)),
                        const SizedBox(height: 10),
                        Text(AppStrings.loginSubtitle,
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 14, height: 1.6,
                            color: AppColors.white.withOpacity(0.65))),
                      ],
                    ),
                  ),
                ),
              ),
            ),

            // ── White sheet — bottom 55% ──
            Expanded(
              flex: 55,
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: AppColors.ink50,
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(24)),
                ),
                child: SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(28, 32, 28, 28),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [

                      // Phone field
                      Text(AppStrings.phoneLabel,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, fontWeight: FontWeight.w600,
                          color: AppColors.ink700)),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _phoneController,
                        keyboardType: TextInputType.phone,
                        textDirection: TextDirection.ltr,
                        onChanged: (_) => setState(() {}),
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

                      const SizedBox(height: 24),

                      // OTP label
                      Text(AppStrings.codeLabel,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, fontWeight: FontWeight.w600,
                          color: AppColors.ink700)),
                      const SizedBox(height: 8),

                      // 4-box OTP row
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: List.generate(4, (i) => _OtpBox(
                          controller: _otpControllers[i],
                          focusNode: _otpFocusNodes[i],
                          onChanged: (v) => _onOtpChanged(v, i),
                        )),
                      ),

                      // Error banner
                      if (hasError) ...[
                        const SizedBox(height: 16),
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.riskCritBg,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                              color: AppColors.riskCritText.withOpacity(0.3)),
                          ),
                          child: Text(AppStrings.loginError,
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 13, color: AppColors.riskCritText),
                            textAlign: TextAlign.center),
                        ),
                      ],

                      const SizedBox(height: 32),

                      // Login button
                      SizedBox(
                        height: 52,
                        child: ElevatedButton(
                          onPressed: isLoading ? null : _handleLogin,
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
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700)),
                        ),
                      ),

                      const SizedBox(height: 20),

                      Text(AppStrings.disclaimer,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 11, color: AppColors.ink400, height: 1.5),
                        textAlign: TextAlign.center),
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

class _OtpBox extends StatelessWidget {
  final TextEditingController controller;
  final FocusNode focusNode;
  final ValueChanged<String> onChanged;

  const _OtpBox({
    required this.controller,
    required this.focusNode,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 64, height: 64,
      child: TextFormField(
        controller: controller,
        focusNode: focusNode,
        keyboardType: TextInputType.number,
        textAlign: TextAlign.center,
        maxLength: 1,
        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
        onChanged: onChanged,
        style: GoogleFonts.ibmPlexSansArabic(
          fontSize: 24, fontWeight: FontWeight.w700,
          color: AppColors.navy600),
        decoration: InputDecoration(
          counterText: '',
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
              color: AppColors.navy600, width: 2)),
          contentPadding: EdgeInsets.zero,
        ),
      ),
    );
  }
}
