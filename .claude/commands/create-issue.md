---
allowed-tools:
  - TodoWrite
  - TodoRead
  - Bash
description:
This command creates a TODO list for the specified issue and generates corresponding GitHub issues using the gh CLI tool.
If the command is run in an environment where gh is not installed, it should display an error message suggesting the user install the gh tool.
---

This command will create a GitHub-issue based on $ARGUEMENTS.

# 🚨 **Importants** 🚨

**This command will create GitHub-issue only.**

## 📋 **This command will be follow steps**
1. **THINK** about specified issue and analyze it then break down issue to TODOs.
2. Create a markdown text to upload as GitHub-issue.
3. Display markdown text that will create and ask me approve.
4. Create a GitHub-issue by `gh` CLI.

## 🚫 **Deny**
- ✋ This command **NEVER** implement or modify any codes.
- ✋ This command **NEVER** create or delete and update any files.
- ✋ This command **NEVER** refers to how implements for.


---

### Add tags for Issue will be created
This command will create a tags for issue to categrize them.
You **SHOULD** analyze content and categorize it whether but report or new feature request
You can pick up one or some from belows.
- feature
  - This Tag will be attached for New feature request.
- bug
  - This Tag will be attached for BugReport.

---

## Issue Templates

### Basic
```markdown
## 概要
[Title of this Issue]

## 背景・動機
[なぜこの Issue が必要なのか]

## 詳細
[具体的な内容や仕様]

## TODO
- [ ] 基準1
- [ ] 基準2
- [ ] 基準3

## 追加情報
[その他の情報、参考資料など]
```

### BugReport
```markdown
## バグの概要
[バグの概要を記述]

## 再現手順
1. [手順1]
2. [手順2]
3. [手順3]

## 期待する動作
[期待していた動作]

## 実際の動作
[実際に起こった動作]

## 環境
- OS:
- Unity Version:
- その他関連する情報:

## 補足情報
[スクリーンショット、ログ、その他の情報]
```

### 機能要求構造
```markdown
## 機能の概要
[機能の概要を記述]

## ユーザーストーリー
[ユーザー名]として、[機能]を使って、[目的]を達成したい。

## 詳細仕様
[具体的な仕様や動作]

## 受け入れ基準
- [ ] 基準1
- [ ] 基準2
- [ ] 基準3

## 技術的考慮事項
[技術的な制約や考慮すべき点]
```

## 基本的なIssue作成コマンド

### シンプルなIssue作成
```bash
gh issue create --title "タイトル" --body "Issue の詳細内容"
```

### ラベル付きIssue作成
```bash
gh issue create --title "タイトル" --body "Issue の詳細内容" --label "bug,priority-high"
```


## 様々なパターンのIssue作成例

### バグレポート
```bash
gh issue create \
  --title "【BUG】BLE接続時にアプリがクラッシュする" \
  --body "## バグの概要
BLE デバイスとの接続時に Unity アプリケーションがクラッシュします。

## 再現手順
1. アプリを起動
2. BLE デバイスをスキャン
3. デバイスに接続を試行
4. アプリがクラッシュ

## 期待する動作
正常に BLE デバイスに接続できること

## 実際の動作
アプリがクラッシュして強制終了される

## 環境
- OS: macOS 14.1
- Unity Version: 6000.0.47f1
- デバイス: iPhone 15 Pro

## 補足情報
クラッシュログを添付予定" \
  --label "bug,priority-high,ble" \
  --assignee "miurayuta"
```

### 機能要求
```bash
gh issue create \
  --title "【FEATURE】BLE接続状態の視覚的インジケーター追加" \
  --body "## 機能の概要
BLE デバイスとの接続状態を視覚的に表示するインジケーターを追加したい。

## ユーザーストーリー
開発者として、BLE デバイスとの接続状態を一目で確認できる UI を使って、デバッグ効率を向上させたい。

## 詳細仕様
- 接続状態を色で表示（緑：接続中、赤：切断、黄：接続試行中）
- 接続中のデバイス名を表示
- 信号強度の表示

## 受け入れ基準
- [ ] 接続状態が色で表示される
- [ ] デバイス名が表示される
- [ ] 信号強度が表示される
- [ ] リアルタイムで状態が更新される

## 技術的考慮事項
- UI Toolkit を使用
- 既存の BLE マネージャーとの連携" \
  --label "enhancement,ui,ble" \
  --assignee "miurayuta"
```

### タスク・改善
```bash
gh issue create \
  --title "【TASK】BLE プラグインのドキュメント整備" \
  --body "## 概要
BLE プラグインの使用方法や設定に関するドキュメントを整備する。

## 背景・動機
現在ドキュメントが不足しており、新しい開発者が参加する際の学習コストが高い。

## 詳細
- README.md の更新
- API リファレンスの作成
- サンプルコードの追加
- 設定手順の詳細化

## 受け入れ基準
- [ ] README.md が更新される
- [ ] API リファレンスが作成される
- [ ] サンプルコードが追加される
- [ ] 設定手順が詳細化される

## 追加情報
既存のドキュメントを参考に、統一感のあるスタイルで作成する。" \
  --label "documentation,task" \
  --assignee "miurayuta"
```

## 高度な使用例

### テンプレートファイルを使用したIssue作成
```bash
# テンプレートファイルを作成
cat > issue_template.md << 'EOF'
## 概要
[Issue の概要を記述]

## 背景・動機
[なぜこの Issue が必要なのか]

## 詳細
[具体的な内容や仕様]

## 受け入れ基準
- [ ] 基準1
- [ ] 基準2
- [ ] 基準3
EOF

# テンプレートファイルを使用してIssue作成
gh issue create --title "Issue タイトル" --body-file issue_template.md --label "enhancement"
```

### 対話的なIssue作成
```bash
gh issue create --web
```

## 便利なエイリアス設定

```bash
# ~/.bashrc または ~/.zshrc に追加
alias gh-bug='gh issue create --label "bug" --title'
alias gh-feature='gh issue create --label "enhancement" --title'
alias gh-task='gh issue create --label "task" --title'
```

## 使用例（エイリアス使用）
```bash
gh-bug "BLE接続エラー" --body "詳細な説明"
gh-feature "新機能追加" --body "機能の詳細"
gh-task "ドキュメント更新" --body "タスクの詳細"
```

## 注意事項
- Issue作成後は適切なマイルストーンやプロジェクトに追加することを検討
- 重複したIssueがないか事前に確認
- 適切なラベルを使用してIssueを分類