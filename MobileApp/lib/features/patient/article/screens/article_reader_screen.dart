import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';

class ArticleReaderScreen extends StatelessWidget {
  final String articleId;
  const ArticleReaderScreen({super.key, required this.articleId});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy700,
          foregroundColor: AppColors.white,
          title: Text(
            'المقال',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w600,
              color: AppColors.white,
            ),
          ),
        ),
        body: Center(
          child: Text(
            'قريباً',
            style: GoogleFonts.ibmPlexSansArabic(
              fontSize: 18,
              color: AppColors.ink400,
            ),
          ),
        ),
      ),
    );
  }
}
