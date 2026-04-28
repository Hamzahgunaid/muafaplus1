import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';

class InvitationCodeScreen extends ConsumerStatefulWidget {
  const InvitationCodeScreen({super.key});

  @override
  ConsumerState<InvitationCodeScreen> createState() =>
    _InvitationCodeScreenState();
}

class _InvitationCodeScreenState
    extends ConsumerState<InvitationCodeScreen> {
  final _codeController = TextEditingController();
  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    final code = _codeController.text.trim().toUpperCase();
    if (code.isEmpty) {
      setState(() => _error = 'يرجى إدخال رمز الدعوة');
      return;
    }
    setState(() { _isLoading = true; _error = null; });
    await Future.delayed(const Duration(milliseconds: 600));
    setState(() => _isLoading = false);
    // Navigate to patient login for now — invitation flow TBD in Phase 4B
    if (mounted) context.go('/login');
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
              flex: 40,
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
                        const SizedBox(height: 24),
                        Text('رمز الدعوة',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 26,
                            fontWeight: FontWeight.w700,
                            color: AppColors.white)),
                        const SizedBox(height: 10),
                        Text(
                          'أدخل رمز الدعوة الذي زودك به طبيبك للوصول إلى التطبيق',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 14, height: 1.6,
                            color: AppColors.white.withOpacity(0.65))),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            Expanded(
              flex: 60,
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
                      Text('رمز الدعوة',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink700)),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _codeController,
                        keyboardType: TextInputType.text,
                        textCapitalization: TextCapitalization.characters,
                        inputFormatters: [
                          FilteringTextInputFormatter.allow(
                            RegExp(r'[A-Za-z0-9\-]')),
                        ],
                        onChanged: (_) => setState(() => _error = null),
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                          color: AppColors.ink900,
                          letterSpacing: 2),
                        textDirection: TextDirection.ltr,
                        textAlign: TextAlign.center,
                        decoration: InputDecoration(
                          hintText: 'PH-XXXXXX',
                          hintStyle: GoogleFonts.ibmPlexSansArabic(
                            color: AppColors.ink400,
                            letterSpacing: 1),
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
                            horizontal: 16, vertical: 18),
                        ),
                      ),
                      if (_error != null) ...[
                        const SizedBox(height: 12),
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.riskCritBg,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                              color: AppColors.riskCritText.withOpacity(0.3))),
                          child: Text(_error!,
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 13, color: AppColors.riskCritText),
                            textAlign: TextAlign.center),
                        ),
                      ],
                      const SizedBox(height: 32),
                      SizedBox(
                        height: 52,
                        child: ElevatedButton(
                          onPressed: _isLoading ? null : _verify,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.navy600,
                            foregroundColor: AppColors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12)),
                            elevation: 0),
                          child: _isLoading
                            ? const SizedBox(width: 20, height: 20,
                                child: CircularProgressIndicator(
                                  color: Colors.white, strokeWidth: 2))
                            : Text('تحقق من الرمز',
                                style: GoogleFonts.ibmPlexSansArabic(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700)),
                        ),
                      ),
                      const SizedBox(height: 20),
                      GestureDetector(
                        onTap: () => context.go('/login'),
                        child: Text('لدي رقم هاتف ورمز دخول — تسجيل الدخول',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 13,
                            color: AppColors.navy600,
                            fontWeight: FontWeight.w600),
                          textAlign: TextAlign.center),
                      ),
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
