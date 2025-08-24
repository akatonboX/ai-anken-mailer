# ai-anken-mailerプロジェクト - Clineルールファイル

## プロジェクト概要
ai-anken-mailerは、案件情報メールを自動データ化するIMAPメーラーアプリケーションです。
Azure OpenAI (ChatGPT) を使用してメール本文から案件情報を抽出し、SQLiteデータベースで管理します。

## 技術スタック
- **フレームワーク**: .NET 8.0 WPF (Windows 10.0.22621.0)
- **言語**: C# (nullable有効、ImplicitUsings有効)
- **パッケージ**:
  - Azure.AI.OpenAI (2.1.0) - Azure OpenAI API接続
  - MailKit (4.12.1) - IMAPクライアント  
  - MimeKit (4.12.0) - メール解析
  - Microsoft.Data.Sqlite (9.0.5) - SQLiteデータベース
  - CommunityToolkit.Mvvm (8.4.0) - MVVM実装
  - HtmlAgilityPack (1.12.1) - HTML解析

## プロジェクト構造

### メインファイル
- `AnkenMailer/MainWindow.xaml(.cs)` - メインUIとビジネスロジック
- `AnkenMailer/ConvertContentManager.cs` - AI変換処理の中核
- `AnkenMailer/App.xaml(.cs)` - アプリケーション起動・DB接続管理

### データモデル (AnkenMailer/Model/)
- `Anken.cs` - 案件情報モデル
- `AnkenHeader.cs` - AI解析結果ヘッダ
- `MailItem.cs` - メールアイテム
- `MailMessage.cs` - メール本文
- `MailFolder.cs` - メールフォルダ

### データベーススキーマ (AnkenMailer/ddl.sql)
- **Envelope**: メール基本情報 (MessageId, Date, From, Subject等)
- **Message**: メール本文 (Body)
- **AnkenHeader**: AI解析結果 (HasError, ErrorMessage, JSON)
- **Anken**: 案件情報 (Name, Start, End, Place, Details, MainSkill, 単価等)
- **Skill**: 技術スタック情報
- **LlmUsage**: LLM使用量記録
- **NecessarySkill**: 必要スキル管理

### ウィンドウ類
- `SettingsWindow` - IMAP/AI設定
- `ColumnFilterWindow` - データグリッドフィルタ
- `SelectFolderWindow` - フォルダ選択
- `TotalizationResultWindow` - 統計結果表示
- `LlmUsageWindow` - LLM使用量表示

## 主要機能

### 1. IMAP接続・メール管理
- IMAPサーバー接続設定 (Properties.Settings)
- フォルダ階層表示・選択
- メール一覧表示・ソート・フィルタ
- メール移動・削除・重複整理

### 2. AI案件情報抽出
- Azure OpenAI ChatGPT API使用
- システムプロンプトで案件情報JSON抽出
- 複数案件対応 (配列形式)
- エラーハンドリング・再処理機能

### 3. データ管理
- SQLiteローカルDB
- トランザクション処理
- データエクスポート/インポート
- DB最適化・サイズ表示

### 4. 統計・分析
- 技術スタック別集計
- 単価帯別分析
- フォルダ横断集計

## コーディング規約

### C#スタイル
- nullable reference types有効
- var型推論積極使用
- MVVM パターン (CommunityToolkit.Mvvm使用)
- ObservableObject継承でプロパティ変更通知
- async/await非同期処理

### データベース操作
```csharp
using var command = App.CurrentApp.Connection.CreateCommand();
command.CommandText = "...";
command.Parameters.AddWithValue("@param", value);
```

### AI API呼び出し
```csharp
var chatClient = azureClient.GetChatClient(deploymentName);
var response = chatClient.CompleteChat(messages);
// LlmUsageテーブルに使用量記録
```

### エラーハンドリング
- try-catch でユーザーフレンドリーなメッセージ表示
- MessageBox.Show() でエラー通知
- AnkenHeader.HasError でAI変換エラー管理

## 設定管理
`Properties.Settings` でアプリケーション設定:
- ImapServer, ImapPort, ImapUser, ImapPassword
- Endpoint (Azure OpenAI), DeploymentName, ApiKey
- WebMailPath (Webメール URL)

## ビルド・デプロイ
```bash
# NuGet復元
dotnet restore AnkenMailer/AnkenMailer.csproj

# WPFビルド  
dotnet build AnkenMailer/AnkenMailer.csproj -c Release

# パッケージ化
msbuild AnkenMailer-app/AnkenMailer-app.wapproj /p:Configuration=Release /p:Platform=x86
```

## 注意点

### パフォーマンス
- IMAPクライアント接続はusing文で適切にDispose
- AI API呼び出しは直列実行 (並列処理でサーバーエラー回避)
- DataGrid大量データ表示時の仮想化対応

### セキュリティ
- IMAP認証情報は暗号化設定ファイルに保存
- Azure OpenAI APIキーは設定で管理

### データ整合性
- SQLite外部キー制約有効
- トランザクション使用
- データベース破損時の自動初期化

## 開発時のベストプラクティス
1. 非同期処理では適切なCancellationToken使用
2. IDisposableリソースはusing文必須
3. NULL許容型を活用した安全なコード
4. MVVMパターンでUI/ビジネスロジック分離
5. エラー情報はユーザー向けとログ向けを使い分け

## デバッグ・テスト
- Visual Studio 2022推奨
- SQLite Browser でデータ確認
- Azure OpenAI Studioでプロンプト検証
- メモリ使用量監視 (大量メール処理時)
