import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
      connectTimeout: ApiConstants.connectTimeout,
      receiveTimeout: ApiConstants.receiveTimeout,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    ));
  }

  Dio get dio => _dio;

  static Dio get instance => Dio(BaseOptions(
    baseUrl: ApiConstants.baseUrl,
    connectTimeout: ApiConstants.connectTimeout,
    receiveTimeout: ApiConstants.receiveTimeout,
    headers: {'Content-Type': 'application/json'},
  ));

  static Dio instanceWithToken(String token) => Dio(BaseOptions(
    baseUrl: ApiConstants.baseUrl,
    connectTimeout: ApiConstants.connectTimeout,
    receiveTimeout: ApiConstants.receiveTimeout,
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
  ));
}
