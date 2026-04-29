import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
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
        appBar: AppBar(
          backgroundColor: const Color(0xFF283481),
          automaticallyImplyLeading: false,
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(auth.fullName ?? 'الطبيب',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.bold)),
              if ((auth.specialty ?? '').isNotEmpty)
                Text(auth.specialty!,
                  style: const TextStyle(
                    color: Colors.white70, fontSize: 12)),
            ],
          ),
          actions: [
            IconButton(
              icon: const Icon(Icons.logout, color: Colors.white),
              tooltip: 'تسجيل الخروج',
              onPressed: () async {
                await ref.read(physicianAuthProvider.notifier).logout();
                if (context.mounted) context.go('/login');
              },
            ),
          ],
        ),
        body: RefreshIndicator(
          onRefresh: () => ref.refresh(recentReferralsProvider.future),
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Stats row
                referralsAsync.when(
                  data: (referrals) => Row(
                    children: [
                      _StatCard(
                        label: 'إجمالي الإحالات',
                        value: referrals.length.toString(),
                        color: const Color(0xFF283481)),
                      const SizedBox(width: 12),
                      _StatCard(
                        label: 'هذا الشهر',
                        value: referrals
                          .where((r) => r.createdAt.month == DateTime.now().month
                            && r.createdAt.year == DateTime.now().year)
                          .length.toString(),
                        color: const Color(0xFF21A740)),
                    ],
                  ),
                  loading: () => const SizedBox(
                    height: 80,
                    child: Center(child: CircularProgressIndicator(
                      color: Color(0xFF283481)))),
                  error: (_, __) => const SizedBox.shrink(),
                ),

                const SizedBox(height: 20),

                // New referral button
                ElevatedButton.icon(
                  onPressed: () => context.push('/provider/referrals/new'),
                  icon: const Icon(Icons.add, color: Colors.white),
                  label: const Text('إحالة مريض جديد',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 15,
                      fontWeight: FontWeight.bold)),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF283481),
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                  ),
                ),

                const SizedBox(height: 20),

                const Text('الإحالات الأخيرة',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF0E1726)),
                  textAlign: TextAlign.right),

                const SizedBox(height: 12),

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
                  error: (e, _) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    child: Text('خطأ في تحميل الإحالات',
                      style: const TextStyle(color: Color(0xFFD64545)),
                      textAlign: TextAlign.right)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _StatCard({
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) => Expanded(
    child: Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFEEF0F5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(value,
            style: TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.bold,
              color: color)),
          const SizedBox(height: 4),
          Text(label,
            style: const TextStyle(
              fontSize: 12,
              color: Color(0xFF5A6478))),
        ],
      ),
    ),
  );
}

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

  @override
  Widget build(BuildContext context) {
    final color = _riskColors[referral.riskLevel] ?? const Color(0xFF5A6478);
    final label = _riskLabels[referral.riskLevel] ?? referral.riskLevel;

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFEEF0F5)),
      ),
      child: Row(
        children: [
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
                  style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    color: Color(0xFF0E1726)),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
                if (referral.patientPhone.isNotEmpty)
                  Text(referral.patientPhone,
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFF5A6478))),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Icon(Icons.chevron_left,
            color: const Color(0xFF8A93A6), size: 20),
        ],
      ),
    );
  }
}
