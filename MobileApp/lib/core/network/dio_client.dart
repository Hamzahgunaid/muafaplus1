import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../constants/api_constants.dart';

final tokenProvider = StateProvider<String?>((ref) => null);

final dioClientProvider = Provider<DioClient>((ref) {
  final token = ref.watch(tokenProvider);
  return DioClient(token: token);
});

class DioClient {
  late final Dio _dio;

  DioClient({String? token}) {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConstants.baseUrl,
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    ));
  }

  Dio get dio => _dio;
}
