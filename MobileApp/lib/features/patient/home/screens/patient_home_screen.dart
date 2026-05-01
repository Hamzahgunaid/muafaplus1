import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_colors.dart';
import '../../auth/providers/auth_provider.dart';

// ─── Model ────────────────────────────────────────────────
class ReferralSummary {
  final String id;
  final String status;
  final String riskLevel;
  final String primaryDiagnosis;
  final String createdAt;
  final bool hasStage2;

  ReferralSummary({
    required this.id,
    required this.status,
    required this.riskLevel,
    required this.primaryDiagnosis,
    required this.createdAt,
    required this.hasStage2,
  });

  factory ReferralSummary.fromJson(Map<String, dynamic> j) => ReferralSummary(
        id: j['referralId'] ?? j['id'] ?? '',
        status: j['status'] ?? '',
        riskLevel: j['riskLevel'] ?? 'LOW',
        primaryDiagnosis: j['primaryDiagnosis'] ?? '',
        createdAt: j['createdAt'] ?? '',
        hasStage2: j['stage2RequestedAt'] != null,
      );
}

// ─── Provider ─────────────────────────────────────────────
final referralsProvider = FutureProvider<List<ReferralSummary>>((ref) async {
  final auth = ref.watch(authProvider);
  if (auth.token == null) return [];
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/referrals/patient',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  final data = resp.data['data'] as List? ?? [];
  return data.map((e) => ReferralSummary.fromJson(e)).toList();
});

// ─── Screen ───────────────────────────────────────────────
class PatientHomeScreen extends ConsumerStatefulWidget {
  const PatientHomeScreen({super.key});

  @override
  ConsumerState<PatientHomeScreen> createState() => _PatientHomeScreenState();
}

class _PatientHomeScreenState extends ConsumerState<PatientHomeScreen> {
  int _navIndex = 0;

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

  String _statusLabel(String status) {
    switch (status) {
      case 'Stage2Complete':    return 'مكتمل';
      case 'Stage2Requested':  return 'طلب المرحلة ٢';
      case 'Stage1Delivered':  return 'تم الإرسال';
      case 'FeedbackSubmitted': return 'تم التقييم';
      default:                 return 'تم الإنشاء';
    }
  }

