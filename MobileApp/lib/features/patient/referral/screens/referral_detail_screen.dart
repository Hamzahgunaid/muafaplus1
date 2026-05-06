import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/models/referral_article.dart';
import '../../../../core/widgets/article_outline_card.dart';
import '../../../../features/patient/auth/providers/auth_provider.dart';

const _base = 'https://muafaplus1-production.up.railway.app/api/v1';

// ── Models ──────────────────────────────────────────────────────────────────

class ReferralDetail {
  final String id;
  final String riskLevel;
  final String primaryDiagnosis;
  final String ageGroup;
  final String stage2Status;

  ReferralDetail({
    required this.id,
    required this.riskLevel,
    required this.primaryDiagnosis,
    required this.ageGroup,
    required this.stage2Status,
  });

  factory ReferralDetail.fromJson(Map<String, dynamic> j) {
    final status = j['status'] as String? ?? '';
    String stage2Status;
    if (status == 'Stage2Complete') {
      stage2Status = 'Complete';
    } else if (status == 'Stage2Requested') {
      stage2Status = 'Generating';
    } else if (status == 'Stage1Complete' || status == 'Stage1Delivered') {
      stage2Status = 'NotRequested';
    } else {
      stage2Status = 'Pending';
    }
    return ReferralDetail(
      id: j['referralId'] ?? j['id'] ?? '',
      riskLevel: j['riskLevel'] ?? 'LOW',
      primaryDiagnosis: j['patientProfile']?['primaryDiagnosis'] ??
          j['primaryDiagnosis'] ?? '',
      ageGroup:
          j['patientProfile']?['ageGroup'] ?? j['ageGroup'] ?? '',
      stage2Status: stage2Status,
    );
  }
}

// ── Providers ────────────────────────────────────────────────────────────────

final referralDetailProvider =
    FutureProvider.family<ReferralDetail, (String, String)>(
  (ref, params) async {
    final dio = Dio(BaseOptions(
      baseUrl: _base,
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${params.$2}',
      },
    ));
    final response = await dio.get('/referrals/${params.$1}');
    return ReferralDetail.fromJson(response.data['data']);
  },
);

// ── Screen ───────────────────────────────────────────────────────────────────

class ReferralDetailScreen extends ConsumerStatefulWidget {
  final String referralId;
  const ReferralDetailScreen({super.key, required this.referralId});

  @override
  ConsumerState<ReferralDetailScreen> createState() =>
      _ReferralDetailScreenState();
}

