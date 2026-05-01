import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';
import '../providers/physician_auth_provider.dart';
import '../providers/referrals_provider.dart';
import '../models/physician_model.dart';

class ProviderDashboardScreen extends ConsumerWidget {
  const ProviderDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(physicianAuthProvider);
    final referralsAsync = ref.watch(recentReferralsProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        body: RefreshIndicator(
          onRefresh: () => ref.refresh(recentReferralsProvider.future),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              // ── Navy header ───────────────────────────────────────────────
              SliverToBoxAdapter(
                child: Container(
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [Color(0xFF1E3A72), Color(0xFF283481)],
                    ),
                  ),
                  child: SafeArea(
                    bottom: false,
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              // Status badge
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10, vertical: 4),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF21A740).withOpacity(0.2),
                                  borderRadius: BorderRadius.circular(20),
                                  border: Border.all(
                                    color: const Color(0xFF21A740).withOpacity(0.4)),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Container(
                                      width: 6, height: 6,
                                      decoration: const BoxDecoration(
                                        color: Color(0xFF21A740),
                                        shape: BoxShape.circle,
                                      ),
                                    ),
                                    const SizedBox(width: 6),
                                    Text('متصل',
                                      style: GoogleFonts.ibmPlexSansArabic(
                                        fontSize: 11,
                                        fontWeight: FontWeight.w600,
                                        color: const Color(0xFF21A740))),
                                  ],
                                ),
                              ),
                              // Logout
                              IconButton(
                                icon: const Icon(Icons.logout,
                                  color: Colors.white70, size: 20),
                                tooltip: 'تسجيل الخروج',
                                onPressed: () async {
                                  await ref.read(physicianAuthProvider.notifier).logout();
                                  if (context.mounted) context.go('/login');
                                },
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Text(auth.fullName ?? 'الطبيب',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 22,
                              fontWeight: FontWeight.w700,
                              color: Colors.white)),
                          if ((auth.specialty ?? '').isNotEmpty) ...[
                            const SizedBox(height: 4),
                            Text(auth.specialty!,
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 13,
                                color: Colors.white60)),
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
              ),

              // ── Body ─────────────────────────────────────────────────────
              SliverPadding(
                padding: const EdgeInsets.all(16),
                sliver: SliverList(
                  delegate: SliverChildListDelegate([

                    // New referral button
                    SizedBox(
                      height: 52,
                      child: ElevatedButton.icon(
                        onPressed: () => context.push('/provider/referrals/new'),
                        icon: const Icon(Icons.add, color: Colors.white, size: 20),
                        label: Text('إحالة مريض جديد',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: Colors.white)),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF283481),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                          elevation: 0,
                        ),
                      ),
                    ),

                    const SizedBox(height: 16),

                    // ── Stat cards ──────────────────────────────────────────
                    referralsAsync.when(
                      data: (referrals) => _StatsGrid(referrals: referrals),
                      loading: () => const SizedBox(
                        height: 100,
                        child: Center(child: CircularProgressIndicator(
                          color: Color(0xFF283481)))),
                      error: (_, __) => const SizedBox.shrink(),
                    ),

                    const SizedBox(height: 16),

                    // ── Risk distribution ────────────────────────────────────
                    referralsAsync.when(
                      data: (referrals) => _RiskDistributionCard(referrals: referrals),
                      loading: () => const SizedBox.shrink(),
                      error: (_, __) => const SizedBox.shrink(),
                    ),

                    const SizedBox(height: 16),

                    // ── AI activity card ─────────────────────────────────────
                    referralsAsync.when(
                      data: (referrals) => _AIActivityCard(referrals: referrals),
                      loading: () => const SizedBox.shrink(),
                      error: (_, __) => const SizedBox.shrink(),
                    ),

                    const SizedBox(height: 16),

                    // ── Recent referrals header ──────────────────────────────
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        TextButton(
                          onPressed: () => context.push('/provider/referrals'),
                          child: Text('عرض الكل',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: const Color(0xFF283481))),
                        ),
                        Text('الإحالات الأخيرة',
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 16,
                            fontWeight: FontWeight.w700,
                            color: const Color(0xFF0E1726))),
                      ],
                    ),

                    const SizedBox(height: 8),

                    // ── Referral list ────────────────────────────────────────
                    referralsAsync.when(
                      data: (referrals) => referrals.isEmpty
                        ? const Padding(
                            padding: EdgeInsets.symmetric(vertical: 32),
                            child: Center(child: Text('لا توجد إحالات بعد',
                              style: TextStyle(color: Color(0xFF5A6478)))))
                        : Column(
                            children: referrals.take(5)
                              .map((r) => _ReferralCard(referral: r))
                              .toList()),
                      loading: () => const Padding(
                        padding: EdgeInsets.symmetric(vertical: 32),
                        child: Center(child: CircularProgressIndicator(
                          color: Color(0xFF283481)))),
                      error: (e, _) => const Padding(
                        padding: EdgeInsets.symmetric(vertical: 16),
                        child: Text('خطأ في تحميل الإحالات',
                          style: TextStyle(color: Color(0xFFD64545)),
                          textAlign: TextAlign.right)),
                    ),

                    const SizedBox(height: 32),
                  ]),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Stats grid ────────────────────────────────────────────────────────────────

