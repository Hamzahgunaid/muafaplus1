import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../core/constants/app_colors.dart';

class MuafaButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool isLoading;
  final bool isOutlined;
  final double height;
  final Color? backgroundColor;
  final Color? foregroundColor;
  final IconData? icon;

  const MuafaButton({
    super.key,
    required this.label,
    this.onPressed,
    this.isLoading = false,
    this.isOutlined = false,
    this.height = 52,
    this.backgroundColor,
    this.foregroundColor,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    final bgColor = backgroundColor ?? AppColors.navy600;
    final fgColor = foregroundColor ?? AppColors.white;

    final child = isLoading
      ? SizedBox(
          width: 22, height: 22,
          child: CircularProgressIndicator(
            color: isOutlined ? bgColor : fgColor,
            strokeWidth: 2.5))
      : icon != null
        ? Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 18),
              const SizedBox(width: 8),
              Text(label, style: GoogleFonts.ibmPlexSansArabic(
                fontSize: 15, fontWeight: FontWeight.w700)),
            ])
        : Text(label, style: GoogleFonts.ibmPlexSansArabic(
            fontSize: 15, fontWeight: FontWeight.w700));

    if (isOutlined) {
      return SizedBox(
        width: double.infinity,
        height: height,
        child: OutlinedButton(
          onPressed: isLoading ? null : onPressed,
          style: OutlinedButton.styleFrom(
            foregroundColor: bgColor,
            side: BorderSide(color: bgColor),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12))),
          child: child,
        ),
      );
    }

    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: bgColor,
          foregroundColor: fgColor,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12)),
          elevation: 0),
        child: child,
      ),
    );
  }
}
