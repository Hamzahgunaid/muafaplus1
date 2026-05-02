import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';

// ─── Model ────────────────────────────────────────────────────────────────────

class TestScenario {
  final String scenarioId;
  final String primaryDiagnosis;
  final String riskLevel;
  final String status;
  final String createdAt;
  final bool hasEvaluation;

  TestScenario({
    required this.scenarioId,
    required this.primaryDiagnosis,
    required this.riskLevel,
    required this.status,
    required this.createdAt,
    required this.hasEvaluation,
  });

  factory TestScenario.fromJson(Map<String, dynamic> j) {
    Map<String, dynamic> patientData = {};
    try {
      final raw = j['patientDataJson'] as String? ?? '';
      if (raw.isNotEmpty) {
        patientData = json.decode(raw) as Map<String, dynamic>;
      }
    } catch (_) {}

    return TestScenario(
      scenarioId: j['scenarioId']?.toString() ?? '',
      primaryDiagnosis: patientData['primaryDiagnosis'] as String? ?? '',
      riskLevel: j['riskLevel'] as String? ?? 'LOW',
      status: j['status'] as String? ?? '',
      createdAt: j['createdAt'] as String? ?? '',
      hasEvaluation: j['evaluation'] != null,
    );
  }

  static String _formatDate(String iso) {
    if (iso.isEmpty) return '';
    try {
      final dt = DateTime.parse(iso).toLocal();
      final now = DateTime.now();
      final diff = now.difference(dt);
      if (diff.inDays > 7) return '${dt.day}/${dt.month}/${dt.year}';
      if (diff.inDays >= 1) return 'منذ ${diff.inDays} ${diff.inDays == 1 ? 'يوم' : 'أيام'}';
      if (diff.inHours >= 1) return 'منذ ${diff.inHours} ${diff.inHours == 1 ? 'ساعة' : 'ساعات'}';
      if (diff.inMinutes >= 1) return 'منذ ${diff.inMinutes} دقيقة';
      return 'الآن';
    } catch (_) {
      return iso;
    }
  }

  String get formattedDate => _formatDate(createdAt);
}

// ─── Provider ─────────────────────────────────────────────────────────────────

final testScenariosProvider = FutureProvider<List<TestScenario>>((ref) async {
  final auth = ref.watch(physicianAuthProvider);
  if (auth.token == null) return [];
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/test-scenarios',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  final data = resp.data['data'] as List? ?? [];
  return data
      .map((e) => TestScenario.fromJson(e as Map<String, dynamic>))
      .toList();
});

// ─── Screen ───────────────────────────────────────────────────────────────────

class TestScenariosScreen extends ConsumerWidget {
  const TestScenariosScreen({super.key});

