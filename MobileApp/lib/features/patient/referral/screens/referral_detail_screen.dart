import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../features/patient/auth/providers/auth_provider.dart';

// ── Models ──────────────────────────────────────────────────────────────────

class ArticleItem {
  final String id;
  final String title;
  final String content;
  final String status; // Ready | Generating

  ArticleItem({
    required this.id,
    required this.title,
    required this.content,
    required this.status,
  });

  factory ArticleItem.fromJson(Map<String, dynamic> j) {
    return ArticleItem(
      id: j['articleId'] ?? j['id'] ?? '',
      title: j['title'] ?? j['articleTitle'] ?? j['heading'] ?? 'مقال طبي',
      content: j['content_ar'] ?? j['content'] ?? j['articleContent'] ?? j['body'] ?? '',
      status: j['status'] ?? j['generationStatus'] ?? 'Ready',
    );
  }

  bool get isReady => status == 'Ready';
}

class ReferralDetail {
  final String id;
  final String riskLevel;
  final String primaryDiagnosis;
  final String ageGroup;
  final ArticleItem? summaryArticle;
  final List<ArticleItem> stage2Articles;
  final String stage2Status;

  ReferralDetail({
    required this.id,
    required this.riskLevel,
    required this.primaryDiagnosis,
    required this.ageGroup,
    this.summaryArticle,
    required this.stage2Articles,
    required this.stage2Status,
  });

  factory ReferralDetail.fromJson(Map<String, dynamic> j) {
    final status = j['status'] as String? ?? '';

    // Stage 2 can only be triggered when Stage 1 is fully delivered.
    // Stage2Requested means it's queued in Hangfire — treat as Generating.
    String stage2Status;
    if (status == 'Stage2Complete') {
      stage2Status = 'Complete';
    } else if (status == 'Stage2Requested') {
      stage2Status = 'Generating';
    } else if (status == 'Stage1Complete' || status == 'Stage1Delivered') {
      stage2Status = 'NotRequested';
    } else {
      // Created or any other pre-Stage1 status — Stage 1 not ready yet
      stage2Status = 'Pending';
    }

    return ReferralDetail(
      id: j['referralId'] ?? j['id'] ?? '',
      riskLevel: j['riskLevel'] ?? 'LOW',
      primaryDiagnosis: j['patientProfile']?['primaryDiagnosis'] ??
        j['primaryDiagnosis'] ?? '',
      ageGroup: j['patientProfile']?['ageGroup'] ?? j['ageGroup'] ?? '',
      summaryArticle: null,
      stage2Articles: [],
      stage2Status: stage2Status,
    );
  }
}

// ── Providers ────────────────────────────────────────────────────────────────

final referralDetailProvider = FutureProvider.family<ReferralDetail, (String, String)>(
  (ref, params) async {
    final id = params.$1;
    final token = params.$2;

    final dio = Dio(BaseOptions(
      baseUrl: 'https://muafaplus1-production.up.railway.app/api/v1',
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    ));

    final response = await dio.get('/referrals/$id');
    print('DEBUG referral raw response: ${response.data}');
    return ReferralDetail.fromJson(response.data['data']);
  },
);

final stage2StatusProvider = StateProvider.family<String, String>(
  (ref, id) => 'NotRequested',
);

