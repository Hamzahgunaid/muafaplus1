import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';

class EvaluationFormScreen extends ConsumerStatefulWidget {
  final String scenarioId;
  const EvaluationFormScreen({super.key, required this.scenarioId});

  @override
  ConsumerState<EvaluationFormScreen> createState() =>
      _EvaluationFormScreenState();
}

class _EvaluationFormScreenState extends ConsumerState<EvaluationFormScreen> {
  int _accuracyRating     = 0;
  int _clarityRating      = 0;
  int _relevanceRating    = 0;
  int _completenessRating = 0;

  bool? _isAppropriate;
  bool? _isCulturallySensitive;
  bool? _isArabicQuality;

  final _whatWorkedCtrl        = TextEditingController();
  final _needsImprovementCtrl  = TextEditingController();
  final _commentsCtrl          = TextEditingController();

  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _whatWorkedCtrl.dispose();
    _needsImprovementCtrl.dispose();
    _commentsCtrl.dispose();
    super.dispose();
  }

  bool get _canSubmit =>
      _accuracyRating > 0 &&
      _clarityRating > 0 &&
      _relevanceRating > 0 &&
      _completenessRating > 0 &&
      _isAppropriate != null &&
      _isCulturallySensitive != null &&
      _isArabicQuality != null;

  Future<void> _submit() async {
    if (!_canSubmit) {
      setState(() => _error = 'يرجى إكمال جميع التقييمات قبل الإرسال');
      return;
    }
    setState(() { _isLoading = true; _error = null; });
    try {
      final auth = ref.read(physicianAuthProvider);
      final dio = Dio();
      await dio.post(
        'https://muafaplus1-production.up.railway.app/api/v1/test-scenarios/${widget.scenarioId}/evaluation',
        data: {
          'accuracyRating':       _accuracyRating,
          'clarityRating':        _clarityRating,
          'relevanceRating':      _relevanceRating,
          'completenessRating':   _completenessRating,
          'isAppropriate':        _isAppropriate,
          'isCulturallySensitive': _isCulturallySensitive,
          'isArabicQuality':      _isArabicQuality,
          'whatWorked':           _whatWorkedCtrl.text.trim(),
          'needsImprovement':     _needsImprovementCtrl.text.trim(),
          'comments':             _commentsCtrl.text.trim(),
        },
        options: Options(
            headers: {'Authorization': 'Bearer ${auth.token}'}),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('تم إرسال التقييم بنجاح ✅'),
            backgroundColor: Color(0xFF197540),
          ),
        );
        context.pop();
      }
    } catch (e) {
      setState(() {
        _error = 'حدث خطأ أثناء إرسال التقييم';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF1E3A72),
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text('تقييم جودة المحتوى',
              style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Info banner
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFEEF1F7),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.info_outline,
                        color: Color(0xFF1E3A72), size: 16),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'تقييمك يساعد على تحسين جودة المحتوى الصحي المولَّد بالذكاء الاصطناعي',
                        style: TextStyle(fontSize: 12, color: Color(0xFF2D3748)),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // ── Star ratings ──────────────────────────────────────────
              const _SectionHeader(title: 'تقييم الجودة', subtitle: '١-٥ نجوم'),
              const SizedBox(height: 12),
              _StarRatingRow(
                label: 'الدقة الطبية',
                sublabel: 'صحيح طبياً للتشخيص والملف الصحي',
                rating: _accuracyRating,
                onChanged: (v) => setState(() => _accuracyRating = v),
              ),
              _StarRatingRow(
                label: 'الوضوح والقابلية للقراءة',
                sublabel: 'مناسب لمستوى إلمام المريض',
                rating: _clarityRating,
                onChanged: (v) => setState(() => _clarityRating = v),
              ),
              _StarRatingRow(
                label: 'الصلة بالحالة',
                sublabel: 'يطابق الاحتياجات السريرية',
                rating: _relevanceRating,
                onChanged: (v) => setState(() => _relevanceRating = v),
              ),
              _StarRatingRow(
                label: 'الاكتمال',
                sublabel: 'يغطي جميع الجوانب المهمة',
                rating: _completenessRating,
                onChanged: (v) => setState(() => _completenessRating = v),
              ),
              const SizedBox(height: 20),

              // ── Yes/No checks ─────────────────────────────────────────
              const _SectionHeader(title: 'الملاءمة', subtitle: 'نعم / لا'),
              const SizedBox(height: 12),
              _YesNoRow(
                label: 'مناسب لهذا الملف الصحي',
                value: _isAppropriate,
                onChanged: (v) => setState(() => _isAppropriate = v),
              ),
              _YesNoRow(
                label: 'مناسب ثقافياً للسياق اليمني',
                value: _isCulturallySensitive,
                onChanged: (v) => setState(() => _isCulturallySensitive = v),
              ),
              _YesNoRow(
                label: 'جودة اللغة العربية مناسبة',
                value: _isArabicQuality,
                onChanged: (v) => setState(() => _isArabicQuality = v),
              ),
              const SizedBox(height: 20),

              // ── Free text ─────────────────────────────────────────────
              const _SectionHeader(
                  title: 'ملاحظات إضافية', subtitle: 'اختياري'),
              const SizedBox(height: 12),
              _TextAreaField(
                label: 'ما الذي نجح في هذا المحتوى؟',
                controller: _whatWorkedCtrl,
                hint: 'نقاط القوة في المحتوى المولَّد...',
              ),
              const SizedBox(height: 12),
              _TextAreaField(
                label: 'ما الذي يحتاج إلى تحسين؟',
                controller: _needsImprovementCtrl,
                hint: 'اقتراحات للتحسين...',
              ),
              const SizedBox(height: 12),
              _TextAreaField(
                label: 'تعليقات إضافية',
                controller: _commentsCtrl,
                hint: 'أي ملاحظات أخرى...',
              ),
              const SizedBox(height: 20),

              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(_error!,
                      style: const TextStyle(
                          color: Color(0xFFD64545), fontSize: 13),
                      textAlign: TextAlign.center),
                ),

              ElevatedButton(
                onPressed: _isLoading ? null : _submit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF1E3A72),
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                  disabledBackgroundColor:
                      const Color(0xFF1E3A72).withOpacity(0.5),
                ),
                child: _isLoading
                    ? const SizedBox(
                        height: 20, width: 20,
                        child: CircularProgressIndicator(
                            color: Colors.white, strokeWidth: 2))
                    : const Text('إرسال التقييم',
                        style: TextStyle(
                            color: Colors.white,
                            fontSize: 15,
                            fontWeight: FontWeight.w600)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ─── Helper widgets ───────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  final String title;
  final String subtitle;
  const _SectionHeader({required this.title, required this.subtitle});

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Text(title,
              style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF0E1726))),
          const SizedBox(width: 8),
          Text(subtitle,
              style: const TextStyle(fontSize: 11, color: Color(0xFF8A93A6))),
        ],
      );
}

