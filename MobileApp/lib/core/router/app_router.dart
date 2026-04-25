import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/patient/auth/providers/auth_provider.dart';
import '../../features/patient/auth/screens/patient_login_screen.dart';
import '../../features/patient/home/screens/patient_home_screen.dart';
import '../../features/patient/referral/screens/referral_detail_screen.dart';
import '../../features/patient/article/screens/article_reader_screen.dart';
import '../../features/patient/feedback/screens/feedback_screen.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);
  return GoRouter(
    initialLocation: '/login',
    redirect: (context, state) {
      if (authState.isInitializing) return null;
      final isAuth  = authState.status == AuthStatus.authenticated;
      final isLogin = state.matchedLocation == '/login';
      if (isAuth && isLogin) return '/home';
      if (!isAuth && !isLogin) return '/login';
      return null;
    },
    routes: [
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
    ],
  );
});