final referralArticlesProvider = FutureProvider.family<List<ArticleItem>, (String, String)>(
  (ref, params) async {
    final id = params.$1;
    final token = params.$2;

    final dio = Dio(BaseOptions(
      baseUrl: 'https://muafaplus1-production.up.railway.app/api/v1',
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    ));

    try {
      final response = await dio.get('/referrals/$id/articles');
      print('DEBUG articles response: ${response.data}');
      final list = response.data['data'] as List? ?? [];
      return list.map((a) => ArticleItem.fromJson(a)).toList();
    } catch (e) {
      print('DEBUG articles error: $e');
      return [];
    }
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

  bool _triggeringStage2 = false;

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

  Future<void> _triggerStage2() async {
    setState(() => _triggeringStage2 = true);
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
      await dio.post('/referrals/${widget.referralId}/stage2', data: {});
      ref.invalidate(referralDetailProvider((widget.referralId, token)));
      ref.invalidate(referralArticlesProvider((widget.referralId, token)));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('حدث خطأ، يرجى المحاولة مجدداً',
            style: GoogleFonts.ibmPlexSansArabic())));
      }
    } finally {
      if (mounted) setState(() => _triggeringStage2 = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final token = ref.watch(authProvider).token ?? '';
    print('DEBUG referralDetail: token=${token.isEmpty ? "EMPTY" : token.substring(0, 20)}, id=${widget.referralId}');
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
              fontWeight: FontWeight.w700, color: AppColors.white)),
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward, color: AppColors.white),
            onPressed: () => context.pop(),
          ),
        ),
        body: detailAsync.when(
          loading: () => const Center(child: CircularProgressIndicator(
            color: AppColors.navy600)),
          error: (e, _) => Center(child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text('تعذّر تحميل البيانات. يرجى المحاولة مجدداً',
              style: GoogleFonts.ibmPlexSansArabic(
                color: AppColors.ink500, fontSize: 15),
              textAlign: TextAlign.center),
          )),
          data: (detail) => _buildContent(detail),
        ),
      ),
    );
  }

  Widget _buildContent(ReferralDetail detail) {
    final token = ref.watch(authProvider).token ?? '';
    final articlesAsync = ref.watch(
      referralArticlesProvider((detail.id, token)));

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [

        // ── Hero card ────────────────────────────────────────────────────
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              begin: Alignment.topRight,
              end: Alignment.bottomLeft,
              colors: [AppColors.navy600, AppColors.navy800],
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
                    color: AppColors.white.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(12)),
                  child: const Icon(Icons.person_outline,
                    color: AppColors.white, size: 24),
                ),
                const SizedBox(width: 12),
                Expanded(child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(detail.primaryDiagnosis,
                      style: GoogleFonts.ibmPlexSansArabic(
                        fontSize: 16, fontWeight: FontWeight.w700,
                        color: AppColors.white)),
                    if (detail.ageGroup.isNotEmpty)
                      Text(detail.ageGroup,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 13,
                          color: AppColors.white.withOpacity(0.65))),
                  ],
                )),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12, vertical: 5),
                  decoration: BoxDecoration(
                    color: _riskBg(detail.riskLevel),
                    borderRadius: BorderRadius.circular(999)),
                  child: Text(_riskLabel(detail.riskLevel),
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 12, fontWeight: FontWeight.w700,
                      color: _riskColor(detail.riskLevel))),
                ),
              ]),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: AppColors.orange500.withOpacity(0.15),
                  borderRadius: BorderRadius.circular(8)),
                child: Row(mainAxisSize: MainAxisSize.min, children: [
                  const Icon(Icons.auto_awesome,
                    color: AppColors.orange500, size: 14),
                  const SizedBox(width: 6),
                  Text('مولَّد بواسطة الذكاء الاصطناعي',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 11, color: AppColors.orange500,
                      fontWeight: FontWeight.w600)),
                ]),
              ),
            ],
          ),
        ),

        const SizedBox(height: 16),

        // ── Articles (loaded from /articles endpoint) ──────────────────────
        articlesAsync.when(
          loading: () => const Center(child: Padding(
            padding: EdgeInsets.all(20),
            child: CircularProgressIndicator(color: AppColors.navy600))),
          error: (e, _) => const SizedBox(),
          data: (articles) {
            final summary = articles.isNotEmpty ? articles.first : null;
            final stage2 = articles.length > 1
              ? articles.sublist(1) : <ArticleItem>[];

            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (summary != null) ...[
                  _SectionHeader(
                    title: 'الملخص الصحي',
                    icon: Icons.article_outlined),
                  const SizedBox(height: 8),
                  _ArticleCard(
                    article: summary,
                    isStage1: true,
                    onTap: () => context.push('/article/${detail.id}/${summary.id}'),
                  ),
                  const SizedBox(height: 16),
                ],

                _SectionHeader(
                  title: 'المقالات التفصيلية',
                  icon: Icons.menu_book_outlined),
                const SizedBox(height: 8),

                if (detail.stage2Status == 'NotRequested')
                  _Stage2TriggerCard(
                    isLoading: _triggeringStage2,
                    onTap: _triggerStage2,
                  )
                else if (detail.stage2Status == 'Generating' ||
                         detail.stage2Status == 'Pending')
                  _GeneratingCard()
                else ...[
                  ...stage2.map((article) => _ArticleCard(
                    article: article,
                    isStage1: false,
                    onTap: article.isReady
                      ? () => context.push('/article/${detail.id}/${article.id}')
                      : null,
                  )),
                ],
              ],
            );
          },
        ),

        const SizedBox(height: 16),

        // ── Feedback button ───────────────────────────────────────────────
        SizedBox(
          width: double.infinity,
          height: 52,
          child: ElevatedButton.icon(
            onPressed: () => context.push('/feedback/${detail.id}'),
            icon: const Icon(Icons.rate_review_outlined, size: 20),
            label: Text('إنهاء وتقديم التقييم',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 15, fontWeight: FontWeight.w700)),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.navy600,
              foregroundColor: AppColors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
              elevation: 0),
          ),
        ),

        const SizedBox(height: 32),

        // ── Disclaimer ────────────────────────────────────────────────────
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppColors.orange500.withOpacity(0.08),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: AppColors.orange500.withOpacity(0.2)),
          ),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start,
            children: [
            const Icon(Icons.info_outline,
              color: AppColors.orange500, size: 16),
            const SizedBox(width: 8),
            Expanded(child: Text(
              'هذا المحتوى للتثقيف الصحي فقط ولا يُغني عن استشارة طبيبك',
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 11, color: AppColors.orange500, height: 1.5))),
          ]),
        ),

        const SizedBox(height: 16),
      ],
      ),
    );
  }
}

