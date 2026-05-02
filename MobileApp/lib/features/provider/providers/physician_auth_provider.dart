import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../core/network/dio_client.dart';
// ignore: avoid_web_libraries_in_flutter
import 'dart:js' as js;

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
  }) =>
      PhysicianAuthState(
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

  // ── Storage helpers ───────────────────────────────────
  Future<void> _save(String key, String value) async {
    try {
      js.context['localStorage'].callMethod('setItem', [key, value]);
    } catch (_) {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(key, value);
    }
  }

  Future<String?> _load(String key) async {
    try {
      final val = js.context['localStorage'].callMethod('getItem', [key]);
      if (val != null && val.toString().isNotEmpty) return val.toString();
    } catch (_) {}
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getString(key);
    } catch (_) {
      return null;
    }
  }

  Future<void> _remove(String key) async {
    try {
      js.context['localStorage'].callMethod('removeItem', [key]);
    } catch (_) {}
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(key);
    } catch (_) {}
  }

  // ── Load from storage on startup ─────────────────────
  Future<void> _loadFromStorage() async {
    final token = await _load('muafa_provider_token');
    if (token != null) {
      state = state.copyWith(
        token: token,
        role: await _load('muafa_provider_role'),
        fullName: await _load('muafa_provider_fullname'),
        specialty: await _load('muafa_provider_specialty'),
        tenantId: await _load('muafa_provider_tenantid'),
      );
    }
    state = state.copyWith(isInitializing: false);
  }

  // ── Login ─────────────────────────────────────────────
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

      await _save('muafa_provider_token', token);
      await _save('muafa_provider_role', role);
      await _save('muafa_provider_fullname', fullName);
      await _save('muafa_provider_specialty', specialty);
      await _save('muafa_provider_tenantid', tenantId);

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

  // ── Logout ────────────────────────────────────────────
  Future<void> logout() async {
    await _remove('muafa_provider_token');
    await _remove('muafa_provider_role');
    await _remove('muafa_provider_fullname');
    await _remove('muafa_provider_specialty');
    await _remove('muafa_provider_tenantid');
    state = const PhysicianAuthState(isInitializing: false);
  }
}

final physicianAuthProvider =
    StateNotifierProvider<PhysicianAuthNotifier, PhysicianAuthState>(
  (ref) => PhysicianAuthNotifier(),
);
