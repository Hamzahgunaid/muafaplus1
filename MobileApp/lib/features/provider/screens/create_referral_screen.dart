import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/physician_auth_provider.dart';
import '../providers/referrals_provider.dart';
import '../../../core/network/dio_client.dart';

class CreateReferralScreen extends ConsumerStatefulWidget {
  const CreateReferralScreen({super.key});

  @override
  ConsumerState<CreateReferralScreen> createState() =>
      _CreateReferralScreenState();
}

class _CreateReferralScreenState extends ConsumerState<CreateReferralScreen> {
  final _phoneController          = TextEditingController();
  final _diagnosisController      = TextEditingController();
  final _comorbiditiesController  = TextEditingController();
  final _medicationsController    = TextEditingController();
  final _allergiesController      = TextEditingController();
  final _restrictionsController   = TextEditingController();

  String _ageGroup    = 'Adult';
  bool   _whatsApp    = true;
  int    _delayHours  = 2;
  bool   _submitting  = false;
  String? _error;
  Map<String, dynamic>? _success;

  static const _ageGroups = [
    {'value': 'Child',      'label': 'طفل (0-12 سنة)'},
    {'value': 'Adolescent', 'label': 'مراهق (13-17 سنة)'},
    {'value': 'Adult',      'label': 'بالغ (18-65 سنة)'},
    {'value': 'Elderly',    'label': 'كبير السن (65+)'},
  ];

  static const _delays = [
    {'value': 0, 'label': 'فوري (الآن)'},
    {'value': 2, 'label': 'ساعتان (موصى به)'},
    {'value': 4, 'label': '4 ساعات'},
    {'value': 8, 'label': '8 ساعات'},
  ];

  @override
  void dispose() {
    _phoneController.dispose();
    _diagnosisController.dispose();
    _comorbiditiesController.dispose();
    _medicationsController.dispose();
    _allergiesController.dispose();
    _restrictionsController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_phoneController.text.trim().isEmpty ||
        _diagnosisController.text.trim().isEmpty) {
      setState(() => _error = 'يرجى تعبئة رقم الهاتف والتشخيص الرئيسي');
      return;
    }
    setState(() { _submitting = true; _error = null; });

