class ReferralArticle {
  final String articleId;
  final String articleType;
  final String title;
  final String contentAr;
  final int wordCount;

  const ReferralArticle({
    required this.articleId,
    required this.articleType,
    required this.title,
    required this.contentAr,
    required this.wordCount,
  });

  bool get isSummary => articleType == 'summary';
  bool get isDetailed => articleType == 'detailed';

  factory ReferralArticle.fromJson(Map<String, dynamic> j) => ReferralArticle(
        articleId: j['articleId'] as String? ?? '',
        articleType: j['articleType'] as String? ?? 'detailed',
        title: j['title'] as String? ?? '',
        contentAr: j['content_ar'] as String? ?? '',
        wordCount: j['wordCount'] as int? ?? 0,
      );
}
