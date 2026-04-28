class ReferralSummary {
  final String referralId;
  final String primaryDiagnosis;
  final String riskLevel;
  final String status;
  final String patientPhone;
  final DateTime createdAt;
  final bool whatsAppDelivery;

  const ReferralSummary({
    required this.referralId,
    required this.primaryDiagnosis,
    required this.riskLevel,
    required this.status,
    required this.patientPhone,
    required this.createdAt,
    required this.whatsAppDelivery,
  });

  factory ReferralSummary.fromJson(Map<String, dynamic> json) => ReferralSummary(
    referralId: json['referralId'] as String? ?? json['id'] as String? ?? '',
    primaryDiagnosis: json['primaryDiagnosis'] as String? ??
      (json['patientProfile'] as Map<String, dynamic>?)?['primaryDiagnosis'] as String? ?? '',
    riskLevel: json['riskLevel'] as String? ?? 'LOW',
    status: json['status'] as String? ?? '',
    patientPhone: json['patientPhone'] as String? ??
      (json['patientProfile'] as Map<String, dynamic>?)?['phoneNumber'] as String? ?? '',
    createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
    whatsAppDelivery: json['whatsAppDelivery'] as bool? ?? false,
  );
}
