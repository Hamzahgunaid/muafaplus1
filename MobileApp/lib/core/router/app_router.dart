import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/auth/screens/splash_decider_screen.dart';
import '../../features/patient/auth/screens/patient_login_screen.dart';
import '../../features/patient/home/screens/patient_home_screen.dart';
import '../../features/patient/referral/screens/referral_detail_screen.dart';
import '../../features/patient/article/screens/article_reader_screen.dart';
import '../../features/patient/screens/feedback_screen.dart';
import '../../features/patient/chat/screens/patient_chat_list_screen.dart';
import '../../features/auth/screens/provider_login_screen.dart';
import '../../features/provider/screens/dashboard_screen.dart';
import '../../features/provider/screens/create_referral_screen.dart';
import '../../features/provider/screens/referrals_screen.dart';
import '../../features/provider/screens/referral_detail_screen.dart';
import '../../features/provider/screens/test_scenarios_screen.dart';
import '../../features/provider/screens/create_test_scenario_screen.dart';
import '../../features/provider/screens/test_scenario_detail_screen.dart';
import '../../features/provider/screens/evaluation_form_screen.dart';
import '../../features/assistant/screens/assistant_dashboard_screen.dart';
import '../../features/assistant/screens/assistant_referrals_screen.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/splash',
    routes: [
      // ── Splash ──────────────────────────────────────────────────────────
      GoRoute(path: '/splash',
        builder: (_, __) => const SplashDeciderScreen()),

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
      GoRoute(path: '/patient/chats',
        builder: (_, __) => const PatientChatListScreen()),

      // ── Provider routes ──────────────────────────────────────────────────
      GoRoute(path: '/provider/login',
        builder: (_, __) => const ProviderLoginScreen()),
      GoRoute(path: '/provider/dashboard',
        builder: (_, __) => const ProviderDashboardScreen()),
      GoRoute(path: '/provider/referrals',
        builder: (_, __) => const ProviderReferralsScreen()),
      GoRoute(path: '/provider/referrals/new',
        builder: (_, __) => const CreateReferralScreen()),
      GoRoute(path: '/provider/referrals/:id',
        builder: (_, state) => ProviderReferralDetailScreen(
          referralId: state.pathParameters['id']!)),
      GoRoute(path: '/provider/test-scenarios',
        builder: (_, __) => const TestScenariosScreen()),
      GoRoute(path: '/provider/test-scenarios/new',
        builder: (_, __) => const CreateTestScenarioScreen()),
      GoRoute(path: '/provider/test-scenarios/:id',
        builder: (_, state) => TestScenarioDetailScreen(
          scenarioId: state.pathParameters['id']!)),
      GoRoute(path: '/provider/test-scenarios/:id/evaluate',
        builder: (_, state) => EvaluationFormScreen(
          scenarioId: state.pathParameters['id']!)),

      // ── Assistant routes ─────────────────────────────────────────────────
      GoRoute(path: '/assistant/dashboard',
        builder: (_, __) => const AssistantDashboardScreen()),
      GoRoute(path: '/assistant/referrals',
        builder: (_, __) => const AssistantReferralsScreen()),
    ],
  );
});