  @override
  Widget build(BuildContext context) {
    final referralsAsync = ref.watch(referralsProvider);
    final auth = ref.watch(authProvider);

    return Scaffold(
      backgroundColor: const Color(0xFFF6F7FB),
      body: SafeArea(
        child: Column(
          children: [
            // ── Hero header ──────────────────────────────
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [Color(0xFF17305F), Color(0xFF11254A)],
                ),
                borderRadius: BorderRadius.only(
                  bottomLeft: Radius.circular(24),
                  bottomRight: Radius.circular(24),
                ),
              ),
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Top row — logo + bell
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Text(
                          'معافى+',
                          style: TextStyle(
                            color: Color(0xFF1E3A72),
                            fontWeight: FontWeight.w800,
                            fontSize: 16,
                          ),
                        ),
                      ),
                      Container(
                        width: 36, height: 36,
                        decoration: BoxDecoration(
                          color: Colors.white.withOpacity(0.12),
                          borderRadius: BorderRadius.circular(18),
                        ),
                        child: const Icon(Icons.notifications_outlined,
                            color: Colors.white, size: 18),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  // Greeting
                  const Text('مرحباً،',
                      style: TextStyle(color: Colors.white70, fontSize: 12)),
                  const SizedBox(height: 2),
                  Text(
                    '${auth.phoneNumber ?? 'المريض'} 👋',
                    style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w700,
                        fontSize: 20),
                  ),
                  const SizedBox(height: 4),
                  const Text('خطة الرعاية الخاصة بك جاهزة',
                      style: TextStyle(color: Colors.white60, fontSize: 11)),
                  const SizedBox(height: 18),
                  // Reading progress card
                  referralsAsync.when(
                    data: (referrals) {
                      final total = referrals.length;
                      final done = referrals
                          .where((r) =>
                              r.status == 'FeedbackSubmitted' ||
                              r.status == 'Stage2Complete')
                          .length;
                      final pct = total == 0 ? 0.0 : done / total;
                      return Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: Colors.white.withOpacity(0.10),
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(
                              color: Colors.white.withOpacity(0.12)),
                        ),
                        child: Column(
                          children: [
                            Row(
                              mainAxisAlignment:
                                  MainAxisAlignment.spaceBetween,
                              children: [
                                const Text('تقدم القراءة',
                                    style: TextStyle(
                                        color: Colors.white70,
                                        fontSize: 12)),
                                Text('$done / $total',
                                    style: const TextStyle(
                                        color: Colors.white,
                                        fontWeight: FontWeight.w600,
                                        fontSize: 12)),
                              ],
                            ),
                            const SizedBox(height: 8),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(3),
                              child: LinearProgressIndicator(
                                value: pct,
                                backgroundColor:
                                    Colors.white.withOpacity(0.15),
                                valueColor:
                                    const AlwaysStoppedAnimation<Color>(
                                        Color(0xFF3FA868)),
                                minHeight: 5,
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                    loading: () => const SizedBox(height: 48),
                    error: (_, __) => const SizedBox(height: 48),
                  ),
                ],
              ),
            ),

            // ── Referral list ────────────────────────────
            Expanded(
              child: referralsAsync.when(
                data: (referrals) {
                  if (referrals.isEmpty) {
                    return const Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.inbox_outlined,
                              size: 48, color: Color(0xFFB7BDCB)),
                          SizedBox(height: 12),
                          Text('لا توجد إحالات بعد',
                              style: TextStyle(
                                  color: Color(0xFF8A93A6), fontSize: 14)),
                        ],
                      ),
                    );
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 80),
                    itemCount: referrals.length,
                    itemBuilder: (ctx, i) {
                      final r = referrals[i];
                      return _ReferralCard(
                        referral: r,
                        riskColor: _riskColor(r.riskLevel),
                        riskBgColor: _riskBgColor(r.riskLevel),
                        riskLabel: _riskLabel(r.riskLevel),
                        statusLabel: _statusLabel(r.status),
                        onTap: () => context.push('/referral/${r.id}'),
                      );
                    },
                  );
                },
                loading: () => const Center(
                    child: CircularProgressIndicator(
                        color: Color(0xFF1E3A72))),
                error: (e, _) => Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.wifi_off_outlined,
                          size: 40, color: Color(0xFFB7BDCB)),
                      const SizedBox(height: 12),
                      const Text('تعذّر تحميل البيانات',
                          style: TextStyle(
                              color: Color(0xFF5A6478), fontSize: 14)),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: () => ref.refresh(referralsProvider),
                        child: const Text('إعادة المحاولة',
                            style: TextStyle(color: Color(0xFF1E3A72))),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),

      // ── Bottom nav ───────────────────────────────────
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _navIndex,
        onTap: (i) {
          if (i == 3) {
            ref.read(authProvider.notifier).logout();
            context.go('/login');
          } else {
            setState(() => _navIndex = i);
          }
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
              label: 'الرئيسية'),
          BottomNavigationBarItem(
              icon: Icon(Icons.article_outlined),
              activeIcon: Icon(Icons.article),
              label: 'مقالاتي'),
          BottomNavigationBarItem(
              icon: Icon(Icons.chat_bubble_outline),
              activeIcon: Icon(Icons.chat_bubble),
              label: 'اسأل'),
          BottomNavigationBarItem(
              icon: Icon(Icons.logout),
              label: 'خروج'),
        ],
      ),
    );
  }
}

// ─── Referral Card Widget ─────────────────────────────────
class _ReferralCard extends StatelessWidget {
  final ReferralSummary referral;
  final Color riskColor;
  final Color riskBgColor;
  final String riskLabel;
  final String statusLabel;
  final VoidCallback onTap;

  const _ReferralCard({
    required this.referral,
    required this.riskColor,
    required this.riskBgColor,
    required this.riskLabel,
    required this.statusLabel,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFEEF0F5)),
          boxShadow: [
            BoxShadow(
              color: const Color(0xFF0E1726).withOpacity(0.05),
              blurRadius: 12,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Risk accent bar
            Container(
              height: 4,
              decoration: BoxDecoration(
                color: riskColor,
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Top row — diagnosis + risk badge
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        width: 36, height: 36,
                        decoration: BoxDecoration(
                          color: const Color(0xFFE6F4EC),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Icon(Icons.auto_awesome,
                            size: 16, color: Color(0xFF197540)),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              referral.primaryDiagnosis.isNotEmpty
                                  ? referral.primaryDiagnosis
                                  : 'إحالة طبية',
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF0E1726),
                              ),
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                            ),
                            const SizedBox(height: 2),
                            const Text(
                              'مولَّد بواسطة AI',
                              style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF197540)),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      // Risk badge
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: riskBgColor,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          'خطر $riskLabel',
                          style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.w600,
                              color: riskColor),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  // Bottom row — status + date + arrow
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: const Color(0xFFEEF0F5),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          statusLabel,
                          style: const TextStyle(
                              fontSize: 10,
                              color: Color(0xFF5A6478),
                              fontWeight: FontWeight.w500),
                        ),
                      ),
                      const SizedBox(width: 8),
                      const Icon(Icons.access_time,
                          size: 11, color: Color(0xFFB7BDCB)),
                      const SizedBox(width: 3),
                      Expanded(
                        child: Text(
                          referral.createdAt,
                          style: const TextStyle(
                              fontSize: 10, color: Color(0xFF8A93A6)),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const Icon(Icons.chevron_left,
                          size: 16, color: Color(0xFFB7BDCB)),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
