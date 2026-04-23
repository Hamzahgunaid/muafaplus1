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

  AuthState({
    this.status = AuthStatus.initial,
    this.token,
    this.phoneNumber,
    this.referralCount,
    this.errorMessage,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? token,
    String? phoneNumber,
    int? referralCount,
    String? errorMessage,
  }) =>
      AuthState(
        status: status ?? this.status,
        token: token ?? this.token,
        phoneNumber: phoneNumber ?? this.phoneNumber,
        referralCount: referralCount ?? this.referralCount,
        errorMessage: errorMessage ?? this.errorMessage,
      );
}

class AuthNotifier extends StateNotifier<AuthState> {
  final DioClient _dioClient;

  AuthNotifier(this._dioClient) : super(AuthState()) {
    _checkExistingToken();
  }

  Future<void> _checkExistingToken() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('patient_token');
    if (token != null) {
      state = state.copyWith(
        status: AuthStatus.authenticated,
        token: token,
        phoneNumber: prefs.getString('patient_phone'),
      );
    }
  }

  Future<bool> login(String phoneNumber, String code) async {
    state = state.copyWith(status: AuthStatus.loading);
    try {
      final response = await _dioClient.dio.post(
        ApiConstants.patientLogin,
        data: PatientLoginRequest(
          phoneNumber: phoneNumber,
          code: code,
        ).toJson(),
      );

      final result = PatientLoginResponse.fromJson(response.data);

      if (result.success && result.token != null) {
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('patient_token', result.token!);
        await prefs.setString('patient_phone', result.phoneNumber ?? phoneNumber);

        state = state.copyWith(
          status: AuthStatus.authenticated,
          token: result.token,
          phoneNumber: result.phoneNumber,
          referralCount: result.referralCount,
        );
        return true;
      } else {
        state = state.copyWith(
          status: AuthStatus.error,
          errorMessage: result.error,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        status: AuthStatus.error,
        errorMessage: e.toString(),
      );
      return false;
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('patient_token');
    await prefs.remove('patient_phone');
    state = AuthState();
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.watch(dioClientProvider));
});
