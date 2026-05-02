import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../core/network/dio_client.dart';

class PhysicianAuthState {
  final String? token;
  final String? role;
  final String? fullName;
  final String? specialty;
  final String? tenantId;
  final bool isLoading;
  final String? error;
  final bool isInitializing;

  const PhysicianAuthState({
    this.token,
    this.role,
    this.fullName,
    this.specialty,
    this.tenantId,
    this.isLoading = false,
    this.error,
    this.isInitializing = true,
  });

  bool get isLoggedIn => token != null;

  PhysicianAuthState copyWith({
    String? token,
    String? role,
    String? fullName,
    String? specialty,
    String? tenantId,
    bool? isLoading,
    String? error,
    bool? isInitializing,
  }) => PhysicianAuthState(
    token: token ?? this.token,
    role: role ?? this.role,
    fullName: fullName ?? this.fullName,
    specialty: specialty ?? this.specialty,
    tenantId: tenantId ?? this.tenantId,
    isLoading: isLoading ?? this.isLoading,
    error: error ?? this.error,
    isInitializing: isInitializing ?? this.isInitializing,
  );
}

class PhysicianAuthNotifier extends StateNotifier<PhysicianAuthState> {
  PhysicianAuthNotifier() : super(const PhysicianAuthState()) {
    _loadFromStorage();
  }

  Future<void> _loadFromStorage() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('muafa_provider_token');
    if (token != null) {
      state = state.copyWith(
        token: token,
        role: prefs.getString('muafa_provider_role'),
        fullName: prefs.getString('muafa_provider_fullname'),
        specialty: prefs.getString('muafa_provider_specialty'),
        tenantId: prefs.getString('muafa_provider_tenantid'),
      );
    }
    state = state.copyWith(isInitializing: false);
  }

  Future<bool> login(String email, String password) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final dio = DioClient.instance;
      final res = await dio.post('/auth/login', data: {
        'email': email,
        'password': password,
      });
      final data = res.data['data'] as Map<String, dynamic>;
      final token = data['token'] as String;
      final role = data['role'] as String;
      final fullName = data['fullName'] as String? ?? '';
      final specialty = data['specialty'] as String? ?? '';
      final tenantId = data['tenantId'] as String? ?? '';

      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('muafa_provider_token', token);
      await prefs.setString('muafa_provider_role', role);
      await prefs.setString('muafa_provider_fullname', fullName);
      await prefs.setString('muafa_provider_specialty', specialty);
      await prefs.setString('muafa_provider_tenantid', tenantId);

      state = state.copyWith(
        token: token,
        role: role,
        fullName: fullName,
        specialty: specialty,
        tenantId: tenantId,
        isLoading: false,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'البريد الإلكتروني أو كلمة المرور غير صحيحة',
      );
      return false;
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('muafa_provider_token');
    await prefs.remove('muafa_provider_role');
    await prefs.remove('muafa_provider_fullname');
    await prefs.remove('muafa_provider_specialty');
    await prefs.remove('muafa_provider_tenantid');
    state = const PhysicianAuthState();
  }
}

final physicianAuthProvider =
    StateNotifierProvider<PhysicianAuthNotifier, PhysicianAuthState>(
  (ref) => PhysicianAuthNotifier(),
);
