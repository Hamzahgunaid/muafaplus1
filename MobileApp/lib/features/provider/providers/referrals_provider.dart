import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/physician_model.dart';
import '../../../core/network/dio_client.dart';
import 'physician_auth_provider.dart';

final recentReferralsProvider = FutureProvider<List<ReferralSummary>>((ref) async {
  final auth = ref.watch(physicianAuthProvider);
  if (!auth.isLoggedIn) return [];

  final dio = DioClient.instanceWithToken(auth.token!);
  final res = await dio.get('/referrals', queryParameters: {
    'page': 1,
    'pageSize': 20,
  });

  final items = res.data['data'] as List? ?? [];
  return items
      .map((e) => ReferralSummary.fromJson(e as Map<String, dynamic>))
      .toList();
});
