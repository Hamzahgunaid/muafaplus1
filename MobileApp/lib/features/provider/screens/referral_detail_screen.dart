import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../providers/physician_auth_provider.dart';
import '../../../core/widgets/article_outline_card.dart';

// ─── Models ───────────────────────────────────────────────────────────────────

class ProviderReferralDetail {
  final String referralId;
  final String status;
  final String riskLevel;
  final String patientPhone;
  final String primaryDiagnosis;
  final bool whatsAppDelivery;
  final String createdAt;
  final String? deliveredAt;
  final bool chatEnabled;
  final EngagementTimeline engagement;
  final List<ReferralArticle> articles;

  ProviderReferralDetail({
    required this.referralId,
    required this.status,
    required this.riskLevel,
    required this.patientPhone,
    required this.primaryDiagnosis,
    required this.whatsAppDelivery,
    required this.createdAt,
    required this.deliveredAt,
    required this.chatEnabled,
    required this.engagement,
    required this.articles,
  });

  factory ProviderReferralDetail.fromJson(Map<String, dynamic> j) =>
      ProviderReferralDetail(
        referralId: j['referralId'] ?? '',
        status: j['status'] ?? '',
        riskLevel: j['riskLevel'] ?? 'LOW',
        patientPhone: j['patientPhone'] ?? '',
        primaryDiagnosis: j['primaryDiagnosis'] ?? '',
        whatsAppDelivery: j['whatsAppDelivery'] ?? false,
        createdAt: j['createdAt'] ?? '',
        deliveredAt: j['deliveredAt'],
        chatEnabled: j['chatEnabled'] ?? false,
        engagement: EngagementTimeline.fromJson(
            j['engagement'] as Map<String, dynamic>? ?? {}),
        articles: [],
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
        messageSentAt: j['messageSentAt'],
        appOpenedAt: j['appOpenedAt'],
        summaryViewedAt: j['summaryViewedAt'],
        stage2RequestedAt: j['stage2RequestedAt'],
        feedbackSubmittedAt: j['feedbackSubmittedAt'],
      );
}

class ReferralArticle {
  final String articleId;
  final String title;
  final String? content;

  ReferralArticle({
    required this.articleId,
    required this.title,
    this.content,
  });

  factory ReferralArticle.fromJson(Map<String, dynamic> j) {
    final raw = j['content_ar'] ?? j['content'] ?? j['contentAr'] ??
        j['articleContent'] ?? j['body'];
    return ReferralArticle(
      articleId: j['articleId'] ?? j['id'] ?? '',
      title: j['title'] ?? j['titleAr'] ?? j['title_ar'] ??
          j['articleTitle'] ?? j['heading'] ?? '',
      content: (raw != null && (raw as String).isNotEmpty) ? raw : null,
    );
  }
}

// ─── Providers ────────────────────────────────────────────────────────────────

final providerReferralDetailProvider =
    FutureProvider.family<ProviderReferralDetail, String>((ref, id) async {
  final auth = ref.watch(physicianAuthProvider);
  if (auth.isInitializing) throw Exception('Initializing');
  if (auth.token == null) throw Exception('Not authenticated');
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/referrals/$id',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  return ProviderReferralDetail.fromJson(
      resp.data['data'] as Map<String, dynamic>);
});

