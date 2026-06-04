import 'package:flutter_test/flutter_test.dart';
import 'package:sohojatri_frontend/main.dart';

void main() {
  testWidgets('App renders frontend title', (WidgetTester tester) async {
    await tester.pumpWidget(const SohojatriApp());

    expect(find.text('Sohojatri Frontend'), findsOneWidget);
    expect(find.text('Welcome to Sohojatri Flutter frontend'), findsOneWidget);
  });
}
