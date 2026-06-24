#!/bin/bash

# 🚀 Ayomant - Script de Instalação para Mac
# Este script instala o Ayomant no seu Mac

echo "🚀 Bem-vindo ao Ayomant - Intelligent Maintenance & Asset Management"
echo "================================================================"
echo ""

# Verificar se o Homebrew está instalado
if ! command -v brew &> /dev/null; then
    echo "📦 Instalando Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    echo "✅ Homebrew instalado com sucesso!"
else
    echo "✅ Homebrew já está instalado"
fi

# Verificar se o Node.js está instalado
if ! command -v node &> /dev/null; then
    echo "📦 Instalando Node.js..."
    brew install node
    echo "✅ Node.js instalado com sucesso!"
else
    echo "✅ Node.js já está instalado (versão: $(node --version))"
fi

# Verificar se o Docker está instalado
if ! command -v docker &> /dev/null; then
    echo "📦 Instalando Docker Desktop..."
    brew install --cask docker
    echo "✅ Docker Desktop instalado com sucesso!"
    echo "⚠️  Por favor, abra o Docker Desktop e aceite os termos de uso"
else
    echo "✅ Docker já está instalado"
fi

# Verificar se o Git está instalado
if ! command -v git &> /dev/null; then
    echo "📦 Instalando Git..."
    brew install git
    echo "✅ Git instalado com sucesso!"
else
    echo "✅ Git já está instalado (versão: $(git --version))"
fi

echo ""
echo "🔧 Configurando o ambiente Ayomant..."

# Criar diretório para o projeto
PROJECT_DIR="$HOME/Ayomant"
if [ ! -d "$PROJECT_DIR" ]; then
    mkdir -p "$PROJECT_DIR"
    echo "✅ Diretório do projeto criado: $PROJECT_DIR"
else
    echo "✅ Diretório do projeto já existe: $PROJECT_DIR"
fi

# Navegar para o diretório do projeto
cd "$PROJECT_DIR"

# Clonar o repositório (se não existir)
if [ ! -d ".git" ]; then
    echo "📥 Clonando o repositório Ayomant..."
    git clone https://github.com/ayomant/ayomant.git .
    echo "✅ Repositório clonado com sucesso!"
else
    echo "✅ Repositório já está clonado"
fi

# Instalar dependências
echo "📦 Instalando dependências..."
npm install

# Configurar variáveis de ambiente
echo "⚙️  Configurando variáveis de ambiente..."
if [ ! -f ".env.local" ]; then
    cat > .env.local << EOF
NODE_ENV=development
PORT=6000
DB_HOST=localhost
DB_PORT=5433
DB_USERNAME=ayomant
DB_PASSWORD=ayomant123
DB_NAME=ayomant
REDIS_HOST=localhost
REDIS_PORT=6379
JWT_SECRET=ayomant-dev-secret-key-change-in-production
JWT_EXPIRES_IN=7d
JWT_REFRESH_EXPIRES_IN=30d
EOF
    echo "✅ Arquivo .env.local criado"
else
    echo "✅ Arquivo .env.local já existe"
fi

# Iniciar infraestrutura Docker
echo "🐳 Iniciando infraestrutura Docker..."
npm run docker:up

# Aguardar os serviços iniciarem
echo "⏳ Aguardando serviços iniciarem..."
sleep 10

# Executar migrações do banco
echo "🗄️  Configurando banco de dados..."
cd apps/api
DB_PORT=5433 npm run migration:run

# Executar seeds
echo "🌱 Populando banco com dados de exemplo..."
DB_PORT=5433 npm run seed

# Voltar para o diretório raiz
cd ../..

# Criar aplicação desktop
echo "🖥️  Criando aplicação desktop para Mac..."
cd apps/web
npm install electron electron-builder --save-dev

# Criar diretório de assets
mkdir -p assets
mkdir -p build

# Criar ícone básico (placeholder)
echo "🎨 Criando ícone da aplicação..."
cat > assets/icon.icns << EOF
# Este é um placeholder para o ícone
# Substitua por um arquivo .icns real
EOF

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
mkdir -p scripts
cat > scripts/notarize.js << EOF
// Script de notarização para Mac App Store
// Implementar conforme necessário
console.log('Notarização não implementada ainda');
EOF

# Voltar para o diretório raiz
cd ../..

echo ""
echo "🎉 Instalação concluída com sucesso!"
echo "================================================================"
echo ""
echo "🚀 Para iniciar o Ayomant:"
echo "   1. Abra o Docker Desktop"
echo "   2. Execute: npm run dev:web (para o frontend)"
echo "   3. Execute: npm run dev:api (para a API)"
echo "   4. Abra: http://localhost:3000"
echo ""
echo "🖥️  Para criar a aplicação desktop:"
echo "   cd apps/web && npm run package:mac"
echo ""
echo "📱 Para usar no Mac:"
echo "   - A aplicação será criada em apps/web/dist-electron/"
echo "   - Arraste o .dmg para a pasta Aplicações"
echo "   - Ou use o .zip para instalação manual"
echo ""
echo "🔗 Links úteis:"
echo "   - Dashboard: http://localhost:3000"
echo "   - API: http://localhost:6000"
echo "   - Documentação: https://ayomant.com/docs"
echo ""
echo "✨ Obrigado por escolher o Ayomant!"
echo "   Sistema de Manutenção Inteligente e Gestão de Ativos"