class _StarRatingRow extends StatelessWidget {
  final String label;
  final String sublabel;
  final int rating;
  final ValueChanged<int> onChanged;

  const _StarRatingRow({
    required this.label,
    required this.sublabel,
    required this.rating,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) => Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label,
                style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: Color(0xFF0E1726))),
            const SizedBox(height: 2),
            Text(sublabel,
                style: const TextStyle(
                    fontSize: 11, color: Color(0xFF8A93A6))),
            const SizedBox(height: 10),
            Row(
              children: List.generate(5, (i) {
                final filled = i < rating;
                return GestureDetector(
                  onTap: () => onChanged(i + 1),
                  child: Padding(
                    padding: const EdgeInsets.only(left: 4),
                    child: Icon(
                      filled ? Icons.star : Icons.star_outline,
                      color: filled
                          ? const Color(0xFFF39A4F)
                          : const Color(0xFFDFE2EA),
                      size: 28,
                    ),
                  ),
                );
              }),
            ),
          ],
        ),
      );
}

class _YesNoRow extends StatelessWidget {
  final String label;
  final bool? value;
  final ValueChanged<bool> onChanged;

  const _YesNoRow({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) => Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(label,
                  style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF0E1726))),
            ),
            const SizedBox(width: 12),
            _ToggleButton(
              label: 'نعم',
              selected: value == true,
              selectedColor: const Color(0xFF197540),
              onTap: () => onChanged(true),
            ),
            const SizedBox(width: 8),
            _ToggleButton(
              label: 'لا',
              selected: value == false,
              selectedColor: const Color(0xFFD64545),
              onTap: () => onChanged(false),
            ),
          ],
        ),
      );
}

class _ToggleButton extends StatelessWidget {
  final String label;
  final bool selected;
  final Color selectedColor;
  final VoidCallback onTap;

  const _ToggleButton({
    required this.label,
    required this.selected,
    required this.selectedColor,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) => GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: selected ? selectedColor : const Color(0xFFEEF0F5),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(label,
              style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: selected ? Colors.white : const Color(0xFF5A6478))),
        ),
      );
}

class _TextAreaField extends StatelessWidget {
  final String label;
  final TextEditingController controller;
  final String hint;

  const _TextAreaField({
    required this.label,
    required this.controller,
    required this.hint,
  });

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF2D3748))),
          const SizedBox(height: 6),
          TextField(
            controller: controller,
            textDirection: TextDirection.rtl,
            maxLines: 3,
            style: const TextStyle(fontSize: 13, color: Color(0xFF0E1726)),
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: const TextStyle(
                  color: Color(0xFFB7BDCB), fontSize: 12),
              filled: true,
              fillColor: Colors.white,
              contentPadding: const EdgeInsets.all(14),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: Color(0xFFDFE2EA)),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: Color(0xFFDFE2EA)),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(
                    color: Color(0xFF1E3A72), width: 1.5),
              ),
            ),
          ),
        ],
      );
}
