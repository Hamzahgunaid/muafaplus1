import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';

class CreateTestScenarioScreen extends ConsumerStatefulWidget {
  const CreateTestScenarioScreen({super.key});

  @override
  ConsumerState<CreateTestScenarioScreen> createState() =>
      _CreateTestScenarioScreenState();
}

class _CreateTestScenarioScreenState
    extends ConsumerState<CreateTestScenarioScreen> {
  final _diagnosisCtrl      = TextEditingController();
  final _ageCtrl            = TextEditingController();
  final _comorbiditiesCtrl  = TextEditingController();
  final _medicationsCtrl    = TextEditingController();
  final _allergiesCtrl      = TextEditingController();
  final _restrictionsCtrl   = TextEditingController();

  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _diagnosisCtrl.dispose();
    _ageCtrl.dispose();
    _comorbiditiesCtrl.dispose();
    _medicationsCtrl.dispose();
    _allergiesCtrl.dispose();
    _restrictionsCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_diagnosisCtrl.text.trim().isEmpty ||
        _ageCtrl.text.trim().isEmpty) {
      setState(() => _error = 'يرجى إدخال التشخيص والعمر');
      return;
    }

    setState(() { _isLoading = true; _error = null; });

    try {
      final auth = ref.read(physicianAuthProvider);
      final dio = Dio();
      final resp = await dio.post(
        'https://muafaplus1-production.up.railway.app/api/v1/test-scenarios',
        data: {
          'primaryDiagnosis':   _diagnosisCtrl.text.trim(),
          'ageGroup':           _ageCtrl.text.trim(),
          'comorbidities':      _comorbiditiesCtrl.text.trim(),
          'currentMedications': _medicationsCtrl.text.trim(),
          'allergies':          _allergiesCtrl.text.trim(),
          'medicalRestrictions': _restrictionsCtrl.text.trim(),
        },
        options: Options(
            headers: {'Authorization': 'Bearer ${auth.token}'}),
      );

      final scenarioId = resp.data['data']?['scenarioId'] ??
          resp.data['data']?['id'] ?? '';

      if (mounted) {
        context.pushReplacement('/provider/test-scenarios/$scenarioId');
      }
    } catch (e) {
      setState(() {
        _error = 'حدث خطأ أثناء إنشاء السيناريو';
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
          title: const Text('سيناريو اختبار جديد',
              style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Info card
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFEEF1F7),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.info_outline,
                        color: Color(0xFF1E3A72), size: 18),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'سيناريو الاختبار لا يرسل رسائل للمرضى ويُستخدم لتقييم جودة المحتوى',
                        style: TextStyle(
                            fontSize: 12, color: Color(0xFF2D3748)),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // Form card
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFEEF0F5)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('بيانات المريض الافتراضي',
                        style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF0E1726))),
                    const SizedBox(height: 16),
                    _FormField(
                      label: 'التشخيص الرئيسي *',
                      controller: _diagnosisCtrl,
                      hint: 'مثال: سكري من النوع الثاني',
                    ),
                    _FormField(
                      label: 'الفئة العمرية *',
                      controller: _ageCtrl,
                      hint: 'مثال: 40-50 سنة',
                    ),
                    _FormField(
                      label: 'الأمراض المصاحبة',
                      controller: _comorbiditiesCtrl,
                      hint: 'مثال: ارتفاع ضغط الدم، أمراض القلب',
                    ),
                    _FormField(
                      label: 'الأدوية الحالية',
                      controller: _medicationsCtrl,
                      hint: 'مثال: ميتفورمين 500mg مرتين يومياً',
                    ),
                    _FormField(
                      label: 'الحساسية',
                      controller: _allergiesCtrl,
                      hint: 'مثال: حساسية من البنسلين',
                    ),
                    _FormField(
                      label: 'القيود الطبية',
                      controller: _restrictionsCtrl,
                      hint: 'مثال: فشل كلوي، حمل',
                      isLast: true,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

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
                    : const Text('إنشاء السيناريو وتوليد المحتوى',
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

class _FormField extends StatelessWidget {
  final String label;
  final TextEditingController controller;
  final String hint;
  final bool isLast;

  const _FormField({
    required this.label,
    required this.controller,
    required this.hint,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) => Padding(
        padding: EdgeInsets.only(bottom: isLast ? 0 : 16),
        child: Column(
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
              style: const TextStyle(fontSize: 14, color: Color(0xFF0E1726)),
              decoration: InputDecoration(
                hintText: hint,
                hintStyle: const TextStyle(
                    color: Color(0xFFB7BDCB), fontSize: 13),
                filled: true,
                fillColor: const Color(0xFFF6F7FB),
                contentPadding: const EdgeInsets.symmetric(
                    horizontal: 14, vertical: 12),
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
        ),
      );
}
