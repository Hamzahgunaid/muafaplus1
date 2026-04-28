class ApiResponse<T> {
  final bool success;
  final T? data;
  final String? error;
  final int? statusCode;

  const ApiResponse({
    required this.success,
    this.data,
    this.error,
    this.statusCode,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(dynamic)? fromData,
  ) {
    return ApiResponse(
      success: json['success'] ?? false,
      data: json['data'] != null && fromData != null
        ? fromData(json['data'])
        : null,
      error: json['error'] as String?,
    );
  }

  bool get isSuccess => success && error == null;
}
