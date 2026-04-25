import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/constants/api_constants.dart';
import '../models/patient_auth_model.dart';

enum AuthStatus { initial, loading, authenticated, error }

class AuthState {
  final AuthStatus status;
  final String? token;
  final String? phoneNumber;
  final int? referralCount;
  final String? errorMessage;
  final bool isInitializing;

  AuthState({
    this.status = AuthStatus.initial,
    this.token,
    this.phoneNumber,
    this.referralCount,
    this.errorMessage,
    this.isInitializing = true,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? token,
    String? phoneNumber,
    int? referralCount,
    String? errorMessage,
    bool? isInitializing,
  }) => AuthState(
    status: status ?? this.status,
    token: token ?? this.token,
    phoneNumber: phoneNumber ?? this.phoneNumber,
    referralCount: referralCount ?? this.referralCount,
    errorMessage: errorMessage ?? this.errorMessage,
    isInitializing: isInitializing ?? this.isInitializing,
  );
}

class AuthNotifier extends StateNotifier<AuthState> {
  final Ref _ref;

  AuthNotifier(this._ref) : super(AuthState()) {
    _checkExistingToken();
  }

  Future<void> _checkExistingToken() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('patient_token');
    print('DEBUG _checkExistingToken: token=${token != null ? token.substring(0, 20) : 'NULL'}');
    if (token != null) {
      _ref.read(tokenProvider.notifier).state = token;
      state = state.copyWith(
        status: AuthStatus.authenticated,
        token: token,
        phoneNumber: prefs.getString('patient_phone'),
        isInitializing: false,
      );
    } else {
      state = state.copyWith(isInitializing: false);
    }
  }

  Future<bool> login(String phoneNumber, String code) async {
    state = state.copyWith(status: AuthStatus.loading);
    try {
      final dio = _ref.read(dioClientProvider).dio;
      final response = await dio.post(
        ApiConstants.patientLogin,
        data: PatientLoginRequest(
          phoneNumber: phoneNumber, code: code).toJson(),
      );
      final result = PatientLoginResponse.fromJson(response.data);
      if (result.success && result.token != null) {
        print('DEBUG login success: token=${result.token!.substring(0, 20)}');
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('patient_token', result.token!);
        await prefs.setString('patient_phone',
          result.phoneNumber ?? phoneNumber);
        _ref.read(tokenProvider.notifier).state = result.token;
        state = state.copyWith(
          status: AuthStatus.authenticated,
          token: result.token,
          phoneNumber: result.phoneNumber,
          referralCount: result.referralCount,
        );
        print('DEBUG authProvider.token after login: ${state.token?.substring(0, 20)}');
        return true;
      }
      state = state.copyWith(
        status: AuthStatus.error, errorMessage: result.error);
      return false;
    } catch (e) {
      print('DEBUG login error: $e');
      state = state.copyWith(
        status: AuthStatus.error, errorMessage: e.toString());
      return false;
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('patient_token');
    await prefs.remove('patient_phone');
    _ref.read(tokenProvider.notifier).state = null;
    state = AuthState();
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>(
  (ref) => AuthNotifier(ref),
);
