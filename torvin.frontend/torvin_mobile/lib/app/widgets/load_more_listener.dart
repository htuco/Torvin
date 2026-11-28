import 'package:flutter/widgets.dart';

import 'paged_list_footer.dart';

/// Traži sljedeću stranicu paginirane liste kad je korisnik blizu kraja skrola. `onLoadMore` je obično
/// `PagedNotifier.loadMore`, koji sam ne radi ništa dok stranica već stiže ili kad stranica više nema.
class LoadMoreListener extends StatelessWidget {
  const LoadMoreListener({super.key, required this.onLoadMore, required this.child});

  final VoidCallback onLoadMore;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return NotificationListener<ScrollNotification>(
      onNotification: (notification) {
        if (notification.metrics.extentAfter < PagedListFooter.loadMoreThreshold) {
          onLoadMore();
        }
        return false;
      },
      child: child,
    );
  }
}