  static const _riskColors = {
    'LOW':      Color(0xFF197540),
    'MODERATE': Color(0xFFB8771F),
    'HIGH':     Color(0xFFD85A30),
    'CRITICAL': Color(0xFFD64545),
  };
  static const _riskBgColors = {
    'LOW':      Color(0xFFE6F4EC),
    'MODERATE': Color(0xFFFDF3E1),
    'HIGH':     Color(0xFFFDECE2),
    'CRITICAL': Color(0xFFFBE5E5),
  };
  static const _riskLabels = {
    'LOW':      'منخفض',
    'MODERATE': 'متوسط',
    'HIGH':     'مرتفع',
    'CRITICAL': 'حرج',
  };

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final scenariosAsync = ref.watch(testScenariosProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF1E3A72),
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text('سيناريوهات الاختبار',
              style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => context.push('/provider/test-scenarios/new'),
          backgroundColor: const Color(0xFF1E3A72),
          icon: const Icon(Icons.add, color: Colors.white),
          label: const Text('سيناريو جديد',
              style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
        ),
        bottomNavigationBar: BottomNavigationBar(
          currentIndex: 2,
          onTap: (i) {
            if (i == 0) context.go('/provider/dashboard');
            if (i == 1) context.go('/provider/referrals');
            if (i == 2) context.go('/provider/test-scenarios');
          },
          type: BottomNavigationBarType.fixed,
          backgroundColor: Colors.white,
          selectedItemColor: const Color(0xFF1E3A72),
          unselectedItemColor: const Color(0xFF8A93A6),
          selectedFontSize: 10,
          unselectedFontSize: 10,
          elevation: 8,
          items: const [
            BottomNavigationBarItem(
              icon: Icon(Icons.home_outlined),
              activeIcon: Icon(Icons.home),
              label: 'الرئيسية',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.folder_shared_outlined),
              activeIcon: Icon(Icons.folder_shared),
              label: 'الإحالات',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.science_outlined),
              activeIcon: Icon(Icons.science),
              label: 'الاختبار',
            ),
          ],
        ),
        body: scenariosAsync.when(
          data: (scenarios) {
            if (scenarios.isEmpty) {
              return Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 72, height: 72,
                      decoration: BoxDecoration(
                        color: const Color(0xFF1E3A72).withOpacity(0.08),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: const Icon(Icons.science_outlined,
                          color: Color(0xFF1E3A72), size: 36),
                    ),
                    const SizedBox(height: 16),
                    const Text('لا توجد سيناريوهات بعد',
                        style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF0E1726))),
                    const SizedBox(height: 8),
                    const Text('أنشئ سيناريو لتقييم جودة المحتوى',
                        style: TextStyle(
                            fontSize: 13, color: Color(0xFF8A93A6))),
                  ],
                ),
              );
            }
            return RefreshIndicator(
              onRefresh: () => ref.refresh(testScenariosProvider.future),
              child: ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 80),
                itemCount: scenarios.length,
                itemBuilder: (ctx, i) {
                  final s = scenarios[i];
                  final riskColor =
                      _riskColors[s.riskLevel] ?? const Color(0xFF8A93A6);
                  final riskBgColor =
                      _riskBgColors[s.riskLevel] ?? const Color(0xFFEEF0F5);
                  final riskLabel = _riskLabels[s.riskLevel] ?? s.riskLevel;

                  return GestureDetector(
                    onTap: () =>
                        context.push('/provider/test-scenarios/${s.scenarioId}'),
                    child: Container(
                      margin: const EdgeInsets.only(bottom: 10),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFFEEF0F5)),
                        boxShadow: [
                          BoxShadow(
                            color: const Color(0xFF0E1726).withOpacity(0.04),
                            blurRadius: 8,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      child: Column(
                        children: [
                          Container(
                            height: 3,
                            decoration: BoxDecoration(
                              color: riskColor,
                              borderRadius: const BorderRadius.only(
                                topLeft: Radius.circular(12),
                                topRight: Radius.circular(12),
                              ),
                            ),
                          ),
                          Padding(
                            padding: const EdgeInsets.all(12),
                            child: Row(
                              children: [
                                Container(
                                  width: 38, height: 38,
                                  decoration: BoxDecoration(
                                    color: riskBgColor,
                                    borderRadius: BorderRadius.circular(10),
                                  ),
                                  child: const Icon(Icons.science_outlined,
                                      size: 16, color: Color(0xFF1E3A72)),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        s.primaryDiagnosis.isNotEmpty
                                            ? s.primaryDiagnosis
                                            : 'سيناريو اختبار',
                                        style: const TextStyle(
                                          fontSize: 13,
                                          fontWeight: FontWeight.w600,
                                          color: Color(0xFF0E1726),
                                        ),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                      const SizedBox(height: 2),
                                      Row(
                                        children: [
                                          const Icon(Icons.access_time,
                                              size: 11,
                                              color: Color(0xFFB7BDCB)),
                                          const SizedBox(width: 3),
                                          Text(s.formattedDate,
                                              style: const TextStyle(
                                                  fontSize: 11,
                                                  color: Color(0xFF8A93A6))),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 7, vertical: 3),
                                      decoration: BoxDecoration(
                                        color: riskBgColor,
                                        borderRadius: BorderRadius.circular(6),
                                      ),
                                      child: Text('خطر $riskLabel',
                                          style: TextStyle(
                                              fontSize: 10,
                                              fontWeight: FontWeight.w600,
                                              color: riskColor)),
                                    ),
                                    if (s.hasEvaluation) ...[
                                      const SizedBox(height: 4),
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 7, vertical: 3),
                                        decoration: BoxDecoration(
                                          color: const Color(0xFFE6F4EC),
                                          borderRadius: BorderRadius.circular(6),
                                        ),
                                        child: const Text('تم التقييم',
                                            style: TextStyle(
                                                fontSize: 10,
                                                fontWeight: FontWeight.w600,
                                                color: Color(0xFF197540))),
                                      ),
                                    ],
                                  ],
                                ),
                                const SizedBox(width: 6),
                                const Icon(Icons.chevron_left,
                                    color: Color(0xFFB7BDCB), size: 18),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            );
          },
          loading: () => const Center(
              child: CircularProgressIndicator(color: Color(0xFF1E3A72))),
          error: (e, _) => Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.wifi_off_outlined,
                    size: 40, color: Color(0xFFB7BDCB)),
                const SizedBox(height: 12),
                const Text('تعذّر تحميل السيناريوهات',
                    style: TextStyle(color: Color(0xFF5A6478))),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: () => ref.refresh(testScenariosProvider),
                  child: const Text('إعادة المحاولة',
                      style: TextStyle(color: Color(0xFF1E3A72))),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
