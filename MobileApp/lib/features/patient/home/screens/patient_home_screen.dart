import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/dio_client.dart';
import '../../auth/providers/auth_provider.dart';

// ── Model ─────────────────────────────────────────────────────────────────────

class ReferralSummary {
  final String id;
  final String riskLevel;
  final String status;
  final String primaryDiagnosis;
  final String createdAt;
  final int articleCount;

  ReferralSummary({
    required this.id,
    required this.riskLevel,
    required this.status,
    required this.primaryDiagnosis,
    required this.createdAt,
    required this.articleCount,
  });

  factory ReferralSummary.fromJson(Map<String, dynamic> j) => ReferralSummary(
        id: j['referralId'] ?? j['id'] ?? '',
        riskLevel: j['riskLevel'] ?? 'LOW',
        status: j['status'] ?? '',
        primaryDiagnosis:
            j['patientProfile']?['primaryDiagnosis'] ?? '',
        createdAt: j['createdAt'] ?? '',
        articleCount: j['articleCount'] ?? 0,
      );
}

// ── Provider ──────────────────────────────────────────────────────────────────

final referralsProvider = FutureProvider<List<ReferralSummary>>((ref) async {
  final dio = ref.watch(dioClientProvider).dio;
  final response = await dio.get(ApiConstants.referrals);
  final data = response.data['data'] as List? ?? [];
  return data.map((j) => ReferralSummary.fromJson(j)).toList();
});

// ── Helpers ───────────────────────────────────────────────────────────────────

Color _riskColor(String level) {
  switch (level.toUpperCase()) {
    case 'LOW':      return AppColors.riskLowText;
    case 'MODERATE': return AppColors.riskModText;
    case 'HIGH':     return AppColors.riskHighText;
    case 'CRITICAL': return AppColors.riskCritText;
    default:         return AppColors.riskLowText;
  }
}

Color _riskBg(String level) {
  switch (level.toUpperCase()) {
    case 'LOW':      return AppColors.riskLowBg;
    case 'MODERATE': return AppColors.riskModBg;
    case 'HIGH':     return AppColors.riskHighBg;
    case 'CRITICAL': return AppColors.riskCritBg;
    default:         return AppColors.riskLowBg;
  }
}

String _riskLabel(String level) {
  switch (level.toUpperCase()) {
    case 'LOW':      return 'منخفض';
    case 'MODERATE': return 'متوسط';
    case 'HIGH':     return 'مرتفع';
    case 'CRITICAL': return 'حرج';
    default:         return level;
  }
}

// ── Screen ────────────────────────────────────────────────────────────────────

