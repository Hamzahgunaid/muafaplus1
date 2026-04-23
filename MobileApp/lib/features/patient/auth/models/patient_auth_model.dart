class PatientLoginRequest {
  final String phoneNumber;
  final String code;

  PatientLoginRequest({required this.phoneNumber, required this.code});

  Map<String, dynamic> toJson() => {
    'phoneNumber': phoneNumber,
    'code': code,
  };
}

class PatientLoginResponse {
  final bool success;
  final String? token;
  final String? phoneNumber;
  final int? referralCount;
  final String? error;

  PatientLoginResponse({
    required this.success,
    this.token,
    this.phoneNumber,
    this.referralCount,
    this.error,
  });

  factory PatientLoginResponse.fromJson(Map<String, dynamic> json) {
    final data = json['data'];
    return PatientLoginResponse(
      success: json['success'] ?? false,
      token: data?['token'],
      phoneNumber: data?['phoneNumber'],
      referralCount: data?['referralCount'],
      error: json['error'],
    );
  }
}
