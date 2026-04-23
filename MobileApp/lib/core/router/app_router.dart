import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/patient/auth/providers/auth_provider.dart';
import '../../features/patient/auth/screens/patient_login_screen.dart';
import '../../features/patient/home/screens/patient_home_screen.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    initialLocation: '/login',
    redirect: (context, state) {
      final isAuthenticated = authState.status == AuthStatus.authenticated;
      final isLoginRoute = state.matchedLocation == '/login';

      if (isAuthenticated && isLoginRoute) return '/home';
      if (!isAuthenticated && !isLoginRoute) return '/login';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (_, __) => const PatientLoginScreen()),
      GoRoute(path: '/home',  builder: (_, __) => const PatientHomeScreen()),
    ],
  );
});