class _StatsGrid extends StatelessWidget {
  final List<ReferralSummary> referrals;
  const _StatsGrid({required this.referrals});

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final total = referrals.length;
    final activeThisMonth = referrals
      .where((r) => r.createdAt.month == now.month && r.createdAt.year == now.year)
      .length;
    final viaWhatsApp = referrals.where((r) => r.whatsAppDelivery).length;
    final completed = referrals
      .where((r) => r.status.toLowerCase() == 'completed')
      .length;

    return Column(
      children: [
        IntrinsicHeight(
          child: Row(
            children: [
              _StatCard(
                label: 'إجمالي الإحالات',
                value: total.toString(),
                icon: Icons.people_alt_outlined,
                color: const Color(0xFF283481)),
              const SizedBox(width: 12),
              _StatCard(
                label: 'هذا الشهر',
                value: activeThisMonth.toString(),
                icon: Icons.calendar_today_outlined,
                color: const Color(0xFF355BA7)),
            ],
          ),
        ),
        const SizedBox(height: 12),
        IntrinsicHeight(
          child: Row(
            children: [
              _StatCard(
                label: 'عبر واتساب',
                value: viaWhatsApp.toString(),
                icon: Icons.chat_outlined,
                color: const Color(0xFF21A740)),
              const SizedBox(width: 12),
              _StatCard(
                label: 'مكتملة',
                value: completed.toString(),
                icon: Icons.check_circle_outline,
                color: const Color(0xFFDC6B20)),
            ],
          ),
        ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) => Expanded(
    child: ConstrainedBox(
      constraints: const BoxConstraints(maxHeight: 100),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFEEF0F5)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Container(
                  width: 32, height: 32,
                  decoration: BoxDecoration(
                    color: color.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8)),
                  child: Icon(icon, color: color, size: 16),
                ),
                Text(value,
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 24,
                    fontWeight: FontWeight.w700,
                    color: color)),
              ],
            ),
            const SizedBox(height: 6),
            Text(label,
              style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 11,
                color: const Color(0xFF5A6478)),
              textAlign: TextAlign.right),
          ],
        ),
      ),
    ),
  );
}

// ── Risk distribution card ────────────────────────────────────────────────────

class _RiskDistributionCard extends StatelessWidget {
  final List<ReferralSummary> referrals;
  const _RiskDistributionCard({required this.referrals});

  static const _levels = [
    {'key': 'CRITICAL', 'label': 'حرج',     'color': Color(0xFFC0392B)},
    {'key': 'HIGH',     'label': 'مرتفع',   'color': Color(0xFFDC6B20)},
    {'key': 'MODERATE', 'label': 'متوسط',   'color': Color(0xFF355BA7)},
    {'key': 'LOW',      'label': 'منخفض',   'color': Color(0xFF21A740)},
  ];