class _ReferralDetailScreenState
    extends ConsumerState<ReferralDetailScreen> {
  List<ReferralArticle> _articles = [];
  bool _articlesLoading = true;
  bool _triggeringStage2 = false;
  bool _stage2Requested = false;
  bool _articlesLoadTriggered = false;

  Future<void> _loadArticles(String token) async {
    try {
      final dio = Dio(BaseOptions(
        baseUrl: _base,
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));
      final response =
          await dio.get('/referrals/${widget.referralId}/articles');
      final list = response.data['data'] as List? ?? [];
      if (mounted) {
        setState(() {
          _articles = list
              .map((a) =>
                  ReferralArticle.fromJson(a as Map<String, dynamic>))
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
      final dio = Dio(BaseOptions(
        baseUrl: _base,
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));
      await dio.post('/referrals/${widget.referralId}/stage2', data: {});
      if (mounted) {
        setState(() {
          _stage2Requested = true;
          _triggeringStage2 = false;
        });
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text('حدث خطأ، يرجى المحاولة مجدداً',
                  style: GoogleFonts.ibmPlexSansArabic())));
        setState(() => _triggeringStage2 = false);
      }
    }
  }

  Color _riskColor(String l) {
    switch (l.toUpperCase()) {
      case 'LOW':      return AppColors.riskLowText;
      case 'MODERATE': return AppColors.riskModText;
      case 'HIGH':     return AppColors.riskHighText;
      case 'CRITICAL': return AppColors.riskCritText;
      default:         return AppColors.riskLowText;
    }
  }

  Color _riskBg(String l) {
    switch (l.toUpperCase()) {
      case 'LOW':      return AppColors.riskLowBg;
      case 'MODERATE': return AppColors.riskModBg;
      case 'HIGH':     return AppColors.riskHighBg;
      case 'CRITICAL': return AppColors.riskCritBg;
      default:         return AppColors.riskLowBg;
    }
  }

  String _riskLabel(String l) {
    switch (l.toUpperCase()) {
      case 'LOW':      return 'منخفض';
      case 'MODERATE': return 'متوسط';
      case 'HIGH':     return 'مرتفع';
      case 'CRITICAL': return 'حرج';
      default:         return l;
    }
  }

  Widget _buildArticleContent(ReferralDetail detail, String token) {
    // Stage 1 not yet complete
    if (detail.stage2Status == 'Pending') {
      return Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: AppColors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.ink100),
        ),
        child: Row(children: [
          const SizedBox(
            width: 20, height: 20,
            child: CircularProgressIndicator(
                strokeWidth: 2, color: AppColors.navy600),
          ),
          const SizedBox(width: 14),
          Text('جاري توليد المحتوى الصحي...',
              style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 14, color: AppColors.ink500)),
        ]),
      );
    }

    // Articles loading
    if (_articlesLoading) {
      return const Padding(
        padding: EdgeInsets.all(24),
        child: Center(
            child:
                CircularProgressIndicator(color: AppColors.navy600)),
      );
    }

    final summaries =
        _articles.where((a) => a.isSummary).toList();
    final detailed =
        _articles.where((a) => a.isDetailed).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Summary section
        if (summaries.isNotEmpty) ...[
          _SectionHeader(
              title: 'الملخص الصحي',
              icon: Icons.article_outlined),
          const SizedBox(height: 8),
          ...summaries.asMap().entries.map((e) => ArticleOutlineCard(
                key: ValueKey('sum_${e.value.articleId}'),
                index: e.key + 1,
                title: e.value.title.isNotEmpty
                    ? e.value.title
                    : 'الملخص الصحي',
                state: ArticleOutlineState.generated,
                content: e.value.contentAr,
                // onView: null → inline expand
              )),
          const SizedBox(height: 16),
        ],

        // Detailed section
        _SectionHeader(
            title: 'المقالات التفصيلية',
            icon: Icons.menu_book_outlined),
        const SizedBox(height: 8),

        if (detailed.isEmpty &&
            detail.stage2Status == 'NotRequested' &&
            !_stage2Requested &&
            !_triggeringStage2)
          ArticleOutlineCard(
            index: 1,
            title: 'المقالات التفصيلية',
            state: ArticleOutlineState.notGenerated,
            onGenerate: () => _triggerStage2(token),
          )
        else if (detailed.isEmpty &&
            (detail.stage2Status == 'Generating' ||
                _stage2Requested ||
                _triggeringStage2))
          const ArticleOutlineCard(
            index: 1,
            title: 'المقالات التفصيلية',
            state: ArticleOutlineState.generating,
          )
        else ...[
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
    final token = ref.watch(authProvider).token ?? '';
    final detailAsync =
        ref.watch(referralDetailProvider((widget.referralId, token)));

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          elevation: 0,
          title: Text('تفاصيل الإحالة',
              style: GoogleFonts.ibmPlexSansArabic(
                  fontWeight: FontWeight.w700,
                  color: AppColors.white)),
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward,
                color: AppColors.white),
            onPressed: () => context.pop(),
          ),
        ),
        body: detailAsync.when(
          loading: () => const Center(
              child: CircularProgressIndicator(
                  color: AppColors.navy600)),
          error: (e, _) => Center(
              child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(
                'تعذّر تحميل البيانات. يرجى المحاولة مجدداً',
                style: GoogleFonts.ibmPlexSansArabic(
                    color: AppColors.ink500, fontSize: 15),
                textAlign: TextAlign.center),
          )),
          data: (detail) {
            // Trigger article load once when detail is available
            if (!_articlesLoadTriggered) {
              _articlesLoadTriggered = true;
              WidgetsBinding.instance.addPostFrameCallback((_) {
                if (!mounted) return;
                if (detail.stage2Status == 'Pending') {
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
                  // ── Hero card ────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        begin: Alignment.topRight,
                        end: Alignment.bottomLeft,
                        colors: [
                          AppColors.navy600,
                          AppColors.navy800
                        ],
                      ),
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(children: [
                          Container(
                            width: 44, height: 44,
                            decoration: BoxDecoration(
                                color:
                                    AppColors.white.withOpacity(0.15),
                                borderRadius:
                                    BorderRadius.circular(12)),
                            child: const Icon(Icons.person_outline,
                                color: AppColors.white, size: 24),
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
                                    : 'إحالة طبية',
                                style: GoogleFonts.ibmPlexSansArabic(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.white),
                              ),
                              if (detail.ageGroup.isNotEmpty)
                                Text(detail.ageGroup,
                                    style:
                                        GoogleFonts.ibmPlexSansArabic(
                                            fontSize: 13,
                                            color: AppColors.white
                                                .withOpacity(0.65))),
                            ],
                          )),
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 12, vertical: 5),
                            decoration: BoxDecoration(
                                color: _riskBg(detail.riskLevel),
                                borderRadius:
                                    BorderRadius.circular(999)),
                            child: Text(
                                _riskLabel(detail.riskLevel),
                                style: GoogleFonts.ibmPlexSansArabic(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w700,
                                    color:
                                        _riskColor(detail.riskLevel))),
                          ),
                        ]),
                        const SizedBox(height: 12),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 10, vertical: 5),
                          decoration: BoxDecoration(
                              color:
                                  AppColors.orange500.withOpacity(0.15),
                              borderRadius: BorderRadius.circular(8)),
                          child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(Icons.auto_awesome,
                                    color: AppColors.orange500,
                                    size: 14),
                                const SizedBox(width: 6),
                                Text('مولَّد بواسطة الذكاء الاصطناعي',
                                    style:
                                        GoogleFonts.ibmPlexSansArabic(
                                            fontSize: 11,
                                            color: AppColors.orange500,
                                            fontWeight:
                                                FontWeight.w600)),
                              ]),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 16),

                  // ── Article content ───────────────────────────────
                  _buildArticleContent(detail, token),

                  const SizedBox(height: 16),

                  // ── Feedback button ───────────────────────────────
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: ElevatedButton.icon(
                      onPressed: () =>
                          context.push('/feedback/${detail.id}'),
                      icon: const Icon(Icons.rate_review_outlined,
                          size: 20),
                      label: Text('إنهاء وتقديم التقييم',
                          style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 15,
                              fontWeight: FontWeight.w700)),
                      style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.navy600,
                          foregroundColor: AppColors.white,
                          shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12)),
                          elevation: 0),
                    ),
                  ),

                  const SizedBox(height: 32),

                  // ── Disclaimer ────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.orange500.withOpacity(0.08),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                          color: AppColors.orange500.withOpacity(0.2)),
                    ),
                    child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Icons.info_outline,
                              color: AppColors.orange500, size: 16),
                          const SizedBox(width: 8),
                          Expanded(
                              child: Text(
                            'هذا المحتوى للتثقيف الصحي فقط ولا يُغني عن استشارة طبيبك',
                            style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 11,
                                color: AppColors.orange500,
                                height: 1.5),
                          )),
                        ]),
                  ),

                  const SizedBox(height: 16),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

// ── Helper widgets ────────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  final String title;
  final IconData icon;
  const _SectionHeader({required this.title, required this.icon});

  @override
  Widget build(BuildContext context) => Row(children: [
        Icon(icon, color: AppColors.navy600, size: 18),
        const SizedBox(width: 8),
        Text(title,
            style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 15,
                fontWeight: FontWeight.w700,
                color: AppColors.ink900)),
      ]);
}