class PatientHomeScreen extends ConsumerWidget {
  const PatientHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState      = ref.watch(authProvider);
    final referralsAsync = ref.watch(referralsProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        body: Column(
          children: [
            // Navy gradient header
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.navy700, AppColors.navy800],
                ),
                borderRadius: BorderRadius.vertical(
                    bottom: Radius.circular(24)),
              ),
              child: SafeArea(
                bottom: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              AppStrings.homeGreeting,
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 14,
                                color: AppColors.white.withOpacity(0.65)),
                            ),
                            Text(
                              authState.phoneNumber ?? '',
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                                color: AppColors.white),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              AppStrings.homeSubtitle,
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 13,
                                color: AppColors.white.withOpacity(0.55)),
                            ),
                          ],
                        ),
                      ),
                      // Notification bell
                      Container(
                        width: 40,
                        height: 40,
                        decoration: BoxDecoration(
                          color: AppColors.white.withOpacity(0.12),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            const Icon(Icons.notifications_outlined,
                                color: AppColors.white, size: 22),
                            Positioned(
                              top: 8,
                              left: 8,
                              child: Container(
                                width: 8,
                                height: 8,
                                decoration: const BoxDecoration(
                                  color: AppColors.orange500,
                                  shape: BoxShape.circle,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),

            // Referrals list
            Expanded(
              child: referralsAsync.when(
                loading: () => const Center(
                  child: CircularProgressIndicator(
                      color: AppColors.navy600)),
                error: (e, _) => Center(
                  child: Text(
                    'حدث خطأ في تحميل البيانات',
                    style: GoogleFonts.ibmPlexSansArabic(
                        color: AppColors.ink500))),
                data: (referrals) {
                  if (referrals.isEmpty) {
                    return Center(
                      child: Text(
                        AppStrings.noReferrals,
                        style: GoogleFonts.ibmPlexSansArabic(
                            color: AppColors.ink400, fontSize: 15)));
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: referrals.length,
                    itemBuilder: (ctx, i) {
                      final r = referrals[i];
                      return _ReferralCard(
                        referral: r,
                        riskColor: _riskColor(r.riskLevel),
                        riskBg: _riskBg(r.riskLevel),
                        riskLabel: _riskLabel(r.riskLevel),
                        onTap: () => context.push('/referral/${r.id}'),
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),

        // Bottom nav
        bottomNavigationBar: Container(
          decoration: const BoxDecoration(
            color: AppColors.white,
            border: Border(
                top: BorderSide(color: AppColors.ink100)),
          ),
          child: SafeArea(
            top: false,
            child: SizedBox(
              height: 60,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  const _NavItem(
                      icon: Icons.home_outlined,
                      label: 'الرئيسية',
                      active: true),
                  const _NavItem(
                      icon: Icons.article_outlined, label: 'مقالاتي'),
                  const _NavItem(
                      icon: Icons.chat_bubble_outline, label: 'اسأل'),
                  _NavItem(
                    icon: Icons.logout_outlined,
                    label: AppStrings.logout,
                    onTap: () =>
                        ref.read(authProvider.notifier).logout(),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Referral card ─────────────────────────────────────────────────────────────

class _ReferralCard extends StatelessWidget {
  final ReferralSummary referral;
  final Color riskColor;
  final Color riskBg;
  final String riskLabel;
  final VoidCallback onTap;

  const _ReferralCard({
    required this.referral,
    required this.riskColor,
    required this.riskBg,
    required this.riskLabel,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppColors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.ink100),
          boxShadow: const [
            BoxShadow(
              color: Color(0x0F0E1726),
              blurRadius: 20,
              offset: Offset(0, 4)),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: AppColors.orange500.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.auto_awesome,
                      color: AppColors.orange500, size: 20),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        referral.primaryDiagnosis.isNotEmpty
                            ? referral.primaryDiagnosis
                            : 'إحالة طبية',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 15,
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink900),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        AppStrings.aiGenerated,
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontSize: 11,
                          color: AppColors.orange500,
                          fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                ),
                // Risk badge
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: riskBg,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    riskLabel,
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                      color: riskColor),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            SizedBox(
              width: double.infinity,
              height: 40,
              child: ElevatedButton(
                onPressed: onTap,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.navy600,
                  foregroundColor: AppColors.white,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10)),
                  elevation: 0,
                ),
                child: Text(
                  AppStrings.viewContent,
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 13, fontWeight: FontWeight.w600)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Bottom nav item ───────────────────────────────────────────────────────────

class _NavItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool active;
  final VoidCallback? onTap;

  const _NavItem({
    required this.icon,
    required this.label,
    this.active = false,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon,
              color: active ? AppColors.navy600 : AppColors.ink400,
              size: 24),
          const SizedBox(height: 2),
          Text(
            label,
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 10,
              fontWeight: FontWeight.w500,
              color: active ? AppColors.navy600 : AppColors.ink400),
          ),
          if (active)
            Container(
              margin: const EdgeInsets.only(top: 3),
              width: 16,
              height: 2,
              decoration: BoxDecoration(
                color: AppColors.navy600,
                borderRadius: BorderRadius.circular(2)),
            ),
        ],
      ),
    );
  }
}
