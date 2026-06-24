const { contextBridge, ipcRenderer } = require('electron');

// Preload script para o MUFUTU Desktop
// Expõe APIs seguras para o processo de renderização

// Expor versão e informações do app
contextBridge.exposeInMainWorld('mufutuNative', {
  shell: 'desktop',
  platform: process.platform,
});

contextBridge.exposeInMainWorld('connectivityBridge', {
  getStatus: () => ipcRenderer.invoke('connectivity:getStatus'),
  scanWifi: () => ipcRenderer.invoke('connectivity:scanWifi'),
  connectWifi: (ssid, password) => ipcRenderer.invoke('connectivity:connectWifi', { ssid, password }),
});

contextBridge.exposeInMainWorld('labelsBridge', {
  scanWifi: () => ipcRenderer.invoke('labels:scanWifi'),
  getLiveStatus: () => ipcRenderer.invoke('labels:getLiveStatus'),
  discoverPrinters: () => ipcRenderer.invoke('labels:discoverPrinters'),
  setPrinterHost: (host, model) => ipcRenderer.invoke('labels:setPrinterHost', host, model),
  getPrinterConfig: () => ipcRenderer.invoke('labels:getPrinterConfig'),
  connectWifi: (ssid, password) => ipcRenderer.invoke('labels:connectWifi', { ssid, password }),
  configureEthernet: (config) => ipcRenderer.invoke('labels:configureEthernet', config),
  printLabels: (items) => ipcRenderer.invoke('labels:printLabels', items),
});

