import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:dio/dio.dart';
import '../../../core/constants/app_colors.dart';
import '../../patient/auth/providers/auth_provider.dart';

class FeedbackScreen extends ConsumerStatefulWidget {
  final String referralId;
  const FeedbackScreen({super.key, required this.referralId});

  @override
  ConsumerState<FeedbackScreen> createState() => _FeedbackScreenState();
}

class _FeedbackScreenState extends ConsumerState<FeedbackScreen> {
  bool? _isHelpful;
  final _commentController = TextEditingController();
  bool _submitting = false;
  bool _submitted = false;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_isHelpful == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text('يرجى تحديد ما إذا كان المحتوى مفيداً',
          style: GoogleFonts.ibmPlexSansArabic()),
        backgroundColor: AppColors.riskHighText));
      return;
    }

    setState(() => _submitting = true);

    try {
      final token = ref.read(authProvider).token ?? '';
      final dio = Dio(BaseOptions(
        baseUrl: 'https://muafaplus1-production.up.railway.app/api/v1',
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));

      await dio.post(
        '/referrals/${widget.referralId}/feedback',
        data: {
          'isHelpful': _isHelpful,
          'comment': _commentController.text.trim(),
        },
      );

      setState(() { _submitted = true; _submitting = false; });

    } catch (e) {
      setState(() => _submitting = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text('تعذّر إرسال التقييم، يرجى المحاولة مجدداً',
            style: GoogleFonts.ibmPlexSansArabic()),
          backgroundColor: AppColors.riskHighText));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          elevation: 0,
          title: Text('تقييم المحتوى',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700, color: AppColors.white)),
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward, color: AppColors.white),
            onPressed: () => context.pop(),
          ),
        ),
        body: _submitted ? _buildSuccess() : _buildForm(),
      ),
    );
  }

  Widget _buildSuccess() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 80, height: 80,
              decoration: const BoxDecoration(
                color: AppColors.green50,
                shape: BoxShape.circle),
              child: const Icon(Icons.check_circle_outline,
                color: AppColors.green500, size: 44),
            ),
            const SizedBox(height: 24),
            Text('شكراً على رأيك!',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 22, fontWeight: FontWeight.w700,
                color: AppColors.ink900)),
            const SizedBox(height: 12),
            Text('تقييمك يساعدنا على تحسين جودة المحتوى الصحي.',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 14, color: AppColors.ink500, height: 1.6),
              textAlign: TextAlign.center),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity, height: 52,
              child: ElevatedButton(
                onPressed: () => context.go('/home'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.navy600,
                  foregroundColor: AppColors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                  elevation: 0),
                child: Text('العودة إلى الرئيسية',
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 16, fontWeight: FontWeight.w700)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [

          // Header card
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                begin: Alignment.topRight,
                end: Alignment.bottomLeft,
                colors: [AppColors.navy600, AppColors.navy800]),
              borderRadius: BorderRadius.circular(16)),
            child: Column(children: [
              Text('شاركنا رأيك',
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 20, fontWeight: FontWeight.w700,
                  color: AppColors.white)),
              const SizedBox(height: 6),
              Text('هل كان هذا المحتوى مفيداً لك؟',
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 14,
                  color: AppColors.white.withOpacity(0.75)),
                textAlign: TextAlign.center),
            ]),
          ),

          const SizedBox(height: 24),

          // Helpful / Not helpful buttons
          Row(
            children: [
              Expanded(
                child: GestureDetector(
                  onTap: () => setState(() => _isHelpful = true),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 200),
                    padding: const EdgeInsets.symmetric(vertical: 20),
                    decoration: BoxDecoration(
                      color: _isHelpful == true
                        ? AppColors.green500
                        : AppColors.white,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(
                        color: _isHelpful == true
                          ? AppColors.green500 : AppColors.ink100,
                        width: _isHelpful == true ? 2 : 1),
                      boxShadow: [BoxShadow(
                        color: const Color(0x0F0E1726),
                        blurRadius: 8, offset: const Offset(0, 2))],
                    ),
                    child: Column(children: [
                      Text('👍', style: const TextStyle(fontSize: 32)),
                      const SizedBox(height: 8),
                      Text('نعم، كان مفيداً',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, fontWeight: FontWeight.w700,
                          color: _isHelpful == true
                            ? AppColors.white : AppColors.ink700)),
                    ]),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: GestureDetector(
                  onTap: () => setState(() => _isHelpful = false),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 200),
                    padding: const EdgeInsets.symmetric(vertical: 20),
                    decoration: BoxDecoration(
                      color: _isHelpful == false
                        ? AppColors.riskHighText
                        : AppColors.white,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(
                        color: _isHelpful == false
                          ? AppColors.riskHighText : AppColors.ink100,
                        width: _isHelpful == false ? 2 : 1),
                      boxShadow: [BoxShadow(
                        color: const Color(0x0F0E1726),
                        blurRadius: 8, offset: const Offset(0, 2))],
                    ),
                    child: Column(children: [
                      Text('👎', style: const TextStyle(fontSize: 32)),
                      const SizedBox(height: 8),
                      Text('لا، يحتاج تحسين',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13, fontWeight: FontWeight.w700,
                          color: _isHelpful == false
                            ? AppColors.white : AppColors.ink700)),
                    ]),
                  ),
                ),
              ),
            ],
          ),

          const SizedBox(height: 20),

          // Optional comment
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.ink100)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('تعليق إضافي (اختياري)',
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 14, fontWeight: FontWeight.w600,
                    color: AppColors.ink700)),
                const SizedBox(height: 12),
                TextField(
                  controller: _commentController,
                  maxLines: 4,
                  maxLength: 300,
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 14, color: AppColors.ink900),
                  decoration: InputDecoration(
                    hintText: 'شاركنا رأيك بحرية...',
                    hintStyle: GoogleFonts.ibmPlexSansArabic(
                      color: AppColors.ink400),
                    filled: true,
                    fillColor: AppColors.ink50,
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
                    contentPadding: const EdgeInsets.all(14),
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 24),

          SizedBox(
            height: 52,
            child: ElevatedButton(
              onPressed: _submitting ? null : _submit,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.navy600,
                foregroundColor: AppColors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
                elevation: 0),
              child: _submitting
                ? const SizedBox(width: 22, height: 22,
                    child: CircularProgressIndicator(
                      color: Colors.white, strokeWidth: 2.5))
                : Text('إرسال التقييم',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 16, fontWeight: FontWeight.w700)),
            ),
          ),

          const SizedBox(height: 16),

          Text('تقييمك مجهول الهوية ويُستخدم فقط لتحسين المحتوى الطبي.',
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 11, color: AppColors.ink400, height: 1.5),
            textAlign: TextAlign.center),

          const SizedBox(height: 32),
        ],
      ),
    );
  }
}
