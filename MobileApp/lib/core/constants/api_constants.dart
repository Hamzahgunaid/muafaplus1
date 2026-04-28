class ApiConstants {
  static const String baseUrl = 'https://muafaplus1-production.up.railway.app/api/v1';
  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 60);

  static const String patientLogin = '/auth/patient/login';
  static const String referrals = '/referrals';
  static String referralArticles(String id) => '/referrals/$id/articles';
  static String referralStage2(String id) => '/referrals/$id/stage2';
  static String referralFeedback(String id) => '/referrals/$id/feedback';
  static String articleEngagement(String id) => '/articles/$id/engagement';
  static String articleReaction(String id) => '/articles/$id/reaction';
}