contextBridge.exposeInMainWorld('electronAPI', {
  // Informações do app
  getAppVersion: () => ipcRenderer.invoke('get-app-version'),
  getAppName: () => ipcRenderer.invoke('get-app-name'),
  
  // Sistema de arquivos
  openFile: () => ipcRenderer.invoke('dialog:openFile'),
  saveFile: () => ipcRenderer.invoke('dialog:saveFile'),
  
  // Notificações
  showNotification: (title, body) => ipcRenderer.invoke('notification:show', title, body),
  
  // Configurações
  getSettings: () => ipcRenderer.invoke('settings:get'),
  setSettings: (settings) => ipcRenderer.invoke('settings:set', settings),
  
  // Banco de dados local
  getLocalData: (key) => ipcRenderer.invoke('localData:get', key),
  setLocalData: (key, value) => ipcRenderer.invoke('localData:set', key, value),
  
  // Sincronização offline
  syncOfflineData: () => ipcRenderer.invoke('sync:offline'),
  getOfflineStatus: () => ipcRenderer.invoke('sync:status'),
  
  // Impressão
  printDocument: (data) => ipcRenderer.invoke('print:document', data),
  printQRCode: (data) => ipcRenderer.invoke('print:qrcode', data),
  
  // Hardware
  getSystemInfo: () => ipcRenderer.invoke('system:info'),
  getBatteryInfo: () => ipcRenderer.invoke('system:battery'),
  getNetworkInfo: () => ipcRenderer.invoke('system:network'),
  
  // Câmera e QR Code
  scanQRCode: () => ipcRenderer.invoke('camera:scanQR'),
  takePhoto: () => ipcRenderer.invoke('camera:photo'),
  
  // Geolocalização
  getLocation: () => ipcRenderer.invoke('location:get'),
  
  // Permissões
  requestPermission: (permission) => ipcRenderer.invoke('permission:request', permission),
  checkPermission: (permission) => ipcRenderer.invoke('permission:check', permission),
  
  // Atualizações
  checkForUpdates: () => ipcRenderer.invoke('updater:check'),
  downloadUpdate: () => ipcRenderer.invoke('updater:download'),
  installUpdate: () => ipcRenderer.invoke('updater:install'),
  
  // Logs e debugging
  logMessage: (level, message) => ipcRenderer.invoke('logger:log', level, message),
  getLogs: () => ipcRenderer.invoke('logger:get'),
  clearLogs: () => ipcRenderer.invoke('logger:clear'),
  
  // Eventos do sistema
  onWorkOrderCreated: (callback) => ipcRenderer.on('work-order:created', callback),
  onAssetUpdated: (callback) => ipcRenderer.on('asset:updated', callback),
  onMaintenanceDue: (callback) => ipcRenderer.on('maintenance:due', callback),
  onSystemAlert: (callback) => ipcRenderer.on('system:alert', callback),
  
  // Utilitários
  isOnline: () => navigator.onLine,
  getPlatform: () => process.platform,
  getArchitecture: () => process.arch,
  
  // Temas e personalização
  getTheme: () => ipcRenderer.invoke('theme:get'),
  setTheme: (theme) => ipcRenderer.invoke('theme:set', theme),
  
  // Idioma
  getLanguage: () => ipcRenderer.invoke('language:get'),
  setLanguage: (lang) => ipcRenderer.invoke('language:set', lang),
  
  // Backup e restauração
  createBackup: () => ipcRenderer.invoke('backup:create'),
  restoreBackup: (backupPath) => ipcRenderer.invoke('backup:restore', backupPath),
  listBackups: () => ipcRenderer.invoke('backup:list'),
  
  // Exportação de dados
  exportData: (format, filters) => ipcRenderer.invoke('export:data', format, filters),
  importData: (filePath) => ipcRenderer.invoke('import:data', filePath),
  
  // Relatórios
  generateReport: (type, params) => ipcRenderer.invoke('report:generate', type, params),
  scheduleReport: (type, schedule) => ipcRenderer.invoke('report:schedule', type, schedule),
  
  // Integrações
  testConnection: (endpoint) => ipcRenderer.invoke('integration:test', endpoint),
  syncWithCloud: () => ipcRenderer.invoke('integration:syncCloud'),
  syncWithERP: () => ipcRenderer.invoke('integration:syncERP'),
  
  // Segurança
  changePassword: (oldPassword, newPassword) => 
    ipcRenderer.invoke('security:changePassword', oldPassword, newPassword),
  lockApp: () => ipcRenderer.invoke('security:lock'),
  unlockApp: (password) => ipcRenderer.invoke('security:unlock', password),
  
  // Limpeza
  clearCache: () => ipcRenderer.invoke('cleanup:cache'),
  clearTempFiles: () => ipcRenderer.invoke('cleanup:temp'),
  optimizeDatabase: () => ipcRenderer.invoke('cleanup:database'),
  
  // Remover listeners
  removeAllListeners: (channel) => ipcRenderer.removeAllListeners(channel),
});

