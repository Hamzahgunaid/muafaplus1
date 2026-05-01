import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import '../providers/physician_auth_provider.dart';
import '../providers/referrals_provider.dart';
import '../models/physician_model.dart';

class ProviderReferralsScreen extends ConsumerStatefulWidget {
  const ProviderReferralsScreen({super.key});

  @override
  ConsumerState<ProviderReferralsScreen> createState() =>
      _ProviderReferralsScreenState();
}

class _ProviderReferralsScreenState
    extends ConsumerState<ProviderReferralsScreen> {
  String _filterRisk = 'ALL';

  static const _filters = [
    {'key': 'ALL',      'label': 'الكل'},
    {'key': 'CRITICAL', 'label': 'حرج'},
    {'key': 'HIGH',     'label': 'مرتفع'},
    {'key': 'MODERATE', 'label': 'متوسط'},
    {'key': 'LOW',      'label': 'منخفض'},
  ];

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
    final referralsAsync = ref.watch(recentReferralsProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: const Color(0xFFF6F7FB),
        appBar: AppBar(
          backgroundColor: const Color(0xFF283481),
          foregroundColor: Colors.white,
          elevation: 0,
          automaticallyImplyLeading: false,
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward, color: Colors.white),
            onPressed: () => context.pop(),
          ),
          title: Text('الإحالات',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700, color: Colors.white)),
          actions: [
            IconButton(
              icon: const Icon(Icons.add, color: Colors.white),
              tooltip: 'إحالة جديدة',
              onPressed: () => context.push('/provider/referrals/new'),
            ),
          ],
        ),
        bottomNavigationBar: BottomNavigationBar(
          currentIndex: 1,
          onTap: (i) {
            if (i == 0) context.go('/provider/dashboard');
            if (i == 1) context.go('/provider/referrals');
            if (i == 2) context.go('/provider/test-scenarios');
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
            BottomNavigationBarItem(
              icon: Icon(Icons.science_outlined),
              activeIcon: Icon(Icons.science),
              label: 'الاختبار',
            ),
          ],
        ),
        body: Column(
          children: [
            // ── Filter tab bar ────────────────────────────────────────────
            Container(
              color: const Color(0xFF283481),
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                child: Row(
                  children: _filters.map((f) {
                    final key = f['key'] as String;
                    final selected = _filterRisk == key;
                    final riskColor = _riskColors[key];
                    return GestureDetector(
                      onTap: () => setState(() => _filterRisk = key),
                      child: Container(
                        margin: const EdgeInsets.only(left: 8),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 6),
                        decoration: BoxDecoration(
                          color: selected
                            ? (riskColor ?? Colors.white)
                            : Colors.white.withOpacity(0.15),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(f['label'] as String,
                          style: GoogleFonts.ibmPlexSansArabic(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: selected ? Colors.white : Colors.white70)),
                      ),
                    );
                  }).toList(),
                ),
              ),
            ),

            // ── Referral list ─────────────────────────────────────────────
            Expanded(
              child: referralsAsync.when(
                loading: () => const Center(
                  child: CircularProgressIndicator(color: Color(0xFF283481))),
                error: (e, _) => Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline,
                        color: Color(0xFFD64545), size: 48),
                      const SizedBox(height: 12),
                      Text('خطأ في تحميل الإحالات',
                        style: GoogleFonts.ibmPlexSansArabic(
                          color: const Color(0xFF5A6478))),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: () =>
                          ref.refresh(recentReferralsProvider.future),
                        child: const Text('إعادة المحاولة'),
                      ),
                    ],
                  ),
                ),
                data: (referrals) {
                  final filtered = _filterRisk == 'ALL'
                    ? referrals
                    : referrals
                        .where((r) => r.riskLevel == _filterRisk)
                        .toList();

                  if (filtered.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Container(
                            width: 72, height: 72,
                            decoration: BoxDecoration(
                              color: const Color(0xFF283481).withOpacity(0.08),
                              borderRadius: BorderRadius.circular(20)),
                            child: const Icon(Icons.assignment_outlined,
                              color: Color(0xFF283481), size: 36),
                          ),
                          const SizedBox(height: 16),
                          Text('لا توجد إحالات',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                              color: const Color(0xFF0E1726))),
                          const SizedBox(height: 6),
                          Text(_filterRisk == 'ALL'
                            ? 'لم يتم إنشاء أي إحالات بعد'
                            : 'لا توجد إحالات بهذا المستوى',
                            style: GoogleFonts.ibmPlexSansArabic(
                              fontSize: 13,
                              color: const Color(0xFF5A6478))),
                        ],
                      ),
                    );
                  }

                  return RefreshIndicator(
                    onRefresh: () =>
                      ref.refresh(recentReferralsProvider.future),
                    child: ListView.builder(
                      padding: const EdgeInsets.all(16),
                      itemCount: filtered.length,
                      itemBuilder: (_, i) {
                        final r = filtered[i];
                        final color = _riskColors[r.riskLevel] ??
                          const Color(0xFF5A6478);
                        final label = _riskLabels[r.riskLevel] ?? r.riskLevel;

                        return GestureDetector(
                          onTap: () => context.push(
                              '/provider/referrals/${r.referralId}'),
                          child: Container(
                          margin: const EdgeInsets.only(bottom: 10),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: const Color(0xFFEEF0F5)),
                          ),
                          child: Column(
                            children: [
                              // Accent bar
                              Container(
                                height: 4,
                                decoration: BoxDecoration(
                                  color: color,
                                  borderRadius: const BorderRadius.vertical(
                                    top: Radius.circular(12))),
                              ),
                              Padding(
                                padding: const EdgeInsets.all(14),
                                child: Row(
                                  children: [
                                    // Risk badge
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                        horizontal: 8, vertical: 4),
                                      decoration: BoxDecoration(
                                        color: color.withOpacity(0.1),
                                        borderRadius:
                                          BorderRadius.circular(6)),
                                      child: Text(label,
                                        style: TextStyle(
                                          fontSize: 11,
                                          fontWeight: FontWeight.bold,
                                          color: color)),
                                    ),
                                    const SizedBox(width: 12),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                          CrossAxisAlignment.end,
                                        children: [
                                          Text(
                                            r.primaryDiagnosis.isNotEmpty
                                              ? r.primaryDiagnosis
                                              : 'إحالة طبية',
                                            style: GoogleFonts
                                              .ibmPlexSansArabic(
                                                fontWeight: FontWeight.w600,
                                                color: const Color(
                                                  0xFF0E1726)),
                                            maxLines: 1,
                                            overflow:
                                              TextOverflow.ellipsis),
                                          const SizedBox(height: 2),
                                          Row(
                                            mainAxisAlignment:
                                              MainAxisAlignment.end,
                                            children: [
                                              Text(_formatDate(r.createdAt),
                                                style: GoogleFonts
                                                  .ibmPlexSansArabic(
                                                    fontSize: 11,
                                                    color: const Color(
                                                      0xFF8A93A6))),
                                              if (r.patientPhone.isNotEmpty) ...[
                                                const SizedBox(width: 8),
                                                Text('·',
                                                  style: const TextStyle(
                                                    color: Color(0xFF8A93A6))),
                                                const SizedBox(width: 8),
                                                Text(r.patientPhone,
                                                  style: GoogleFonts
                                                    .ibmPlexSansArabic(
                                                      fontSize: 11,
                                                      color: const Color(
                                                        0xFF8A93A6))),
                                              ],
                                            ],
                                          ),
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
                      },
                    ),
                  );
                },
              ),
            ),
          ],
        ),

        // ── FAB ────────────────────────────────────────────────────────────
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => context.push('/provider/referrals/new'),
          backgroundColor: const Color(0xFF283481),
          icon: const Icon(Icons.add, color: Colors.white),
          label: Text('إحالة جديدة',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w600, color: Colors.white)),
        ),
      ),
    );
  }
}
