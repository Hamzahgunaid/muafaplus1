import 'package:flutter/material.dart';
import 'package:flutter_markdown/flutter_markdown.dart';

enum ArticleOutlineState { notGenerated, generating, generated }

class ArticleOutlineCard extends StatefulWidget {
  final int index;
  final String title;
  final ArticleOutlineState state;
  final VoidCallback? onGenerate;
  final VoidCallback? onView;
  final String? content;

  const ArticleOutlineCard({
    super.key,
    required this.index,
    required this.title,
    required this.state,
    this.onGenerate,
    this.onView,
    this.content,
  });

  @override
  State<ArticleOutlineCard> createState() => _ArticleOutlineCardState();
}

class _ArticleOutlineCardState extends State<ArticleOutlineCard> {
  bool _expanded = false;

  @override
  void didUpdateWidget(ArticleOutlineCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Collapse if no longer generated
    if (widget.state != ArticleOutlineState.generated) {
      _expanded = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: _expanded
              ? const Color(0xFF1E3A72)
              : const Color(0xFFEEF0F5),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Index badge
                Container(
                  width: 28, height: 28,
                  decoration: const BoxDecoration(
                    color: Color(0xFF1E3A72),
                    shape: BoxShape.circle,
                  ),
                  child: Center(
                    child: Text('${widget.index}',
                        style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: Colors.white)),
                  ),
                ),
                const SizedBox(width: 10),
                // Title
                Expanded(
                  child: Text(
                    widget.title.isNotEmpty ? widget.title : 'مقال طبي',
                    style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF0E1726)),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                const SizedBox(width: 10),
                // Action button
                _buildActionButton(),
              ],
            ),
          ),
          // Inline content (only when expanded, no onView override, and content available)
          if (_expanded &&
              widget.state == ArticleOutlineState.generated &&
              widget.content != null &&
              widget.onView == null) ...[
            const Divider(height: 1, color: Color(0xFFEEF0F5)),
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 14),
              child: MarkdownBody(
                data: widget.content!,
                styleSheet: MarkdownStyleSheet(
                  p: const TextStyle(
                      fontSize: 14,
                      color: Color(0xFF2D3748),
                      height: 1.7),
                  h2: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF1E3A72)),
                  h3: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF0E1726)),
                  listBullet: const TextStyle(
                      fontSize: 14, color: Color(0xFF2D3748)),
                  strong: const TextStyle(
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF0E1726)),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildActionButton() {
    switch (widget.state) {
      case ArticleOutlineState.notGenerated:
        return OutlinedButton(
          onPressed: widget.onGenerate,
          style: OutlinedButton.styleFrom(
            side: const BorderSide(color: Color(0xFF1E3A72)),
            foregroundColor: const Color(0xFF1E3A72),
            padding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            minimumSize: Size.zero,
            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8)),
          ),
          child: const Text('توليد المقال',
              style:
                  TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
        );

      case ArticleOutlineState.generating:
        return OutlinedButton.icon(
          onPressed: null,
          icon: const SizedBox(
              width: 12,
              height: 12,
              child:
                  CircularProgressIndicator(strokeWidth: 1.5, color: Color(0xFF8A93A6))),
          label: const Text('جاري التوليد...',
              style: TextStyle(fontSize: 12)),
          style: OutlinedButton.styleFrom(
            side: const BorderSide(color: Color(0xFFB7BDCB)),
            foregroundColor: const Color(0xFF8A93A6),
            padding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            minimumSize: Size.zero,
            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8)),
          ),
        );

      case ArticleOutlineState.generated:
        return ElevatedButton(
          onPressed: () {
            if (widget.onView != null) {
              widget.onView!();
            } else {
              setState(() => _expanded = !_expanded);
            }
          },
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF1E3A72),
            foregroundColor: Colors.white,
            padding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            minimumSize: Size.zero,
            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            elevation: 0,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8)),
          ),
          child: const Text('عرض المقال',
              style:
                  TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
        );
    }
  }
}
