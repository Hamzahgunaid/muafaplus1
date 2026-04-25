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
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          title: Text('قراءة المقال',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700, color: AppColors.white)),
        ),
        body: const Center(child: CircularProgressIndicator(
          color: AppColors.navy600)),
      ),
    );
  }
}
