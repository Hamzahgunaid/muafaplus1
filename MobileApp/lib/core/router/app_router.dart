import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/patient/auth/providers/auth_provider.dart';
import '../../features/patient/auth/screens/patient_login_screen.dart';
import '../../features/patient/home/screens/patient_home_screen.dart';
import '../../features/patient/referral/screens/referral_detail_screen.dart';
import '../../features/patient/article/screens/article_reader_screen.dart';
import '../../features/patient/screens/feedback_screen.dart';
import '../../features/provider/screens/provider_login_screen.dart';
import '../../features/provider/screens/dashboard_screen.dart';
import '../../features/provider/screens/create_referral_screen.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/login',
    redirect: (context, state) {
      final location = state.matchedLocation;

      // Never redirect provider routes — they handle their own auth
      if (location.startsWith('/provider')) return null;

      // Check patient auth synchronously from Riverpod
      final patientAuth = ref.read(authProvider);
      final isPatientLoggedIn = patientAuth.token != null;
      final isOnPatientLogin = location == '/login';

      if (!isPatientLoggedIn && !isOnPatientLogin) return '/login';
      if (isPatientLoggedIn && isOnPatientLogin) return '/home';
      return null;
    },
    routes: [
      // ── Patient routes ───────────────────────────────────────────────────
      GoRoute(path: '/login',
        builder: (_, __) => const PatientLoginScreen()),
      GoRoute(path: '/home',
        builder: (_, __) => const PatientHomeScreen()),
      GoRoute(path: '/referral/:id',
        builder: (_, state) => ReferralDetailScreen(
          referralId: state.pathParameters['id']!)),
      GoRoute(path: '/article/:referralId/:articleId',
        builder: (_, state) => ArticleReaderScreen(
          referralId: state.pathParameters['referralId']!,
          articleId: state.pathParameters['articleId']!)),
      GoRoute(path: '/feedback/:id',
        builder: (_, state) => FeedbackScreen(
          referralId: state.pathParameters['id']!)),

      // ── Provider routes ──────────────────────────────────────────────────
      GoRoute(path: '/provider/login',
        builder: (_, __) => const ProviderLoginScreen()),
      GoRoute(path: '/provider/dashboard',
        builder: (_, __) => const ProviderDashboardScreen()),
      GoRoute(path: '/provider/referrals/new',
        builder: (_, __) => const CreateReferralScreen()),
    ],
  );
});
