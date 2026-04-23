import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../auth/providers/auth_provider.dart';

class PatientHomeScreen extends ConsumerWidget {
  const PatientHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.lightGrey,
        appBar: AppBar(
          backgroundColor: AppColors.deepNavy,
          title: Text(
            'معافى+',
            style: GoogleFonts.notoSansArabic(
              color: AppColors.white,
              fontWeight: FontWeight.bold,
            ),
          ),
          actions: [
            IconButton(
              icon: const Icon(Icons.logout, color: Colors.white),
              onPressed: () => ref.read(authProvider.notifier).logout(),
            ),
          ],
        ),
        body: Center(
          child: Text(
            'مرحباً ${authState.phoneNumber ?? ""} — الشاشة الرئيسية قيد الإنشاء',
            style: GoogleFonts.notoSansArabic(
              fontSize: 16,
              color: AppColors.textMedium,
            ),
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}