// Expor utilitários do sistema
contextBridge.exposeInMainWorld('systemUtils', {
  // Formatação de dados
  formatDate: (date) => new Date(date).toLocaleDateString('pt-BR'),
  formatTime: (date) => new Date(date).toLocaleTimeString('pt-BR'),
  formatCurrency: (value) => new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL'
  }).format(value),
  formatNumber: (value) => new Intl.NumberFormat('pt-BR').format(value),
  
  // Validação
  isValidEmail: (email) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email),
  isValidPhone: (phone) => /^[\+]?[1-9][\d]{0,15}$/.test(phone),
  isValidCPF: (cpf) => {
    cpf = cpf.replace(/[^\d]/g, '');
    if (cpf.length !== 11) return false;
    if (/^(\d)\1{10}$/.test(cpf)) return false;
    
    let sum = 0;
    for (let i = 0; i < 9; i++) {
      sum += parseInt(cpf.charAt(i)) * (10 - i);
    }
    let remainder = sum % 11;
    let digit1 = remainder < 2 ? 0 : 11 - remainder;
    
    sum = 0;
    for (let i = 0; i < 10; i++) {
      sum += parseInt(cpf.charAt(i)) * (11 - i);
    }
    remainder = sum % 11;
    let digit2 = remainder < 2 ? 0 : 11 - remainder;
    
    return parseInt(cpf.charAt(9)) === digit1 && parseInt(cpf.charAt(10)) === digit2;
  },
  
  // Criptografia básica
  hashString: (str) => {
    let hash = 0;
    if (str.length === 0) return hash;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    return hash.toString();
  },
  
  // Geração de IDs
  generateId: () => {
    return 'id_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
  },
  
  // Debounce e throttle
  debounce: (func, wait) => {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  },
  
  throttle: (func, limit) => {
    let inThrottle;
    return function() {
      const args = arguments;
      const context = this;
      if (!inThrottle) {
        func.apply(context, args);
        inThrottle = true;
        setTimeout(() => inThrottle = false, limit);
      }
    };
  },
  
  // Local Storage seguro
  secureStorage: {
    set: (key, value) => {
      try {
        const encrypted = btoa(JSON.stringify(value));
        localStorage.setItem(key, encrypted);
        return true;
      } catch (error) {
        console.error('Erro ao salvar no storage:', error);
        return false;
      }
    },
    
    get: (key) => {
      try {
        const encrypted = localStorage.getItem(key);
        if (!encrypted) return null;
        return JSON.parse(atob(encrypted));
      } catch (error) {
        console.error('Erro ao ler do storage:', error);
        return null;
      }
    },
    
    remove: (key) => {
      try {
        localStorage.removeItem(key);
        return true;
      } catch (error) {
        console.error('Erro ao remover do storage:', error);
        return false;
      }
    },
    
    clear: () => {
      try {
        localStorage.clear();
        return true;
      } catch (error) {
        console.error('Erro ao limpar storage:', error);
        return false;
      }
    }
  }
});

// Expor constantes do sistema
contextBridge.exposeInMainWorld('constants', {
  APP_NAME: 'MUFUTU',
  APP_VERSION: '1.0.0',
  COMPANY_NAME: 'MUFUTU',
  SUPPORT_EMAIL: 'support@mufutu.ao',
  WEBSITE_URL: 'https://mufutu.ao',
  API_BASE_URL: process.env.NODE_ENV === 'development' 
    ? 'http://localhost:6000/api' 
    : 'https://api.mufutu.ao',
  
  // Status de manutenção
  MAINTENANCE_STATUS: {
    PENDING: 'pending',
    IN_PROGRESS: 'in_progress',
    COMPLETED: 'completed',
    CANCELLED: 'cancelled',
    ON_HOLD: 'on_hold'
  },
  
  // Prioridades
  PRIORITY_LEVELS: {
    LOW: 'low',
    MEDIUM: 'medium',
    HIGH: 'high',
    CRITICAL: 'critical'
  },
  
  // Tipos de manutenção
  MAINTENANCE_TYPES: {
    PREVENTIVE: 'preventive',
    CORRECTIVE: 'corrective',
    PREDICTIVE: 'predictive',
    EMERGENCY: 'emergency',
    INSPECTION: 'inspection'
  },
  
  // Status de ativos
  ASSET_STATUS: {
    OPERATIONAL: 'operational',
    MAINTENANCE: 'maintenance',
    OUT_OF_SERVICE: 'out_of_service',
    RETIRED: 'retired',
    SPARE: 'spare'
  },
  
  // Limites do sistema
  LIMITS: {
    MAX_FILE_SIZE: 10 * 1024 * 1024, // 10MB
    MAX_ATTACHMENTS: 5,
    MAX_COMMENT_LENGTH: 1000,
    SESSION_TIMEOUT: 30 * 60 * 1000, // 30 minutos
    OFFLINE_SYNC_INTERVAL: 5 * 60 * 1000, // 5 minutos
  }
});

console.log('🚀 Preload script carregado com sucesso!');
console.log('MUFUTU Desktop — Gestão de Activos Mineiros');