  @override
  Widget build(BuildContext context) {
    final total = referrals.isEmpty ? 1 : referrals.length;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFEEF0F5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text('توزيع مستوى الخطر',
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 14,
              fontWeight: FontWeight.w700,
              color: const Color(0xFF0E1726))),
          const SizedBox(height: 12),
          // Stacked bar
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: SizedBox(
              height: 12,
              child: Row(
                children: _levels.map((lvl) {
                  final count = referrals
                    .where((r) => r.riskLevel == lvl['key'] as String)
                    .length;
                  final fraction = count / total;
                  return Flexible(
                    flex: (fraction * 1000).round().clamp(1, 1000),
                    child: Container(color: lvl['color'] as Color),
                  );
                }).toList(),
              ),
            ),
          ),
          const SizedBox(height: 12),
          // Legend
          Wrap(
            spacing: 16,
            runSpacing: 8,
            children: _levels.map((lvl) {
              final count = referrals
                .where((r) => r.riskLevel == lvl['key'] as String)
                .length;
              return Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('$count ${lvl['label']}',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 12,
                      color: const Color(0xFF5A6478))),
                  const SizedBox(width: 4),
                  Container(
                    width: 10, height: 10,
                    decoration: BoxDecoration(
                      color: lvl['color'] as Color,
                      shape: BoxShape.circle,
                    ),
                  ),
                ],
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}

// ── AI activity card ──────────────────────────────────────────────────────────

class _AIActivityCard extends StatelessWidget {
  final List<ReferralSummary> referrals;
  const _AIActivityCard({required this.referrals});

  @override
  Widget build(BuildContext context) {
    final withContent = referrals.length;
    final viaWa = referrals.where((r) => r.whatsAppDelivery).length;
    final waRate = referrals.isEmpty ? 0.0 : viaWa / referrals.length;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [Color(0xFF1A2759), Color(0xFF0E1726)],
        ),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: const Color(0xFF21A740).withOpacity(0.2),
                  borderRadius: BorderRadius.circular(8)),
                child: Text('نشط',
                  style: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    color: const Color(0xFF21A740))),
              ),
              Text('نشاط الذكاء الاصطناعي',
                style: GoogleFonts.ibmPlexSansArabic(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: Colors.white)),
            ],
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text('محتوى مُولَّد',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 11, color: Colors.white54)),
                  Text('$withContent جلسة',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: Colors.white)),
                ],
              ),
              const SizedBox(width: 24),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text('معدل واتساب',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 11, color: Colors.white54)),
                  Text('${(waRate * 100).round()}%',
                    style: GoogleFonts.ibmPlexSansArabic(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: const Color(0xFF21A740))),
                ],
              ),
            ],
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: waRate,
              minHeight: 6,
              backgroundColor: Colors.white12,
              valueColor: const AlwaysStoppedAnimation(Color(0xFF21A740)),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Referral card ─────────────────────────────────────────────────────────────

class _ReferralCard extends StatelessWidget {
  final ReferralSummary referral;
  const _ReferralCard({required this.referral});

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
    final color = _riskColors[referral.riskLevel] ?? const Color(0xFF5A6478);
    final label = _riskLabels[referral.riskLevel] ?? referral.riskLevel;

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFEEF0F5)),
      ),
      child: Column(
        children: [
          // Colored accent bar
          Container(
            height: 4,
            decoration: BoxDecoration(
              color: color,
              borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                // Risk badge
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: color.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(6)),
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
                      Text(referral.primaryDiagnosis.isNotEmpty
                          ? referral.primaryDiagnosis : 'إحالة طبية',
                        style: GoogleFonts.ibmPlexSansArabic(
                          fontWeight: FontWeight.w600,
                          color: const Color(0xFF0E1726)),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis),
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
    );
  }
}
