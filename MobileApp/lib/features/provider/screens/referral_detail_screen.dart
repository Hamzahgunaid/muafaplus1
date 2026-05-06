import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';
import '../../../core/models/referral_article.dart';
import '../../../core/widgets/article_outline_card.dart';

const _base = 'https://muafaplus1-production.up.railway.app/api/v1';

// ─── Models ───────────────────────────────────────────────────────────────────

class ProviderReferralDetail {
  final String referralId;
  final String? sessionId;
  final String status;
  final String riskLevel;
  final String patientPhone;
  final String primaryDiagnosis;
  final bool whatsAppDelivery;
  final String createdAt;
  final String? deliveredAt;
  final bool chatEnabled;
  final EngagementTimeline engagement;

  ProviderReferralDetail({
    required this.referralId,
    required this.sessionId,
    required this.status,
    required this.riskLevel,
    required this.patientPhone,
    required this.primaryDiagnosis,
    required this.whatsAppDelivery,
    required this.createdAt,
    required this.deliveredAt,
    required this.chatEnabled,
    required this.engagement,
  });

  factory ProviderReferralDetail.fromJson(Map<String, dynamic> j) =>
      ProviderReferralDetail(
        referralId: j['referralId'] as String? ?? '',
        sessionId: j['sessionId'] as String?,
        status: j['status'] as String? ?? '',
        riskLevel: j['riskLevel'] as String? ?? 'LOW',
        patientPhone: j['patientPhone'] as String? ?? '',
        primaryDiagnosis: j['primaryDiagnosis'] as String? ??
            j['patientProfile']?['primaryDiagnosis'] as String? ?? '',
        whatsAppDelivery: j['whatsAppDelivery'] as bool? ?? false,
        createdAt: j['createdAt'] as String? ?? '',
        deliveredAt: j['deliveredAt'] as String?,
        chatEnabled: j['chatEnabled'] as bool? ?? false,
        engagement: EngagementTimeline.fromJson(
            j['engagement'] as Map<String, dynamic>? ?? {}),
      );
}

class EngagementTimeline {
  final String? messageSentAt;
  final String? appOpenedAt;
  final String? summaryViewedAt;
  final String? stage2RequestedAt;
  final String? feedbackSubmittedAt;

  EngagementTimeline({
    this.messageSentAt,
    this.appOpenedAt,
    this.summaryViewedAt,
    this.stage2RequestedAt,
    this.feedbackSubmittedAt,
  });

  factory EngagementTimeline.fromJson(Map<String, dynamic> j) =>
      EngagementTimeline(
        messageSentAt: j['messageSentAt'] as String?,
        appOpenedAt: j['appOpenedAt'] as String?,
        summaryViewedAt: j['summaryViewedAt'] as String?,
        stage2RequestedAt: j['stage2RequestedAt'] as String?,
        feedbackSubmittedAt: j['feedbackSubmittedAt'] as String?,
      );
}

// ─── Provider ─────────────────────────────────────────────────────────────────

