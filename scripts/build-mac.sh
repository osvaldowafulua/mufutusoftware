#!/bin/bash

# 🚀 Ayomant - Build Rápido para Mac
# Este script cria a aplicação desktop para Mac

echo "🚀 Ayomant - Build para Mac"
echo "============================"
echo ""

# Verificar se estamos no diretório correto
if [ ! -f "package.json" ]; then
    echo "❌ Erro: Execute este script na raiz do projeto Ayomant"
    exit 1
fi

# Verificar se o Node.js está instalado
if ! command -v node &> /dev/null; then
    echo "❌ Erro: Node.js não está instalado"
    echo "Instale com: brew install node"
    exit 1
fi

# Verificar se o npm está instalado
if ! command -v npm &> /dev/null; then
    echo "❌ Erro: npm não está instalado"
    exit 1
fi

echo "✅ Node.js: $(node --version)"
echo "✅ npm: $(npm --version)"
echo ""

# Instalar dependências se necessário
if [ ! -d "node_modules" ]; then
    echo "📦 Instalando dependências..."
    npm install
    echo "✅ Dependências instaladas"
else
    echo "✅ Dependências já instaladas"
fi

echo ""

# Navegar para o frontend
echo "🌐 Configurando frontend..."
cd apps/web

# Verificar se o Electron está instalado
if ! npm list electron &> /dev/null; then
    echo "📦 Instalando Electron..."
    npm install electron electron-builder --save-dev
    echo "✅ Electron instalado"
else
    echo "✅ Electron já instalado"
fi

# Criar diretórios necessários
echo "📁 Criando diretórios..."
mkdir -p assets
mkdir -p build
mkdir -p scripts

# Criar arquivo de entitlements para Mac
echo "🔐 Configurando permissões para Mac..."
cat > build/entitlements.mac.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.debugger</key>
    <true/>
    <key>com.apple.security.network.client</key>
    <true/>
    <key>com.apple.security.network.server</key>
    <true/>
    <key>com.apple.security.files.user-selected.read-write</key>
    <true/>
</dict>
</plist>
EOF

# Criar script de notarização (placeholder)
echo "📝 Criando script de notarização..."
cat > scripts/notarize.js << EOF
// Script de notarização para Mac App Store
// Implementar conforme necessário
console.log('Notarização não implementada ainda');
EOF

# Criar ícone placeholder
echo "🎨 Criando ícone placeholder..."
cat > assets/icon.icns << EOF
# Este é um placeholder para o ícone
# Substitua por um arquivo .icns real
EOF

echo "✅ Configuração concluída"
echo ""

# Build da aplicação
echo "🔨 Fazendo build da aplicação..."
npm run build

if [ $? -eq 0 ]; then
    echo "✅ Build concluído com sucesso"
else
    echo "❌ Erro no build"
    exit 1
fi

echo ""

# Criar aplicação desktop
echo "🖥️  Criando aplicação desktop para Mac..."
npm run package:mac

if [ $? -eq 0 ]; then
    echo ""
    echo "🎉 Aplicação desktop criada com sucesso!"
    echo "========================================"
    echo ""
    echo "📁 Arquivos criados em:"
    echo "   - apps/web/dist-electron/"
    echo ""
    echo "📱 Para instalar no Mac:"
    echo "   1. Abra a pasta: apps/web/dist-electron/"
    echo "   2. Arraste o arquivo .dmg para a pasta Aplicações"
    echo "   3. Ou use o arquivo .zip para instalação manual"
    echo ""
    echo "🚀 Para testar a aplicação:"
    echo "   cd apps/web && npm run create-desktop"
    echo ""
    echo "✨ Obrigado por usar o Ayomant!"
else
    echo "❌ Erro ao criar aplicação desktop"
    exit 1
fi

