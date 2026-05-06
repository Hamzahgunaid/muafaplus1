import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../provider/providers/physician_auth_provider.dart';
import '../../provider/providers/referrals_provider.dart';
import '../../provider/models/physician_model.dart';

class AssistantDashboardScreen extends ConsumerWidget {
  const AssistantDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(physicianAuthProvider);
    final referralsAsync = ref.watch(recentReferralsProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        body: Column(
          children: [
            // ── Navy header ──────────────────────────────────────────────
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [Color(0xFF1E3A72), Color(0xFF283481)],
                ),
              ),
              child: SafeArea(
                bottom: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('مرحباً',
                                style: GoogleFonts.ibmPlexSansArabic(
                                    fontSize: 14, color: Colors.white70)),
                            Text(auth.fullName ?? 'المساعد',
                                style: GoogleFonts.ibmPlexSansArabic(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: Colors.white)),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.logout,
                            color: Colors.white70),
                        onPressed: () async {
                          await ref
                              .read(physicianAuthProvider.notifier)
                              .logout();
                          if (context.mounted) {
                            context.go('/provider/login');
                          }
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ),

            // ── Body ─────────────────────────────────────────────────────
            Expanded(
              child: RefreshIndicator(
                onRefresh: () =>
                    ref.refresh(recentReferralsProvider.future),
                child: SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // ── Create referral button ──────────────────────
                      SizedBox(
                        height: 52,
                        child: ElevatedButton.icon(
                          onPressed: () =>
                              context.push('/provider/referrals/new'),
                          icon: const Icon(Icons.add, color: Colors.white),
                          label: Text('إحالة مريض جديد',
                              style: GoogleFonts.ibmPlexSansArabic(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                  color: Colors.white)),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFF1E3A72),
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12)),
                            elevation: 0,
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),

                      // ── Total count card ────────────────────────────
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          border:
                              Border.all(color: const Color(0xFFEEF0F5)),
                        ),
                        child: Row(children: [
                          Container(
                            width: 44, height: 44,
                            decoration: BoxDecoration(
                              color: const Color(0xFFEEF1F7),
                              borderRadius: BorderRadius.circular(10)),
                            child: const Icon(
                                Icons.folder_shared_outlined,
                                color: Color(0xFF1E3A72), size: 22),
                          ),
                          const SizedBox(width: 14),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('إجمالي إحالاتك',
                                  style: GoogleFonts.ibmPlexSansArabic(
                                      fontSize: 12,
                                      color: const Color(0xFF8A93A6))),
                              referralsAsync.when(
                                loading: () => Text('...',
                                    style: GoogleFonts.ibmPlexSansArabic(
                                        fontSize: 24,
                                        fontWeight: FontWeight.w700,
                                        color: const Color(0xFF0E1726))),
                                error: (_, __) => Text('—',
                                    style: GoogleFonts.ibmPlexSansArabic(
                                        fontSize: 24,
                                        fontWeight: FontWeight.w700,
                                        color: const Color(0xFF0E1726))),
                                data: (r) => Text('${r.length}',
                                    style: GoogleFonts.ibmPlexSansArabic(
                                        fontSize: 24,
                                        fontWeight: FontWeight.w700,
                                        color: const Color(0xFF0E1726))),
                              ),
                            ],
                          ),
                        ]),
                      ),
                      const SizedBox(height: 20),

                      // ── Recent referrals ────────────────────────────
                      Text('الإحالات الأخيرة',
                          style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: const Color(0xFF0E1726))),
                      const SizedBox(height: 10),

                      referralsAsync.when(
                        loading: () => const Center(
                            child: Padding(
                          padding: EdgeInsets.all(24),
                          child: CircularProgressIndicator(
                              color: Color(0xFF1E3A72)),
                        )),
                        error: (_, __) => Center(
                            child: Text('تعذّر تحميل الإحالات',
                                style: GoogleFonts.ibmPlexSansArabic(
                                    color: const Color(0xFF5A6478)))),
                        data: (referrals) {
                          if (referrals.isEmpty) {
                            return Center(
                              child: Padding(
                                padding: const EdgeInsets.symmetric(
                                    vertical: 24),
                                child: Text('لا توجد إحالات بعد',
                                    style: GoogleFonts.ibmPlexSansArabic(
                                        color: const Color(0xFF8A93A6))),
                              ),
                            );
                          }
                          final recent = referrals.take(5).toList();
                          return Column(
                            children: recent
                                .map((r) =>
                                    _AssistantReferralCard(referral: r))
                                .toList(),
                          );
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
        bottomNavigationBar: BottomNavigationBar(
          currentIndex: 0,
          onTap: (i) {
            if (i == 1) context.go('/assistant/referrals');
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
          ],
        ),
      ),
    );
  }
}

// ── Referral card (inlined — _ReferralCard in dashboard_screen is private) ──

class _AssistantReferralCard extends StatelessWidget {
  final ReferralSummary referral;
  const _AssistantReferralCard({required this.referral});

  static const _riskColors = {
    'LOW':      Color(0xFF21A740),
    'MODERATE': Color(0xFF355BA7),
    'HIGH':     Color(0xFFDC6B20),
    'CRITICAL': Color(0xFFC0392B),
  };
  static const _riskLabels = {
    'LOW': 'منخفض', 'MODERATE': 'متوسط',
    'HIGH': 'مرتفع', 'CRITICAL': 'حرج',
  };

  String _formatDate(DateTime dt) {
    final diff = DateTime.now().difference(dt);
    if (diff.inDays > 0) return 'منذ ${diff.inDays} يوم';
    if (diff.inHours > 0) return 'منذ ${diff.inHours} ساعة';
    return 'منذ قليل';
  }

  @override
  Widget build(BuildContext context) {
    final color =
        _riskColors[referral.riskLevel] ?? const Color(0xFF5A6478);
    final label = _riskLabels[referral.riskLevel] ?? referral.riskLevel;

    return GestureDetector(
      onTap: () => context
          .push('/provider/referrals/${referral.referralId}'),
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Column(
          children: [
            Container(
              height: 4,
              decoration: BoxDecoration(
                color: color,
                borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(12)),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: color.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(label,
                        style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: color)),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          referral.primaryDiagnosis.isNotEmpty
                              ? referral.primaryDiagnosis
                              : 'إحالة طبية',
                          style: GoogleFonts.ibmPlexSansArabic(
                              fontWeight: FontWeight.w600,
                              color: const Color(0xFF0E1726)),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        Text(_formatDate(referral.createdAt),
                            style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 11,
                                color: const Color(0xFF8A93A6))),
                      ],
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Icon(Icons.chevron_left,
                      color: Color(0xFF8A93A6), size: 20),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
