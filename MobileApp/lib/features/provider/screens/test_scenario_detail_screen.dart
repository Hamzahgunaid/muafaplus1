import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';
import '../../../core/widgets/article_outline_card.dart';

// ─── Models ───────────────────────────────────────────────────────────────────

class ScenarioDetail {
  final String scenarioId;
  final String primaryDiagnosis;
  final String age;
  final String comorbidities;
  final String currentMedications;
  final String allergies;
  final String medicalRestrictions;
  final String riskLevel;
  final String status;
  final String createdAt;
  final String stage1Summary;
  final List<ScenarioArticle> articles;
  final List<String> outlineTitles;
  final bool hasEvaluation;

  ScenarioDetail({
    required this.scenarioId,
    required this.primaryDiagnosis,
    required this.age,
    required this.comorbidities,
    required this.currentMedications,
    required this.allergies,
    required this.medicalRestrictions,
    required this.riskLevel,
    required this.status,
    required this.createdAt,
    required this.stage1Summary,
    required this.articles,
    required this.outlineTitles,
    required this.hasEvaluation,
  });

  factory ScenarioDetail.fromJson(Map<String, dynamic> j) {
    Map<String, dynamic> patientData = {};
    try {
      final raw = j['patientDataJson'] as String? ?? '';
      if (raw.isNotEmpty) patientData = json.decode(raw);
    } catch (_) {}

    String stage1Summary = '';
    List<String> outlineTitles = [];
    try {
      final raw = j['generatedContentJson'] as String? ?? '';
      if (raw.isNotEmpty) {
        final content = json.decode(raw) as Map<String, dynamic>;
        stage1Summary = content['summary_article'] as String? ??
            content['summaryArticle'] as String? ??
            content['summary'] as String? ??
            content['content'] as String? ?? '';
        final outlines = content['article_outlines'] as List? ?? [];
        outlineTitles = outlines
            .map((o) => (o as Map<String, dynamic>)['title_ar'] as String? ?? '')
            .where((t) => t.isNotEmpty)
            .toList();
      }
    } catch (_) {}

    List<ScenarioArticle> articles = [];
    try {
      final raw = j['generatedArticlesJson'] as String? ?? '';
      if (raw.isNotEmpty) {
        final decoded = json.decode(raw);
        List articleList = [];
        if (decoded is List) {
          articleList = decoded;
        } else if (decoded is Map) {
          articleList = decoded['articles'] as List? ??
                        decoded['items'] as List? ?? [];
        }
        articles = articleList
            .map((a) => ScenarioArticle.fromJson(a as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {}

    return ScenarioDetail(
      scenarioId: j['scenarioId']?.toString() ?? '',
      primaryDiagnosis: patientData['PrimaryDiagnosis'] as String? ?? patientData['primaryDiagnosis'] as String? ?? '',
      age: patientData['AgeGroup'] as String? ?? patientData['age'] as String? ?? patientData['Age'] as String? ?? '',
      comorbidities: patientData['Comorbidities'] as String? ?? patientData['comorbidities'] as String? ?? '',
      currentMedications: patientData['CurrentMedications'] as String? ?? patientData['currentMedications'] as String? ?? '',
      allergies: patientData['Allergies'] as String? ?? patientData['allergies'] as String? ?? '',
      medicalRestrictions: patientData['MedicalRestrictions'] as String? ?? patientData['medicalRestrictions'] as String? ?? '',
      riskLevel: j['riskLevel'] as String? ?? 'LOW',
      status: j['status'] as String? ?? '',
      createdAt: j['createdAt'] as String? ?? '',
      stage1Summary: stage1Summary,
      articles: articles,
      outlineTitles: outlineTitles,
      hasEvaluation: j['evaluation'] != null,
    );
  }
}

class ScenarioArticle {
  final String title;
  final String content;

  ScenarioArticle({required this.title, required this.content});

  factory ScenarioArticle.fromJson(Map<String, dynamic> j) => ScenarioArticle(
        title: j['title_ar'] ?? j['titleAr'] ?? j['title'] ?? j['Title'] ?? '',
        content: j['content_ar'] ?? j['contentAr'] ?? j['content'] ?? j['Content'] ?? '',
      );
}

// ─── Provider ─────────────────────────────────────────────────────────────────

final scenarioDetailProvider =
    FutureProvider.family<ScenarioDetail, String>((ref, scenarioId) async {
  final auth = ref.watch(physicianAuthProvider);
  if (auth.isInitializing) return ScenarioDetail(
    scenarioId: scenarioId,
    primaryDiagnosis: '',
    age: '',
    comorbidities: '',
    currentMedications: '',
    allergies: '',
    medicalRestrictions: '',
    riskLevel: 'LOW',
    status: '',
    createdAt: '',
    stage1Summary: '',
    articles: [],
    outlineTitles: [],
    hasEvaluation: false,
  );
  if (auth.token == null) throw Exception('Not authenticated');
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/test-scenarios/$scenarioId',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  return ScenarioDetail.fromJson(
      resp.data['data'] as Map<String, dynamic>);
});

// ─── Screen ───────────────────────────────────────────────────────────────────

class TestScenarioDetailScreen extends ConsumerStatefulWidget {
  final String scenarioId;
  const TestScenarioDetailScreen({super.key, required this.scenarioId});

  @override
  ConsumerState<TestScenarioDetailScreen> createState() =>
      _TestScenarioDetailScreenState();
}

class _TestScenarioDetailScreenState
    extends ConsumerState<TestScenarioDetailScreen> {
  final Set<int> _generatingIndexes = {};

  Future<void> _generateArticle(int index) async {
    setState(() => _generatingIndexes.add(index));
    try {
      final auth = ref.read(physicianAuthProvider);
      final dio = Dio();
      await dio.post(
        'https://muafaplus1-production.up.railway.app/api/v1/test-scenarios/${widget.scenarioId}/generate-article?index=$index',
        options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
      );
      ref.invalidate(scenarioDetailProvider(widget.scenarioId));
    } catch (_) {
      ref.invalidate(scenarioDetailProvider(widget.scenarioId));
    } finally {
      if (mounted) setState(() => _generatingIndexes.remove(index));
    }
  }

  Color _riskColor(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return const Color(0xFFD64545);
      case 'HIGH':     return const Color(0xFFD85A30);
      case 'MODERATE': return const Color(0xFFB8771F);
      default:         return const Color(0xFF197540);
    }
  }

  Color _riskBgColor(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return const Color(0xFFFBE5E5);
      case 'HIGH':     return const Color(0xFFFDECE2);
      case 'MODERATE': return const Color(0xFFFDF3E1);
      default:         return const Color(0xFFE6F4EC);
    }
  }

  String _riskLabel(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return 'حرج';
      case 'HIGH':     return 'مرتفع';
      case 'MODERATE': return 'متوسط';
      default:         return 'منخفض';
    }
  }