final providerReferralDetailProvider =
    FutureProvider.family<ProviderReferralDetail, String>((ref, id) async {
  final auth = ref.watch(physicianAuthProvider);
  if (auth.isInitializing) throw Exception('Initializing');
  if (auth.token == null) throw Exception('Not authenticated');
  final dio = Dio();
  final resp = await dio.get(
    '$_base/referrals/$id',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  return ProviderReferralDetail.fromJson(
      resp.data['data'] as Map<String, dynamic>);
});

// ─── Screen ───────────────────────────────────────────────────────────────────

class ProviderReferralDetailScreen extends ConsumerStatefulWidget {
  final String referralId;
  const ProviderReferralDetailScreen({super.key, required this.referralId});

  @override
  ConsumerState<ProviderReferralDetailScreen> createState() =>
      _ProviderReferralDetailScreenState();
}

class _ProviderReferralDetailScreenState
    extends ConsumerState<ProviderReferralDetailScreen> {
  List<ReferralArticle> _articles = [];
  bool _articlesLoading = true;
  bool _triggeringStage2 = false;
  bool _stage2Requested = false;
  bool _articlesLoadTriggered = false;

  Future<void> _loadArticles(String token) async {
    try {
      final dio = Dio();
      final res = await dio.get(
        '$_base/referrals/${widget.referralId}/articles',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );
      final items = res.data['data'] as List? ?? [];
      if (mounted) {
        setState(() {
          _articles = items
              .map((e) => ReferralArticle.fromJson(e as Map<String, dynamic>))
              .toList();
          _articlesLoading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _articlesLoading = false);
    }
  }

  Future<void> _triggerStage2(String token) async {
    setState(() => _triggeringStage2 = true);
    try {
      final dio = Dio();
      await dio.post(
        '$_base/referrals/${widget.referralId}/stage2',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );
      if (mounted) {
        setState(() {
          _stage2Requested = true;
          _triggeringStage2 = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _triggeringStage2 = false);
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

  String _formatDate(String? iso) {
    if (iso == null || iso.isEmpty) return '';
    try {
      final dt = DateTime.parse(iso).toLocal();
      return '${dt.day}/${dt.month}/${dt.year} ${dt.hour}:${dt.minute.toString().padLeft(2, '0')}';
    } catch (_) {
      return iso;
    }
  }

  Widget _spinnerCard(String label) => Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Row(children: [
          const SizedBox(
            width: 20, height: 20,
            child: CircularProgressIndicator(
                strokeWidth: 2, color: Color(0xFF1E3A72)),
          ),
          const SizedBox(width: 14),
          Text(label,
              style: const TextStyle(
                  fontSize: 14, color: Color(0xFF5A6478))),
        ]),
      );

  Widget _buildArticleSection(ProviderReferralDetail detail, String token) {
    // Stage 1 not yet complete
    if (detail.sessionId == null) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircularProgressIndicator(color: Color(0xFF1E3A72)),
            SizedBox(height: 12),
            Text('جاري توليد المحتوى الصحي...',
                style: TextStyle(color: Color(0xFF5A6478))),
          ],
        ),
      );
    }

    // Articles still loading
    if (_articlesLoading) {
      return const Padding(
        padding: EdgeInsets.all(24),
        child: Center(
            child: CircularProgressIndicator(color: Color(0xFF1E3A72))),
      );
    }

    final stage2InProgress = detail.engagement.stage2RequestedAt != null ||
        _stage2Requested ||
        _triggeringStage2;

    // No articles, stage 2 not triggered
    if (_articles.isEmpty && !stage2InProgress) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('المحتوى المولَّد',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF0E1726))),
            const SizedBox(height: 12),
            ArticleOutlineCard(
              index: 1,
              title: 'المقالات التفصيلية',
              state: ArticleOutlineState.notGenerated,
              onGenerate: () => _triggerStage2(token),
            ),
          ],
        ),
      );
    }

    // Stage 2 queued but articles not yet ready
    if (_articles.isEmpty) {
      return _spinnerCard('جاري توليد المقالات التفصيلية...');
    }

    // Articles available — split by type
    final summaries = _articles.where((a) => a.isSummary).toList();
    final detailed = _articles.where((a) => a.isDetailed).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (summaries.isNotEmpty) ...[
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Row(children: [
              const Text('الملخص الصحي',
                  style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF0E1726))),
              const SizedBox(width: 8),
              _StageBadge(label: 'المرحلة ١'),
            ]),
          ),
          ...summaries.asMap().entries.map((e) => ArticleOutlineCard(
                key: ValueKey('sum_${e.value.articleId}'),
                index: e.key + 1,
                title: e.value.title.isNotEmpty ? e.value.title : 'الملخص الصحي',
                state: ArticleOutlineState.generated,
                content: e.value.contentAr,
                // onView: null → inline expand
              )),
          const SizedBox(height: 16),
        ],
        if (detailed.isNotEmpty) ...[
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Row(children: [
              const Text('المحتوى التفصيلي',
                  style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF0E1726))),
              const SizedBox(width: 8),
              _StageBadge(label: 'المرحلة ٢'),
              const SizedBox(width: 8),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFFE6F4EC),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text('${detailed.length} مقالات',
                    style: const TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF197540))),
              ),
            ]),
          ),
          ...detailed.asMap().entries.map((e) => ArticleOutlineCard(
                key: ValueKey('det_${e.value.articleId}'),
                index: e.key + 1,
                title: e.value.title,
                state: ArticleOutlineState.generated,
                onView: () => context.push(
                    '/article/${widget.referralId}/${e.value.articleId}'),
              )),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final token = ref.read(physicianAuthProvider).token ?? '';
    final detailAsync =
        ref.watch(providerReferralDetailProvider(widget.referralId));

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF1E3A72),
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text('تفاصيل الإحالة',
              style: TextStyle(
                  fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        body: detailAsync.when(
          data: (detail) {
            // Trigger article load once, after sessionId is confirmed
            if (!_articlesLoadTriggered) {
              _articlesLoadTriggered = true;
              WidgetsBinding.instance.addPostFrameCallback((_) {
                if (!mounted) return;
                if (detail.sessionId == null) {
                  setState(() => _articlesLoading = false);
                } else {
                  _loadArticles(token);
                }
              });
            }
            return SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // ── Patient header card ─────────────────────────────
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
                                    width: 48, height: 48,
                                    decoration: BoxDecoration(
                                      gradient: const LinearGradient(
                                        colors: [
                                          Color(0xFF1E3A72),
                                          Color(0xFF11254A)
                                        ],
                                      ),
                                      borderRadius:
                                          BorderRadius.circular(12),
                                    ),
                                    child: const Icon(Icons.person,
                                        color: Colors.white, size: 24),
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
                                              : detail.patientPhone.isNotEmpty
                                                  ? detail.patientPhone
                                                  : 'إحالة طبية',
                                          style: const TextStyle(
                                            fontSize: 16,
                                            fontWeight: FontWeight.w700,
                                            color: Color(0xFF0E1726),
                                          ),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(detail.patientPhone,
                                            style: const TextStyle(
                                                fontSize: 13,
                                                color:
                                                    Color(0xFF8A93A6))),
                                      ],
                                    ),
                                  ),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 10, vertical: 5),
                                    decoration: BoxDecoration(
                                      color: _riskBgColor(
                                          detail.riskLevel),
                                      borderRadius:
                                          BorderRadius.circular(8),
                                    ),
                                    child: Text(
                                      'خطر ${_riskLabel(detail.riskLevel)}',
                                      style: TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.w600,
                                        color: _riskColor(
                                            detail.riskLevel),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 12),
                              const Divider(color: Color(0xFFEEF0F5)),
                              const SizedBox(height: 8),
                              Row(
                                children: [
                                  _InfoChip(
                                      icon: Icons.calendar_today_outlined,
                                      label: _formatDate(
                                          detail.createdAt)),
                                  const SizedBox(width: 8),
                                  _InfoChip(
                                      icon: detail.whatsAppDelivery
                                          ? Icons.check_circle_outline
                                          : Icons.pending_outlined,
                                      label: detail.whatsAppDelivery
                                          ? 'تم الإرسال'
                                          : 'لم يُرسل',
                                      color: detail.whatsAppDelivery
                                          ? const Color(0xFF197540)
                                          : const Color(0xFF8A93A6)),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Engagement timeline ─────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(14),
                      border:
                          Border.all(color: const Color(0xFFEEF0F5)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('مسار التفاعل',
                            style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFF0E1726))),
                        const SizedBox(height: 16),
                        _TimelineStep(
                          label: 'تم إنشاء الإحالة',
                          time: _formatDate(detail.createdAt),
                          done: true,
                          isFirst: true,
                        ),
                        _TimelineStep(
                          label: 'تم إرسال الرسالة',
                          time: _formatDate(
                              detail.engagement.messageSentAt),
                          done:
                              detail.engagement.messageSentAt != null,
                        ),
                        _TimelineStep(
                          label: 'فتح المريض التطبيق',
                          time: _formatDate(
                              detail.engagement.appOpenedAt),
                          done: detail.engagement.appOpenedAt != null,
                        ),
                        _TimelineStep(
                          label: 'قرأ الملخص الصحي',
                          time: _formatDate(
                              detail.engagement.summaryViewedAt),
                          done: detail.engagement.summaryViewedAt !=
                              null,
                        ),
                        _TimelineStep(
                          label: 'طلب المقالات التفصيلية',
                          time: _formatDate(
                              detail.engagement.stage2RequestedAt),
                          done: detail.engagement.stage2RequestedAt !=
                              null,
                        ),
                        _TimelineStep(
                          label: 'أرسل التغذية الراجعة',
                          time: _formatDate(detail
                              .engagement.feedbackSubmittedAt),
                          done: detail.engagement
                                  .feedbackSubmittedAt !=
                              null,
                          isLast: true,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Article section ─────────────────────────────────
                  _buildArticleSection(detail, token),
                ],
              ),
            );
          },
          loading: () => const Center(
              child: CircularProgressIndicator(
                  color: Color(0xFF1E3A72))),
          error: (e, _) => Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error_outline,
                    size: 48, color: Color(0xFFB7BDCB)),
                const SizedBox(height: 12),
                const Text('تعذّر تحميل تفاصيل الإحالة',
                    style: TextStyle(color: Color(0xFF5A6478))),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: () => ref.refresh(
                      providerReferralDetailProvider(
                          widget.referralId)),
                  child: const Text('إعادة المحاولة',
                      style:
                          TextStyle(color: Color(0xFF1E3A72))),
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

