import 'package:hive_flutter/hive_flutter.dart';

class HiveService {
  static const _tokenBox = 'auth';

  static Future<void> init() async {
    await Hive.initFlutter();
    await Hive.openBox(_tokenBox);
  }

  static Box get authBox => Hive.box(_tokenBox);
}
