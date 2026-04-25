import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';

class FeedbackScreen extends StatelessWidget {
  final String referralId;
  const FeedbackScreen({super.key, required this.referralId});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          title: Text('التقييم',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700, color: AppColors.white)),
        ),
        body: const Center(child: CircularProgressIndicator(
          color: AppColors.navy600)),
      ),
    );
  }
}