class _StageBadge extends StatelessWidget {
  final String label;
  const _StageBadge({required this.label});

  @override
  Widget build(BuildContext context) => Container(
        padding:
            const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
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

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;

  const _InfoChip({
    required this.icon,
    required this.label,
    this.color = const Color(0xFF5A6478),
  });

  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: color),
          const SizedBox(width: 4),
          Text(label, style: TextStyle(fontSize: 11, color: color)),
        ],
      );
}

class _TimelineStep extends StatelessWidget {
  final String label;
  final String time;
  final bool done;
  final bool isFirst;
  final bool isLast;

  const _TimelineStep({
    required this.label,
    required this.time,
    required this.done,
    this.isFirst = false,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) => Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 20, height: 20,
                decoration: BoxDecoration(
                  color: done
                      ? const Color(0xFF197540)
                      : const Color(0xFFEEF0F5),
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: done
                        ? const Color(0xFF197540)
                        : const Color(0xFFDFE2EA),
                    width: 2,
                  ),
                ),
                child: done
                    ? const Icon(Icons.check,
                        size: 10, color: Colors.white)
                    : null,
              ),
              if (!isLast)
                Container(
                  width: 2,
                  height: 32,
                  color: done
                      ? const Color(0xFF197540).withOpacity(0.3)
                      : const Color(0xFFEEF0F5),
                ),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label,
                      style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                          color: done
                              ? const Color(0xFF0E1726)
                              : const Color(0xFF8A93A6))),
                  if (time.isNotEmpty)
                    Text(time,
                        style: const TextStyle(
                            fontSize: 11,
                            color: Color(0xFF8A93A6))),
                ],
              ),
            ),
          ),
        ],
      );
}
