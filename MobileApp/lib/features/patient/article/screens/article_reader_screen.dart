import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_markdown/flutter_markdown.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../../core/constants/app_colors.dart';
import '../../auth/providers/auth_provider.dart';

class ArticleReaderScreen extends ConsumerStatefulWidget {
  final String referralId;
  final String articleId;
  const ArticleReaderScreen({
    super.key,
    required this.referralId,
    required this.articleId,
  });

  @override
  ConsumerState<ArticleReaderScreen> createState() =>
    _ArticleReaderScreenState();
}

class _ArticleReaderScreenState
    extends ConsumerState<ArticleReaderScreen> {

  String? _content;
  String? _title;
  bool _loading = true;
  String? _error;
  double _scrollDepth = 0;
  final _scrollController = ScrollController();

  String get referralId => widget.referralId;

  @override
  void initState() {
    super.initState();
    _loadArticle();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final max = _scrollController.position.maxScrollExtent;
    if (max == 0) return;
    final current = _scrollController.position.pixels;
    setState(() => _scrollDepth = (current / max).clamp(0.0, 1.0));
  }

  Future<void> _loadArticle() async {
    try {
      final token = ref.read(authProvider).token ?? '';
      final dio = Dio(BaseOptions(
        baseUrl: 'https://muafaplus1-production.up.railway.app/api/v1',
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));
      final response = await dio.get('/referrals/$referralId/articles');
      print('DEBUG articles for reader: ${response.data}');
      final list = response.data['data'] as List? ?? [];

      Map<String, dynamic>? article;
      for (final a in list) {
        if ((a['articleId'] ?? a['id'] ?? '') == widget.articleId) {
          article = a as Map<String, dynamic>;
          break;
        }
      }
      article ??= list.isNotEmpty ? list.first as Map<String, dynamic> : null;

      if (article != null) {
        print('DEBUG matched article: $article');
        setState(() {
          _title   = article!['title'] ?? article['articleTitle'] ??
            article['heading'] ?? 'مقال طبي';
          _content = article['content'] ?? article['articleContent'] ??
            article['body'] ?? '';
          _loading = false;
        });
      } else {
        setState(() { _error = 'المقال غير موجود'; _loading = false; });
      }
    } catch (e) {
      print('DEBUG article load error: $e');
      setState(() { _error = 'تعذّر تحميل المقال'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: AppColors.ink50,
        appBar: AppBar(
          backgroundColor: AppColors.navy600,
          foregroundColor: AppColors.white,
          elevation: 0,
          title: Text(_title ?? 'قراءة المقال',
            style: GoogleFonts.ibmPlexSansArabic(
              fontWeight: FontWeight.w700,
              color: AppColors.white,
              fontSize: 16),
            maxLines: 1,
            overflow: TextOverflow.ellipsis),
          leading: IconButton(
            icon: const Icon(Icons.arrow_forward, color: AppColors.white),
            onPressed: () => Navigator.of(context).pop(),
          ),
          bottom: PreferredSize(
            preferredSize: const Size.fromHeight(3),
            child: LinearProgressIndicator(
              value: _scrollDepth,
              backgroundColor: AppColors.white.withOpacity(0.2),
              valueColor: const AlwaysStoppedAnimation(AppColors.green500),
              minHeight: 3,
            ),
          ),
        ),
        body: _loading
          ? const Center(child: CircularProgressIndicator(
              color: AppColors.navy600))
          : _error != null
            ? Center(child: Text(_error!,
                style: GoogleFonts.ibmPlexSansArabic(
                  color: AppColors.ink500)))
            : Markdown(
                controller: _scrollController,
                data: _content ?? '',
                padding: const EdgeInsets.all(20),
                styleSheet: MarkdownStyleSheet(
                  h1: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 20, fontWeight: FontWeight.w700,
                    color: AppColors.navy600),
                  h2: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 17, fontWeight: FontWeight.w700,
                    color: AppColors.navy600),
                  h3: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 15, fontWeight: FontWeight.w600,
                    color: AppColors.ink900),
                  p: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 15, color: AppColors.ink700,
                    height: 1.8),
                  listBullet: GoogleFonts.ibmPlexSansArabic(
                    fontSize: 15, color: AppColors.ink700),
                  strong: GoogleFonts.ibmPlexSansArabic(
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink900),
                  blockquoteDecoration: BoxDecoration(
                    color: AppColors.navy600.withOpacity(0.05),
                    borderRadius: BorderRadius.circular(8),
                    border: const Border(left: BorderSide(
                      color: AppColors.orange500, width: 3))),
                ),
              ),
        bottomNavigationBar: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: 20, vertical: 12),
          decoration: const BoxDecoration(
            color: AppColors.white,
            border: Border(top: BorderSide(color: AppColors.ink100))),
          child: SafeArea(
            top: false,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                _ReactionButton(
                  icon: Icons.thumb_up_outlined,
                  label: 'مفيد',
                  color: AppColors.green500,
                  onTap: () => _submitReaction('Like'),
                ),
                _ReactionButton(
                  icon: Icons.thumb_down_outlined,
                  label: 'يحتاج تحسين',
                  color: AppColors.riskHighText,
                  onTap: () => _submitReaction('Dislike'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _submitReaction(String reaction) async {
    try {
      final token = ref.read(authProvider).token ?? '';
      final dio = Dio(BaseOptions(
        baseUrl: 'https://muafaplus1-production.up.railway.app/api/v1',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));
      // POST /articles/{articleId}/engagement with eventType "like" or "dislike"
      await dio.post('/articles/${widget.articleId}/engagement',
        data: {
          'referralId': widget.referralId,
          'eventType': reaction.toLowerCase(),
        });
    } catch (e) {
      print('DEBUG reaction error: $e');
    }
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text('شكراً على تقييمك',
          style: GoogleFonts.ibmPlexSansArabic()),
        backgroundColor: AppColors.green500));
    }
  }
}

class _ReactionButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback onTap;

  const _ReactionButton({
    required this.icon,
    required this.label,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: 24, vertical: 10),
        decoration: BoxDecoration(
          color: color.withOpacity(0.08),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: color.withOpacity(0.3)),
        ),
        child: Row(children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: 8),
          Text(label, style: GoogleFonts.ibmPlexSansArabic(
            fontSize: 14, color: color,
            fontWeight: FontWeight.w600)),
        ]),
      ),
    );
  }
}
