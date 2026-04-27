import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_colors.dart';
import '../../auth/providers/auth_provider.dart';

class FeedbackScreen extends ConsumerStatefulWidget {
  final String referralId;
  const FeedbackScreen({super.key, required this.referralId});

  @override
  ConsumerState<FeedbackScreen> createState() => _FeedbackScreenState();
}

class _FeedbackScreenState extends ConsumerState<FeedbackScreen> {
  int _starRating = 0;
  final Set<String> _selectedTags = {};
  final _commentController = TextEditingController();
  bool _submitting = false;
  bool _submitted = false;

  final List<String> _tags = [
    'المحتوى مفيد',
    'سهل الفهم',
    'طول مناسب',
    'أريد المزيد',
    'محتوى دقيق',
    'يحتاج تحسيناً',
  ];

  String get _emoji {
    if (_starRating <= 1) return '😐';
    if (_starRating <= 3) return '🙂';
    return '😊';
  }

  String get _ratingLabel {
    switch (_starRating) {
      case 1: return 'ضعيف';
      case 2: return 'مقبول';
      case 3: return 'جيد';
      case 4: return 'جيد جداً';
      case 5: return 'ممتاز';
      default: return 'اختر تقييمك';
    }
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submitFeedback() async {
    if (_starRating == 0) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text('يرجى اختيار تقييم',
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
          'isHelpful': _starRating >= 3,
          'rating': _starRating,
          'tags': _selectedTags.toList(),
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
            onPressed: () => Navigator.of(context).pop(),
          ),
        ),
        body: _submitted ? _buildSuccessState() : _buildForm(),
      ),
    );
  }

  Widget _buildSuccessState() {
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
            Text('شكراً على تقييمك!',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 22, fontWeight: FontWeight.w700,
                color: AppColors.ink900)),
            const SizedBox(height: 12),
            Text(
              'رأيك يساعدنا على تحسين جودة المحتوى الصحي لجميع المرضى.',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 14, color: AppColors.ink500, height: 1.6),
              textAlign: TextAlign.center),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: () => Navigator.of(context).pop(),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.navy600,
                  foregroundColor: AppColors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                  elevation: 0),
                child: Text('العودة',
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
              Text('تقييمك يساعدنا على تطوير المحتوى الصحي',
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 13,
                  color: AppColors.white.withOpacity(0.7)),
                textAlign: TextAlign.center),
            ]),
          ),

          const SizedBox(height: 24),

          // Star rating
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.ink100)),
            child: Column(children: [
              AnimatedSwitcher(
                duration: const Duration(milliseconds: 200),
                child: Text(
                  _starRating > 0 ? _emoji : '⭐',
                  key: ValueKey(_starRating),
                  style: const TextStyle(fontSize: 44)),
              ),
              const SizedBox(height: 8),
              Text(_ratingLabel,
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 15, fontWeight: FontWeight.w600,
                  color: _starRating > 0
                    ? AppColors.navy600 : AppColors.ink400)),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (i) {
                  final filled = i < _starRating;
                  return GestureDetector(
                    onTap: () => setState(() => _starRating = i + 1),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 6),
                      child: Icon(
                        filled ? Icons.star_rounded : Icons.star_outline_rounded,
                        size: 40,
                        color: filled
                          ? const Color(0xFFF59E0B)
                          : AppColors.ink100),
                    ),
                  );
                }),
              ),
            ]),
          ),

          const SizedBox(height: 16),

          // Tag chips
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.ink100)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('ما الذي أعجبك؟',
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 14, fontWeight: FontWeight.w600,
                    color: AppColors.ink700)),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8, runSpacing: 8,
                  children: _tags.map((tag) {
                    final selected = _selectedTags.contains(tag);
                    return GestureDetector(
                      onTap: () => setState(() {
                        if (selected) {
                          _selectedTags.remove(tag);
                        } else {
                          _selectedTags.add(tag);
                        }
                      }),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 8),
                        decoration: BoxDecoration(
                          color: selected
                            ? AppColors.navy600
                            : AppColors.ink50,
                          borderRadius: BorderRadius.circular(999),
                          border: Border.all(
                            color: selected
                              ? AppColors.navy600 : AppColors.ink100)),
                        child: Text(tag,
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 13,
                            fontWeight: FontWeight.w500,
                            color: selected
                              ? AppColors.white : AppColors.ink700)),
                      ),
                    );
                  }).toList(),
                ),
              ],
            ),
          ),

          const SizedBox(height: 16),

          // Comment field
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
                  maxLength: 200,
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
                    contentPadding: const EdgeInsets.all(14),
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 24),

          // Submit button
          SizedBox(
            height: 52,
            child: ElevatedButton(
              onPressed: _submitting ? null : _submitFeedback,
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

          Text(
            'تقييمك مجهول الهوية ويُستخدم فقط لتحسين جودة المحتوى الطبي.',
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 11, color: AppColors.ink400, height: 1.5),
            textAlign: TextAlign.center),

          const SizedBox(height: 32),
        ],
      ),
    );
  }
}
