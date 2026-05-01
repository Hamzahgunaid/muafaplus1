import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_colors.dart';
import '../../auth/providers/auth_provider.dart';

class ChatThread {
  final String referralId;
  final String diagnosis;
  final String riskLevel;
  final bool chatEnabled;
  final int messageCount;

  ChatThread({
    required this.referralId,
    required this.diagnosis,
    required this.riskLevel,
    required this.chatEnabled,
    required this.messageCount,
  });
}

final patientChatThreadsProvider = FutureProvider<List<ChatThread>>((ref) async {
  final auth = ref.watch(authProvider);
  if (auth.token == null) return [];
  final dio = Dio();
  final resp = await dio.get(
    'https://muafaplus1-production.up.railway.app/api/v1/referrals',
    options: Options(headers: {'Authorization': 'Bearer ${auth.token}'}),
  );
  final data = resp.data['data'] as List? ?? [];
  return data
      .where((e) => e['chatEnabled'] == true)
      .map((e) => ChatThread(
            referralId: e['referralId'] ?? '',
            diagnosis: e['primaryDiagnosis'] ?? 'إحالة طبية',
            riskLevel: e['riskLevel'] ?? 'LOW',
            chatEnabled: e['chatEnabled'] ?? false,
            messageCount: 0,
          ))
      .toList();
});

class PatientChatListScreen extends ConsumerWidget {
  const PatientChatListScreen({super.key});

  Color _riskColor(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return const Color(0xFFD64545);
      case 'HIGH':     return const Color(0xFFD85A30);
      case 'MODERATE': return const Color(0xFFB8771F);
      default:         return const Color(0xFF197540);
    }
  }

  Color _riskBgColor(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return const Color(0xFFFBE5E5);
      case 'HIGH':     return const Color(0xFFFDECE2);
      case 'MODERATE': return const Color(0xFFFDF3E1);
      default:         return const Color(0xFFE6F4EC);
    }
  }

  String _riskLabel(String level) {
    switch (level.toUpperCase()) {
      case 'CRITICAL': return 'حرج';
      case 'HIGH':     return 'مرتفع';
      case 'MODERATE': return 'متوسط';
      default:         return 'منخفض';
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final threadsAsync = ref.watch(patientChatThreadsProvider);

    return Scaffold(
      backgroundColor: const Color(0xFFF6F7FB),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1E3A72),
        foregroundColor: Colors.white,
        title: const Text(
          'المحادثات',
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
        ),
        centerTitle: true,
        elevation: 0,
      ),
      body: threadsAsync.when(
        data: (threads) {
          if (threads.isEmpty) {
            return const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.chat_bubble_outline,
                      size: 48, color: Color(0xFFB7BDCB)),
                  SizedBox(height: 12),
                  Text(
                    'لا توجد محادثات نشطة',
                    style: TextStyle(color: Color(0xFF8A93A6), fontSize: 14),
                  ),
                  SizedBox(height: 6),
                  Text(
                    'ستظهر محادثاتك مع طبيبك هنا',
                    style: TextStyle(color: Color(0xFFB7BDCB), fontSize: 12),
                  ),
                ],
              ),
            );
          }
          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: threads.length,
            itemBuilder: (ctx, i) {
              final t = threads[i];
              return GestureDetector(
                onTap: () => context.push('/referral/${t.referralId}'),
                child: Container(
                  margin: const EdgeInsets.only(bottom: 12),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: const Color(0xFFEEF0F5)),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFF0E1726).withOpacity(0.05),
                        blurRadius: 10,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Row(
                      children: [
                        Container(
                          width: 44, height: 44,
                          decoration: BoxDecoration(
                            color: const Color(0xFFEEF1F7),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Icon(
                            Icons.chat_bubble_outline,
                            color: Color(0xFF1E3A72),
                            size: 20,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                t.diagnosis,
                                style: const TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF0E1726),
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              const SizedBox(height: 4),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(
                                  color: _riskBgColor(t.riskLevel),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  'خطر ${_riskLabel(t.riskLevel)}',
                                  style: TextStyle(
                                    fontSize: 10,
                                    fontWeight: FontWeight.w600,
                                    color: _riskColor(t.riskLevel),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_left,
                            color: Color(0xFFB7BDCB), size: 18),
                      ],
                    ),
                  ),
                ),
              );
            },
          );
        },
        loading: () => const Center(
            child: CircularProgressIndicator(color: Color(0xFF1E3A72))),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.wifi_off_outlined,
                  size: 40, color: Color(0xFFB7BDCB)),
              const SizedBox(height: 12),
              const Text('تعذّر تحميل المحادثات',
                  style: TextStyle(color: Color(0xFF5A6478), fontSize: 14)),
              const SizedBox(height: 12),
              TextButton(
                onPressed: () => ref.refresh(patientChatThreadsProvider),
                child: const Text('إعادة المحاولة',
                    style: TextStyle(color: Color(0xFF1E3A72))),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