    try {
      final auth = ref.read(physicianAuthProvider);
      final dio  = DioClient.instanceWithToken(auth.token!);
      final res  = await dio.post('/referrals', data: {
        'patientPhone':        _phoneController.text.trim(),
        'primaryDiagnosis':    _diagnosisController.text.trim(),
        'ageGroup':            _ageGroup,
        if (_comorbiditiesController.text.trim().isNotEmpty)
          'comorbidities':     _comorbiditiesController.text.trim(),
        if (_medicationsController.text.trim().isNotEmpty)
          'currentMedications': _medicationsController.text.trim(),
        if (_allergiesController.text.trim().isNotEmpty)
          'allergies':         _allergiesController.text.trim(),
        if (_restrictionsController.text.trim().isNotEmpty)
          'medicalRestrictions': _restrictionsController.text.trim(),
        'notificationDelayHours': _delayHours,
        'whatsAppDelivery':    _whatsApp,
      });

      // Invalidate the referrals list so dashboard refreshes
      ref.invalidate(recentReferralsProvider);

      setState(() {
        _success   = res.data['data'] as Map<String, dynamic>?;
        _submitting = false;
      });
    } catch (e) {
      setState(() {
        _error      = 'حدث خطأ أثناء إنشاء الإحالة. يرجى المحاولة مرة أخرى.';
        _submitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_success != null) return _buildSuccess();

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF283481),
          title: const Text('إحالة مريض جديد',
            style: TextStyle(color: Colors.white)),
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward, color: Colors.white),
            onPressed: () => context.pop(),
          ),
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [

              _Section(title: 'معلومات المريض', children: [
                _Field(
                  label: 'رقم الهاتف *',
                  child: TextField(
                    controller: _phoneController,
                    textDirection: TextDirection.ltr,
                    keyboardType: TextInputType.phone,
                    decoration: _dec('مثال: +967700000001'),
                  )),
                _Field(
                  label: 'إرسال عبر واتساب',
                  child: SwitchListTile(
                    value: _whatsApp,
                    onChanged: (v) => setState(() => _whatsApp = v),
                    title: Text(
                      _whatsApp
                        ? 'سيتم إرسال المحتوى عبر واتساب'
                        : 'بدون إرسال واتساب'),
                    activeColor: const Color(0xFF283481),
                    contentPadding: EdgeInsets.zero,
                  )),
              ]),

              const SizedBox(height: 12),

              _Section(title: 'المعلومات الطبية', children: [
                _Field(
                  label: 'الفئة العمرية *',
                  child: DropdownButtonFormField<String>(
                    value: _ageGroup,
                    decoration: _dec(''),
                    items: _ageGroups.map((g) => DropdownMenuItem(
                      value: g['value'] as String,
                      child: Text(g['label'] as String))).toList(),
                    onChanged: (v) => setState(() => _ageGroup = v!),
                  )),
                _Field(
                  label: 'التشخيص الرئيسي *',
                  child: TextField(
                    controller: _diagnosisController,
                    maxLines: 3,
                    decoration: _dec('مثال: داء السكري من النوع الثاني'),
                  )),
                _Field(
                  label: 'الأمراض المصاحبة',
                  child: TextField(
                    controller: _comorbiditiesController,
                    maxLines: 2,
                    decoration: _dec('مثال: ارتفاع ضغط الدم، السمنة'),
                  )),
                _Field(
                  label: 'الأدوية الحالية',
                  child: TextField(
                    controller: _medicationsController,
                    maxLines: 2,
                    decoration: _dec('مثال: ميتفورمين 500 ملغ'),
                  )),
                _Field(
                  label: 'الحساسية الدوائية',
                  child: TextField(
                    controller: _allergiesController,
                    decoration: _dec('مثال: البنسلين'),
                  )),
                _Field(
                  label: 'القيود الطبية',
                  child: TextField(
                    controller: _restrictionsController,
                    maxLines: 2,
                    decoration: _dec('مثال: الحمل، الفشل الكلوي'),
                  )),
              ]),

              const SizedBox(height: 12),

              _Section(title: 'إعدادات التوصيل', children: [
                _Field(
                  label: 'تأخير إرسال الواتساب',
                  child: DropdownButtonFormField<int>(
                    value: _delayHours,
                    decoration: _dec(''),
                    items: _delays.map((d) => DropdownMenuItem(
                      value: d['value'] as int,
                      child: Text(d['label'] as String))).toList(),
                    onChanged: (v) => setState(() => _delayHours = v!),
                  )),
              ]),

              if (_error != null) ...[
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFBE5E5),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFFF5B8B8))),
                  child: Text(_error!,
                    style: const TextStyle(color: Color(0xFFD64545)),
                    textAlign: TextAlign.right),
                ),
              ],

              const SizedBox(height: 16),

              ElevatedButton(
                onPressed: _submitting ? null : _submit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF283481),
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                ),
                child: _submitting
                  ? const SizedBox(height: 20, width: 20,
                      child: CircularProgressIndicator(
                        color: Colors.white, strokeWidth: 2))
                  : const Text('إنشاء الإحالة وتوليد المحتوى',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: Colors.white)),
              ),

              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSuccess() {
    const riskLabels = {
      'LOW': 'منخفض', 'MODERATE': 'متوسط',
      'HIGH': 'مرتفع', 'CRITICAL': 'حرج',
    };
    const riskColors = {
      'LOW':      Color(0xFF21A740),
      'MODERATE': Color(0xFF355BA7),
      'HIGH':     Color(0xFFDC6B20),
      'CRITICAL': Color(0xFFC0392B),
    };
    final risk  = _success!['riskLevel'] as String? ?? 'LOW';
    final color = riskColors[risk] ?? const Color(0xFF283481);
    final label = riskLabels[risk] ?? risk;

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 80, height: 80,
                  decoration: BoxDecoration(
                    color: const Color(0xFFE6F4EC),
                    borderRadius: BorderRadius.circular(40)),
                  child: const Icon(Icons.check,
                    color: Color(0xFF21A740), size: 44),
                ),
                const SizedBox(height: 20),
                const Text('تمت الإحالة بنجاح!',
                  style: TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF0E1726))),
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14, vertical: 6),
                  decoration: BoxDecoration(
                    color: color.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: color)),
                  child: Text('مستوى الخطر: $label',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      color: color)),
                ),
                const SizedBox(height: 12),
                Text(
                  'يتم الآن توليد المحتوى الصحي المخصص للمريض.\n'
                  'سيصله عبر واتساب خلال الفترة المحددة.',
                  style: const TextStyle(
                    fontSize: 14,
                    color: Color(0xFF5A6478),
                    height: 1.6),
                  textAlign: TextAlign.center),
                const SizedBox(height: 32),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: () => context.go('/provider/dashboard'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF283481),
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                    ),
                    child: const Text('العودة للوحة التحكم',
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold)),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  InputDecoration _dec(String hint) => InputDecoration(
    hintText: hint,
    hintStyle: const TextStyle(color: Color(0xFF8A93A6)),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: Color(0xFF283481), width: 2)),
    filled: true,
    fillColor: Colors.white,
    contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
  );
}

// ── Reusable layout widgets ───────────────────────────────────────────────────

class _Section extends StatelessWidget {
  final String title;
  final List<Widget> children;
  const _Section({required this.title, required this.children});

  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: const Color(0xFFEEF0F5))),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
          child: Text(title,
            style: const TextStyle(
              fontWeight: FontWeight.bold,
              color: Color(0xFF0E1726)),
            textAlign: TextAlign.right)),
        const Divider(height: 1, color: Color(0xFFEEF0F5)),
        Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: children
              .map((c) => Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: c))
              .toList(),
          )),
      ],
    ),
  );
}

class _Field extends StatelessWidget {
  final String label;
  final Widget child;
  const _Field({required this.label, required this.child});

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.end,
    children: [
      Text(label,
        style: const TextStyle(
          fontSize: 13,
          fontWeight: FontWeight.w600,
          color: Color(0xFF2D3748))),
      const SizedBox(height: 6),
      child,
    ],
  );
}
