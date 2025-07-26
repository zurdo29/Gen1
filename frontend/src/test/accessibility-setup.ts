import React from 'react';
import ReactDOM from 'react-dom';
import configureAxe from '@axe-core/react';

// Configure axe for accessibility testing
if (process.env.NODE_ENV !== 'production') {
  configureAxe(React, ReactDOM, 1000, {
    rules: [
      {
        id: 'color-contrast',
        enabled: true
      },
      {
        id: 'keyboard-navigation',
        enabled: true
      },
      {
        id: 'focus-management',
        enabled: true
      }
    ]
  });
}