final providerReferralArticlesProvider =
    FutureProvider.family<List<ReferralArticle>, String>((ref, referralId) async {
  final auth = ref.watch(physicianAuthProvider);
  if (auth.isInitializing) return [];
  if (auth.token == null) return [];
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/referrals/$referralId/articles',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  final data = resp.data['data'] as List? ?? [];
  return data.map((a) => ReferralArticle.fromJson(a as Map<String, dynamic>)).toList();
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
  bool _generatingAll = false;

  Future<void> _generateArticles() async {
    setState(() => _generatingAll = true);
    try {
      final auth = ref.read(physicianAuthProvider);
      final dio = Dio();
      await dio.post(
        'https://muafaplus1-production.up.railway.app/api/v1/referrals/${widget.referralId}/stage2',
        options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
      );
      ref.invalidate(providerReferralArticlesProvider(widget.referralId));
    } catch (_) {
      // Silently refresh — articles may appear on next load
      ref.invalidate(providerReferralArticlesProvider(widget.referralId));
    } finally {
      if (mounted) setState(() => _generatingAll = false);
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

  @override
  Widget build(BuildContext context) {
    final detailAsync = ref.watch(providerReferralDetailProvider(widget.referralId));

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF1E3A72),
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text('تفاصيل الإحالة',
              style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
        ),
        body: detailAsync.when(
          data: (detail) => SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ── Patient header card ──────────────────────────────────
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
                                      colors: [Color(0xFF1E3A72), Color(0xFF11254A)],
                                    ),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: const Icon(Icons.person,
                                      color: Colors.white, size: 24),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        detail.primaryDiagnosis.isNotEmpty
                                            ? detail.primaryDiagnosis
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
                                      color: _riskColor(detail.riskLevel),
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
                                    label: _formatDate(detail.createdAt)),
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

                // ── Engagement timeline ──────────────────────────────────
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
                        time: _formatDate(detail.engagement.messageSentAt),
                        done: detail.engagement.messageSentAt != null,
                      ),
                      _TimelineStep(
                        label: 'فتح المريض التطبيق',
                        time: _formatDate(detail.engagement.appOpenedAt),
                        done: detail.engagement.appOpenedAt != null,
                      ),
                      _TimelineStep(
                        label: 'قرأ الملخص الصحي',
                        time: _formatDate(detail.engagement.summaryViewedAt),
                        done: detail.engagement.summaryViewedAt != null,
                      ),
                      _TimelineStep(
                        label: 'طلب المقالات التفصيلية',
                        time: _formatDate(detail.engagement.stage2RequestedAt),
                        done: detail.engagement.stage2RequestedAt != null,
                      ),
                      _TimelineStep(
                        label: 'أرسل التغذية الراجعة',
                        time: _formatDate(detail.engagement.feedbackSubmittedAt),
                        done: detail.engagement.feedbackSubmittedAt != null,
                        isLast: true,
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // ── Articles section ─────────────────────────────────────
                Consumer(
                  builder: (context, ref, _) {
                    final articlesAsync = ref.watch(
                        providerReferralArticlesProvider(detail.referralId));
                    return articlesAsync.when(
                      data: (articles) {
                        if (articles.isEmpty && !_generatingAll) {
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
                                  onGenerate: _generateArticles,
                                ),
                              ],
                            ),
                          );
                        }
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: Row(
                                children: [
                                  const Text('المحتوى المولَّد',
                                      style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w700,
                                          color: Color(0xFF0E1726))),
                                  const SizedBox(width: 8),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 8, vertical: 3),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFFE6F4EC),
                                      borderRadius: BorderRadius.circular(20),
                                    ),
                                    child: Text('${articles.length} مقالات',
                                        style: const TextStyle(
                                            fontSize: 11,
                                            fontWeight: FontWeight.w600,
                                            color: Color(0xFF197540))),
                                  ),
                                ],
                              ),
                            ),
                            ...articles.asMap().entries.map((entry) {
                              final i = entry.key;
                              final article = entry.value;
                              final isGen = _generatingAll && article.content == null;
                              return ArticleOutlineCard(
                                key: ValueKey(article.articleId),
                                index: i + 1,
                                title: article.title,
                                state: isGen
                                    ? ArticleOutlineState.generating
                                    : (article.content != null
                                        ? ArticleOutlineState.generated
                                        : ArticleOutlineState.notGenerated),
                                onGenerate: isGen ? null : _generateArticles,
                                content: article.content,
                              );
                            }),
                          ],
                        );
                      },
                      loading: () => Container(
                        padding: const EdgeInsets.all(24),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: const Color(0xFFEEF0F5)),
                        ),
                        child: const Center(
                          child: CircularProgressIndicator(
                              color: Color(0xFF1E3A72)),
                        ),
                      ),
                      error: (e, _) => Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: const Color(0xFFEEF0F5)),
                        ),
                        child: const Text('تعذّر تحميل المقالات',
                            style: TextStyle(color: Color(0xFF5A6478))),
                      ),
                    );
                  },
                ),
              ],
            ),
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
                const Text('تعذّر تحميل تفاصيل الإحالة',
                    style: TextStyle(color: Color(0xFF5A6478))),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: () =>
                      ref.refresh(providerReferralDetailProvider(widget.referralId)),
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
                    ? const Icon(Icons.check, size: 10, color: Colors.white)
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
                            fontSize: 11, color: Color(0xFF8A93A6))),
                ],
              ),
            ),
          ),
        ],
      );
}
