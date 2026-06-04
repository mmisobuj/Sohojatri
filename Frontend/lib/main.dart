import 'package:flutter/material.dart';

void main() {
  runApp(const SohojatriApp());
}

class SohojatriApp extends StatelessWidget {
  const SohojatriApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Sohojatri',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.green),
      ),
      home: const HomePage(),
    );
  }
}

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Sohojatri Frontend'),
      ),
      body: const Center(
        child: Text('Welcome to Sohojatri Flutter frontend'),
      ),
    );
  }
}
