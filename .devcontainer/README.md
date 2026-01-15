# Dev Container - OpenTelemetry .NET 8 Development Environment

このDevcontainerは、Jaegerと.NET 8開発環境をまとめて起動するための設定です。

## ✨ 主な機能

- **Docker in Docker (dind)**: 開発コンテナ内からDockerコマンドが使用可能
- **Jaeger自動起動**: docker-composeにより、Jaegerが自動的に起動
- **.NET 8 SDK**: 最新の.NET開発環境がプリインストール
- **OpenTelemetry統合**: トレース・メトリクス・ログの収集設定が事前構成済み

## 🚀 起動方法

### VS Codeでの起動

1. VS Codeでこのプロジェクトを開く
2. コマンドパレット（`Ctrl+Shift+P` / `Cmd+Shift+P`）を開く
3. `Dev Containers: Reopen in Container` を選択
4. コンテナのビルドと起動が完了するまで待つ

### 手動起動（Docker Compose）

```bash
cd .devcontainer
docker-compose up -d
```

## 📦 含まれるサービス

| サービス | 説明 | ポート | 起動方法 |
|---------|------|--------|----------|
| **app** | .NET 8 開発コンテナ | - | DevContainer起動時に自動 |
| **jaeger** | 分散トレーシングシステム | 複数 | docker-composeで自動起動 |

> 💡 **Note**: Jaegerサービスはdocker-compose.ymlで定義されており、DevContainer起動時に`depends_on`により自動的に起動されます。手動での起動は不要です。

## 🔌 Jaeger ポート一覧

| ポート | プロトコル | 用途 |
|--------|-----------|------|
| `16686` | HTTP | Jaeger UI |
| `4317` | gRPC | OTLP gRPC Collector |
| `4318` | HTTP | OTLP HTTP Collector |
| `14268` | HTTP | Jaeger Thrift HTTP (レガシー) |
| `14250` | gRPC | Jaeger gRPC (レガシー) |

## 🌐 Jaeger UIへのアクセス

コンテナ起動後、以下のURLでJaeger UIにアクセスできます：

```
http://localhost:16686
```

## ⚙️ OpenTelemetry 環境変数

コンテナには以下の環境変数が設定されています：

| 環境変数 | 値 | 説明 |
|---------|-----|------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | OTLPエクスポーターのエンドポイント |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | OTLPプロトコル |
| `OTEL_SERVICE_NAME` | `TracingSample` | サービス名 |
| `OTEL_TRACES_EXPORTER` | `otlp` | トレースエクスポーター |
| `OTEL_METRICS_EXPORTER` | `otlp` | メトリクスエクスポーター |
| `OTEL_LOGS_EXPORTER` | `otlp` | ログエクスポーター |

## 🛠️ インストール済みツール

### .NET関連
- .NET 8 SDK
- dotnet CLI

### Docker関連 (Docker in Docker)
- Docker Engine
- Docker Compose v2

> 💡 **Docker in Docker (dind)機能について**:  
> `ghcr.io/devcontainers/features/docker-in-docker:2` featureにより、開発コンテナ内で直接Dockerコマンドを実行できます。これにより、コンテナ内から追加のDockerコンテナを起動・管理することが可能です。

```bash
# コンテナ内でDockerコマンドを使用する例
docker ps                    # 実行中のコンテナ一覧
docker images                # ローカルイメージ一覧
docker-compose version       # Docker Composeのバージョン確認
docker build -t myapp .      # 新しいイメージのビルド
```

### 開発ツール
- git
- curl / wget
- vim / nano
- jq (JSON処理)
- htop (プロセスモニタリング)
- zsh + oh-my-zsh

### VS Code拡張機能
- C#
- C# Dev Kit
- Docker
- GitLens
- .NET Test Explorer
- REST Client
- YAML

## 📝 使用例

### アプリケーションの実行

```bash
# ワークスペースに移動
cd /workspaces/opentelemtry/TracingSample

# 依存関係の復元
dotnet restore

# プロジェクトの実行
dotnet run --project src/TracingSample.Console
```

### トレースの確認

1. アプリケーションを実行
2. Jaeger UI (`http://localhost:16686`) を開く
3. "Service" ドロップダウンから `TracingSample` を選択
4. "Find Traces" をクリック

## 🔧 トラブルシューティング

### Jaegerに接続できない場合

```bash
# Jaegerコンテナの状態確認
docker ps | grep jaeger

# Jaegerのログ確認
docker logs jaeger

# ネットワーク接続テスト（コンテナ内から）
curl -v http://jaeger:4318/v1/traces
```

### コンテナが起動しない場合

```bash
# ログの確認
docker-compose -f .devcontainer/docker-compose.yml logs

# コンテナの再ビルド
docker-compose -f .devcontainer/docker-compose.yml build --no-cache
docker-compose -f .devcontainer/docker-compose.yml up -d
```

## 📁 ファイル構成

```
.devcontainer/
├── devcontainer.json    # DevContainer設定 (features, 拡張機能, 環境変数)
├── Dockerfile           # 開発コンテナイメージ
├── docker-compose.yml   # コンテナオーケストレーション (app + jaeger)
└── README.md            # このファイル
```

### 主要設定ファイルの役割

| ファイル | 役割 |
|---------|------|
| `devcontainer.json` | VS Code Dev Containersの設定。Docker in Docker featureの有効化、VS Code拡張機能、環境変数を定義 |
| `docker-compose.yml` | appコンテナとjaegerコンテナを定義。otel-networkで両コンテナを接続 |
| `Dockerfile` | 開発コンテナのベースイメージと追加ツールをインストール |

## 🔗 関連リンク

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
