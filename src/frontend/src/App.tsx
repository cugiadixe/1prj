import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import { Layout, Menu } from 'antd';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import Home from './pages/Home';
import SystemHealth from './pages/SystemHealth';

const { Header, Content, Footer } = Layout;

const queryClient = new QueryClient();

const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Layout className="layout" style={{ minHeight: '100vh' }}>
          <Header>
            <div className="logo" />
            <Menu theme="dark" mode="horizontal" defaultSelectedKeys={['1']}>
              <Menu.Item key="1"><Link to="/">Home</Link></Menu.Item>
              <Menu.Item key="2"><Link to="/system-health">System Health</Link></Menu.Item>
            </Menu>
          </Header>
          <Content style={{ padding: '0 50px', marginTop: '20px' }}>
            <div className="site-layout-content" style={{ padding: 24, minHeight: 380, background: '#fff' }}>
              <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/system-health" element={<SystemHealth />} />
                <Route path="*" element={<div><h1>404 - Not Found</h1></div>} />
              </Routes>
            </div>
          </Content>
          <Footer style={{ textAlign: 'center' }}>PTKD ERP ©2026</Footer>
        </Layout>
      </Router>
    </QueryClientProvider>
  );
};

export default App;
