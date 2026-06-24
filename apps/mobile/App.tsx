import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createDrawerNavigator } from '@react-navigation/drawer';
import { Provider as PaperProvider } from 'react-native-paper';
import { StatusBar } from 'react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Screens
import { LoginScreen } from './src/screens/auth/LoginScreen';
import { DashboardScreen } from './src/screens/dashboard/DashboardScreen';
import { WorkOrdersScreen } from './src/screens/work-orders/WorkOrdersScreen';
import { AssetsScreen } from './src/screens/assets/AssetsScreen';
import { MaintenanceScreen } from './src/screens/maintenance/MaintenanceScreen';
import { InventoryScreen } from './src/screens/inventory/InventoryScreen';
import { ProfileScreen } from './src/screens/profile/ProfileScreen';
import { SettingsScreen } from './src/screens/settings/SettingsScreen';
import { QRScannerScreen } from './src/screens/qr-scanner/QRScannerScreen';
import { WorkOrderDetailScreen } from './src/screens/work-orders/WorkOrderDetailScreen';
import { AssetDetailScreen } from './src/screens/assets/AssetDetailScreen';

// Components
import { CustomDrawerContent } from './src/components/navigation/CustomDrawerContent';
import { TabBarIcon } from './src/components/navigation/TabBarIcon';

// Context
import { AuthProvider, useAuth } from './src/contexts/AuthContext';
import { ThemeProvider } from './src/contexts/ThemeContext';

// Types
import { RootStackParamList, MainTabParamList, DrawerParamList } from './src/types/navigation';

const Stack = createStackNavigator<RootStackParamList>();
const Tab = createBottomTabNavigator<MainTabParamList>();
const Drawer = createDrawerNavigator<DrawerParamList>();

const queryClient = new QueryClient();

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => (
          <TabBarIcon route={route} focused={focused} color={color} size={size} />
        ),
        tabBarActiveTintColor: '#3b82f6',
        tabBarInactiveTintColor: 'gray',
        headerShown: false,
      })}
    >
      <Tab.Screen 
        name="Dashboard" 
        component={DashboardScreen}
        options={{ title: 'Dashboard' }}
      />
      <Tab.Screen 
        name="WorkOrders" 
        component={WorkOrdersScreen}
        options={{ title: 'Work Orders' }}
      />
      <Tab.Screen 
        name="Assets" 
        component={AssetsScreen}
        options={{ title: 'Assets' }}
      />
      <Tab.Screen 
        name="Maintenance" 
        component={MaintenanceScreen}
        options={{ title: 'Maintenance' }}
      />
      <Tab.Screen 
        name="Inventory" 
        component={InventoryScreen}
        options={{ title: 'Inventory' }}
      />
    </Tab.Navigator>
  );
}

function DrawerNavigator() {
  return (
    <Drawer.Navigator
      drawerContent={(props) => <CustomDrawerContent {...props} />}
      screenOptions={{
        headerShown: false,
        drawerActiveTintColor: '#3b82f6',
        drawerInactiveTintColor: 'gray',
      }}
    >
      <Drawer.Screen name="MainTabs" component={MainTabs} />
      <Drawer.Screen name="Profile" component={ProfileScreen} />
      <Drawer.Screen name="Settings" component={SettingsScreen} />
    </Drawer.Navigator>
  );
}

function Navigation() {
  const { isAuthenticated } = useAuth();

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {!isAuthenticated ? (
          <Stack.Screen name="Login" component={LoginScreen} />
        ) : (
          <>
            <Stack.Screen name="DrawerNavigator" component={DrawerNavigator} />
            <Stack.Screen name="QRScanner" component={QRScannerScreen} />
            <Stack.Screen name="WorkOrderDetail" component={WorkOrderDetailScreen} />
            <Stack.Screen name="AssetDetail" component={AssetDetailScreen} />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <PaperProvider>
          <AuthProvider>
            <StatusBar barStyle="dark-content" backgroundColor="#ffffff" />
            <Navigation />
          </AuthProvider>
        </PaperProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