// ── Sub-widgets ───────────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  final String title;
  final IconData icon;
  const _SectionHeader({required this.title, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Icon(icon, color: AppColors.navy600, size: 18),
      const SizedBox(width: 8),
      Text(title, style: GoogleFonts.ibmPlexSansArabic(
        fontSize: 15, fontWeight: FontWeight.w700,
        color: AppColors.ink900)),
    ]);
  }
}

class _ArticleCard extends StatelessWidget {
  final ArticleItem article;
  final bool isStage1;
  final VoidCallback? onTap;
  const _ArticleCard({
    required this.article,
    required this.isStage1,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isGenerating = !article.isReady;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.ink100),
          boxShadow: const [BoxShadow(
            color: Color(0x0A0E1726), blurRadius: 8,
            offset: Offset(0, 2))],
        ),
        child: Row(children: [
          Container(
            width: 38, height: 38,
            decoration: BoxDecoration(
              color: isStage1
                ? AppColors.orange500.withOpacity(0.1)
                : AppColors.navy600.withOpacity(0.08),
              borderRadius: BorderRadius.circular(10)),
            child: Icon(
              isStage1 ? Icons.summarize_outlined : Icons.article_outlined,
              color: isStage1 ? AppColors.orange500 : AppColors.navy600,
              size: 18),
          ),
          const SizedBox(width: 12),
          Expanded(child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(article.title.isNotEmpty ? article.title : 'مقال طبي',
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 14, fontWeight: FontWeight.w600,
                  color: isGenerating
                    ? AppColors.ink400 : AppColors.ink900),
                maxLines: 2, overflow: TextOverflow.ellipsis),
              if (isGenerating) ...[
                const SizedBox(height: 4),
                Row(children: [
                  Container(
                    width: 6, height: 6,
                    decoration: const BoxDecoration(
                      color: AppColors.orange500,
                      shape: BoxShape.circle)),
                  const SizedBox(width: 6),
                  Text('جارٍ التوليد...',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 11, color: AppColors.orange500)),
                ]),
              ],
            ],
          )),
          if (!isGenerating && onTap != null)
            const Icon(Icons.chevron_left,
              color: AppColors.ink400, size: 20),
          if (isGenerating)
            const SizedBox(
              width: 16, height: 16,
              child: CircularProgressIndicator(
                color: AppColors.orange500, strokeWidth: 2)),
        ]),
      ),
    );
  }
}

class _Stage2TriggerCard extends StatelessWidget {
  final bool isLoading;
  final VoidCallback onTap;
  const _Stage2TriggerCard({required this.isLoading, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.ink100),
      ),
      child: Column(children: [
        Container(
          width: 48, height: 48,
          decoration: BoxDecoration(
            color: AppColors.navy600.withOpacity(0.08),
            borderRadius: BorderRadius.circular(14)),
          child: const Icon(Icons.menu_book_outlined,
            color: AppColors.navy600, size: 24),
        ),
        const SizedBox(height: 12),
        Text('اطّلع على المقالات التفصيلية',
          style: GoogleFonts.ibmPlexSansArabic(
            fontSize: 15, fontWeight: FontWeight.w700,
            color: AppColors.ink900),
          textAlign: TextAlign.center),
        const SizedBox(height: 6),
        Text('احصل على مقالات مفصّلة تشرح حالتك بعمق',
          style: GoogleFonts.ibmPlexSansArabic(
            fontSize: 13, color: AppColors.ink500, height: 1.5),
          textAlign: TextAlign.center),
        const SizedBox(height: 16),
        SizedBox(
          width: double.infinity, height: 46,
          child: ElevatedButton(
            onPressed: isLoading ? null : onTap,
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.navy600,
              foregroundColor: AppColors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
              elevation: 0),
            child: isLoading
              ? const SizedBox(width: 20, height: 20,
                  child: CircularProgressIndicator(
                    color: Colors.white, strokeWidth: 2))
              : Text('توليد المقالات التفصيلية',
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 14, fontWeight: FontWeight.w700)),
          ),
        ),
      ]),
    );
  }
}

class _GeneratingCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.ink100)),
      child: Row(children: [
        const SizedBox(
          width: 20, height: 20,
          child: CircularProgressIndicator(
            color: AppColors.orange500, strokeWidth: 2.5)),
        const SizedBox(width: 14),
        Expanded(child: Text('جارٍ توليد المقالات التفصيلية...',
          style: GoogleFonts.ibmPlexSansArabic(
            fontSize: 14, color: AppColors.ink700,
            fontWeight: FontWeight.w500))),
      ]),
    );
  }
}
