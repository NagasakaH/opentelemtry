# .NET 8 SDK Development Container
# OpenTelemetry 1.14.0 対応プロジェクト用

# ベースイメージ: Microsoft公式 .NET 8 SDK
FROM --platform=linux/amd64 mcr.microsoft.com/vscode/devcontainers/dotnet:8.0-bookworm

# メタデータ
LABEL maintainer="development-team"
LABEL description="Development container for .NET 8 with OpenTelemetry"
LABEL dotnet.version="8.0"
LABEL opentelemetry.version="1.14.0"

# 環境変数
ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=true

# 開発に必要なツールをインストール
RUN apt-get update && apt-get install -y --no-install-recommends \
    # バージョン管理
    git \
    # ネットワークツール
    curl \
    wget \
    ca-certificates \
    # エディタ・ユーティリティ
    vim \
    nano \
    less \
    # ビルド関連
    build-essential \
    # プロセス管理
    procps \
    htop \
    # ネットワーク診断
    net-tools \
    iputils-ping \
    dnsutils \
    # JSON/YAML処理
    jq \
    # ファイル操作
    unzip \
    zip \
    tar \
    # ロケール
    locales \
    # sudo
    sudo \
    # zsh (オプション)
    zsh \
    # クリーンアップ
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ロケール設定
RUN sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen && \
    locale-gen
ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8

