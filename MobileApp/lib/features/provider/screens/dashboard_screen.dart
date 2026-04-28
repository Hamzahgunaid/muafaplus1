import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';

// Provider dashboard — Phase 4B
class ProviderDashboardScreen extends ConsumerWidget {
  const ProviderDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        body: Column(
          children: [
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
                            Text('لوحة التحكم',
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 14,
                                color: AppColors.white.withOpacity(0.65))),
                            Text('معافى+',
                              style: GoogleFonts.ibmPlexSansArabic(
                                fontSize: 22,
                                fontWeight: FontWeight.w700,
                                color: AppColors.white)),
                          ],
                        ),
                      ),
                      Container(
                        width: 40, height: 40,
                        decoration: BoxDecoration(
                          color: AppColors.white.withOpacity(0.12),
                          borderRadius: BorderRadius.circular(12)),
                        child: const Icon(Icons.person_outline,
                          color: AppColors.white, size: 22),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            Expanded(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 72, height: 72,
                      decoration: BoxDecoration(
                        color: AppColors.navy600.withOpacity(0.08),
                        borderRadius: BorderRadius.circular(20)),
                      child: const Icon(Icons.medical_services_outlined,
                        color: AppColors.navy600, size: 36),
                    ),
                    const SizedBox(height: 20),
                    Text('لوحة مزود الخدمة',
                      style: GoogleFonts.ibmPlexSansArabic(
                        fontSize: 18,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink900)),
                    const SizedBox(height: 8),
                    Text('ستكون متاحة في التحديث القادم',
                      style: GoogleFonts.ibmPlexSansArabic(
                        fontSize: 14, color: AppColors.ink400)),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
