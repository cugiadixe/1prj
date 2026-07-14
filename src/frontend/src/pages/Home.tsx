import React from 'react';
import { Typography } from 'antd';

const { Title, Paragraph } = Typography;

const Home: React.FC = () => {
  return (
    <div>
      <Title>Welcome to PTKD ERP</Title>
      <Paragraph>
        Hệ thống quản lý PTKD - INDEVCO ERP.
      </Paragraph>
    </div>
  );
};

export default Home;