  @override
  Widget build(BuildContext context) {
    final detailAsync = ref.watch(scenarioDetailProvider(widget.scenarioId));

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF1E3A72),
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text('تفاصيل السيناريو',
              style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        body: detailAsync.when(
          data: (detail) => Stack(
            children: [
              SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // ── Patient profile card ─────────────────────────────
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(color: const Color(0xFFEEF0F5)),
                      ),
                      child: Column(
                        children: [
                          Container(
                            height: 4,
                            decoration: BoxDecoration(
                              color: _riskColor(detail.riskLevel),
                              borderRadius: const BorderRadius.only(
                                topLeft: Radius.circular(14),
                                topRight: Radius.circular(14),
                              ),
                            ),
                          ),
                          Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Container(
                                      width: 44, height: 44,
                                      decoration: BoxDecoration(
                                        color: const Color(0xFFEEF1F7),
                                        borderRadius: BorderRadius.circular(12),
                                      ),
                                      child: const Icon(Icons.science_outlined,
                                          color: Color(0xFF1E3A72), size: 22),
                                    ),
                                    const SizedBox(width: 12),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            detail.primaryDiagnosis.isNotEmpty
                                                ? detail.primaryDiagnosis
                                                : 'سيناريو اختبار',
                                            style: const TextStyle(
                                                fontSize: 16,
                                                fontWeight: FontWeight.w700,
                                                color: Color(0xFF0E1726)),
                                          ),
                                          if (detail.age.isNotEmpty)
                                            Text(detail.age,
                                                style: const TextStyle(
                                                    fontSize: 12,
                                                    color: Color(0xFF8A93A6))),
                                        ],
                                      ),
                                    ),
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 10, vertical: 5),
                                      decoration: BoxDecoration(
                                        color: _riskBgColor(detail.riskLevel),
                                        borderRadius: BorderRadius.circular(8),
                                      ),
                                      child: Text(
                                        'خطر ${_riskLabel(detail.riskLevel)}',
                                        style: TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.w600,
                                            color: _riskColor(detail.riskLevel)),
                                      ),
                                    ),
                                  ],
                                ),
                                if (detail.comorbidities.isNotEmpty ||
                                    detail.currentMedications.isNotEmpty) ...[
                                  const SizedBox(height: 12),
                                  const Divider(color: Color(0xFFEEF0F5)),
                                  const SizedBox(height: 8),
                                  if (detail.comorbidities.isNotEmpty)
                                    _ProfileRow(
                                        label: 'الأمراض المصاحبة',
                                        value: detail.comorbidities),
                                  if (detail.currentMedications.isNotEmpty)
                                    _ProfileRow(
                                        label: 'الأدوية الحالية',
                                        value: detail.currentMedications),
                                  if (detail.allergies.isNotEmpty)
                                    _ProfileRow(
                                        label: 'الحساسية',
                                        value: detail.allergies),
                                  if (detail.medicalRestrictions.isNotEmpty)
                                    _ProfileRow(
                                        label: 'القيود الطبية',
                                        value: detail.medicalRestrictions),
                                ],
                                if (detail.hasEvaluation) ...[
                                  const SizedBox(height: 12),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 10, vertical: 6),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFFE6F4EC),
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                    child: const Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Icon(Icons.check_circle,
                                            size: 14, color: Color(0xFF197540)),
                                        SizedBox(width: 6),
                                        Text('تم التقييم',
                                            style: TextStyle(
                                                fontSize: 12,
                                                fontWeight: FontWeight.w600,
                                                color: Color(0xFF197540))),
                                      ],
                                    ),
                                  ),
                                ],
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    // ── Stage 1 Summary ──────────────────────────────────
                    if (detail.stage1Summary.isNotEmpty) ...[
                      const Padding(
                        padding: EdgeInsets.only(bottom: 10),
                        child: Row(
                          children: [
                            Text('الملخص الصحي',
                                style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w700,
                                    color: Color(0xFF0E1726))),
                            SizedBox(width: 8),
                            _StageBadge(label: 'المرحلة ١'),
                          ],
                        ),
                      ),
                      ArticleOutlineCard(
                        index: 1,
                        title: 'الملخص الصحي المولَّد',
                        state: ArticleOutlineState.generated,
                        content: detail.stage1Summary,
                      ),
                      const SizedBox(height: 16),
                    ],

                    // ── Stage 2 Articles ─────────────────────────────────
                    if (detail.stage1Summary.isNotEmpty) ...[
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: Row(
                          children: [
                            const Text('المقالات التفصيلية',
                                style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w700,
                                    color: Color(0xFF0E1726))),
                            const SizedBox(width: 8),
                            const _StageBadge(label: 'المرحلة ٢'),
                            if (detail.articles.isNotEmpty) ...[
                              const SizedBox(width: 8),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFE6F4EC),
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: Text(
                                  '${detail.articles.length} مقالات',
                                  style: const TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w600,
                                      color: Color(0xFF197540)),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                      if (detail.outlineTitles.isNotEmpty)
                        ...List.generate(detail.outlineTitles.length, (i) {
                          final hasArticle = detail.articles.length > i;
                          final content = hasArticle &&
                                  detail.articles[i].content.isNotEmpty
                              ? detail.articles[i].content
                              : null;
                          final isGenerating = _generatingIndexes.contains(i);
                          return ArticleOutlineCard(
                            key: ValueKey('scenario_article_$i'),
                            index: i + 1,
                            title: detail.outlineTitles[i],
                            state: isGenerating
                                ? ArticleOutlineState.generating
                                : (content != null
                                    ? ArticleOutlineState.generated
                                    : ArticleOutlineState.notGenerated),
                            onGenerate:
                                isGenerating ? null : () => _generateArticle(i),
                            content: content,
                          );
                        })
                      else if (detail.articles.isNotEmpty)
                        ...detail.articles.asMap().entries.map((e) =>
                            ArticleOutlineCard(
                              key: ValueKey('scenario_article_${e.key}'),
                              index: e.key + 1,
                              title: e.value.title,
                              state: e.value.content.isNotEmpty
                                  ? ArticleOutlineState.generated
                                  : ArticleOutlineState.notGenerated,
                              onGenerate: e.value.content.isNotEmpty
                                  ? null
                                  : () => _generateArticle(e.key),
                              content: e.value.content.isNotEmpty
                                  ? e.value.content
                                  : null,
                            )),
                    ],

                    // ── Empty state ──────────────────────────────────────
                    if (detail.stage1Summary.isEmpty && detail.articles.isEmpty)
                      Container(
                        padding: const EdgeInsets.all(24),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: const Color(0xFFEEF0F5)),
                        ),
                        child: const Center(
                          child: Text('لم يتم توليد المحتوى بعد',
                              style: TextStyle(
                                  color: Color(0xFF8A93A6), fontSize: 13)),
                        ),
                      ),
                  ],
                ),
              ),

              // ── Sticky evaluation button ─────────────────────────────
              Positioned(
                bottom: 0, left: 0, right: 0,
                child: Container(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFF0E1726).withOpacity(0.08),
                        blurRadius: 16,
                        offset: const Offset(0, -4),
                      ),
                    ],
                  ),
                  child: ElevatedButton.icon(
                    onPressed: detail.hasEvaluation
                        ? null
                        : () => context.push(
                            '/provider/test-scenarios/${widget.scenarioId}/evaluate'),
                    icon: Icon(
                      detail.hasEvaluation
                          ? Icons.check_circle
                          : Icons.star_outline,
                      color: Colors.white,
                    ),
                    label: Text(
                      detail.hasEvaluation
                          ? 'تم التقييم ✅'
                          : 'تقييم جودة المحتوى',
                      style: const TextStyle(
                          color: Colors.white,
                          fontSize: 15,
                          fontWeight: FontWeight.w600),
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: detail.hasEvaluation
                          ? const Color(0xFF197540)
                          : const Color(0xFF1E3A72),
                      disabledBackgroundColor: const Color(0xFF197540),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                  ),
                ),
              ),
            ],
          ),
          loading: () => const Center(
              child: CircularProgressIndicator(color: Color(0xFF1E3A72))),
          error: (e, _) => Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error_outline,
                    size: 48, color: Color(0xFFB7BDCB)),
                const SizedBox(height: 12),
                const Text('تعذّر تحميل السيناريو',
                    style: TextStyle(color: Color(0xFF5A6478))),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: () =>
                      ref.refresh(scenarioDetailProvider(widget.scenarioId)),
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

// ─── Helper widgets ───────────────────────────────────────────────────────────

class _ProfileRow extends StatelessWidget {
  final String label;
  final String value;
  const _ProfileRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 110,
              child: Text(label,
                  style: const TextStyle(
                      fontSize: 11,
                      color: Color(0xFF8A93A6),
                      fontWeight: FontWeight.w500)),
            ),
            Expanded(
              child: Text(value,
                  style: const TextStyle(
                      fontSize: 12, color: Color(0xFF2D3748))),
            ),
          ],
        ),
      );
}

class _StageBadge extends StatelessWidget {
  final String label;
  const _StageBadge({required this.label});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: const Color(0xFFEEF1F7),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(label,
            style: const TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: Color(0xFF1E3A72))),
      );
}
