import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../core/constants/app_colors.dart';

// Provider referrals management — Phase 4B
class ProviderReferralsScreen extends ConsumerWidget {
  const ProviderReferralsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          elevation: 0,
          title: Text('الإحالات',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700, color: AppColors.white)),
        ),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 72, height: 72,
                decoration: BoxDecoration(
                  color: AppColors.navy600.withOpacity(0.08),
                  borderRadius: BorderRadius.circular(20)),
                child: const Icon(Icons.assignment_outlined,
                  color: AppColors.navy600, size: 36),
              ),
              const SizedBox(height: 20),
              Text('إدارة الإحالات',
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
    );
  }
}
