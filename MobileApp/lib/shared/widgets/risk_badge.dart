import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../core/constants/app_colors.dart';

class RiskBadge extends StatelessWidget {
  final String riskLevel;
  final bool large;

  const RiskBadge({super.key, required this.riskLevel, this.large = false});

  Color get _textColor {
    switch (riskLevel.toUpperCase()) {
      case 'LOW':      return AppColors.riskLowText;
      case 'MODERATE': return AppColors.riskModText;
      case 'HIGH':     return AppColors.riskHighText;
      case 'CRITICAL': return AppColors.riskCritText;
      default:         return AppColors.riskLowText;
    }
  }

  Color get _bgColor {
    switch (riskLevel.toUpperCase()) {
      case 'LOW':      return AppColors.riskLowBg;
      case 'MODERATE': return AppColors.riskModBg;
      case 'HIGH':     return AppColors.riskHighBg;
      case 'CRITICAL': return AppColors.riskCritBg;
      default:         return AppColors.riskLowBg;
    }
  }

  String get _label {
    switch (riskLevel.toUpperCase()) {
      case 'LOW':      return 'منخفض';
      case 'MODERATE': return 'متوسط';
      case 'HIGH':     return 'مرتفع';
      case 'CRITICAL': return 'حرج';
      default:         return riskLevel;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: large ? 14 : 10,
        vertical: large ? 6 : 4),
      decoration: BoxDecoration(
        color: _bgColor,
        borderRadius: BorderRadius.circular(999)),
      child: Text(_label,
        style: GoogleFonts.ibmPlexSansArabic(
          fontSize: large ? 13 : 11,
          fontWeight: FontWeight.w700,
          color: _textColor)),
    );
  }
}
