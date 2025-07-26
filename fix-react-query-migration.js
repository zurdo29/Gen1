#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

// React Query v5 migration fixes
const fixes = [
  // cacheTime → gcTime
  {
    pattern: /cacheTime:/g,
    replacement: 'gcTime:'
  },
  // status === 'loading' → status === 'pending'
  {
    pattern: /status === ['"]loading['"]/g,
    replacement: "status === 'pending'"
  },
  // isLoading → isPending (but be careful with new isLoading)
  {
    pattern: /\.isLoading(?!\s*&&)/g,
    replacement: '.isPending'
  },
  // isInitialLoading → isLoading
  {
    pattern: /\.isInitialLoading/g,
    replacement: '.isLoading'
  },
  // Jest → Vitest imports
  {
    pattern: /import.*jest/g,
    replacement: 'import { vi } from "vitest"'
  },
  // jest. → vi.
  {
    pattern: /jest\./g,
    replacement: 'vi.'
  },
  // jest.Mock → vi.Mock
  {
    pattern: /jest\.Mock/g,
    replacement: 'vi.Mock'
  },
  // fireEvent import fix
  {
    pattern: /import { render } from ['"]@testing-library\/react['"];/g,
    replacement: 'import { render, fireEvent } from "@testing-library/react";'
  }
];

function applyFixesToFile(filePath) {
  try {
    let content = fs.readFileSync(filePath, 'utf8');
    let modified = false;
    
    fixes.forEach(fix => {
      const newContent = content.replace(fix.pattern, fix.replacement);
      if (newContent !== content) {
        content = newContent;
        modified = true;
      }
    });
    
    if (modified) {
      fs.writeFileSync(filePath, content);
      console.log(`Fixed: ${filePath}`);
    }
  } catch (error) {
    console.error(`Error processing ${filePath}:`, error.message);
  }
}

function processDirectory(dir) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory() && !file.includes('node_modules')) {
      processDirectory(filePath);
    } else if (file.endsWith('.ts') || file.endsWith('.tsx')) {
      applyFixesToFile(filePath);
    }
  });
}

// Process frontend src directory
const frontendSrc = path.join(__dirname, 'frontend', 'src');
if (fs.existsSync(frontendSrc)) {
  console.log('Applying React Query v5 migration fixes...');
  processDirectory(frontendSrc);
  console.log('Migration fixes applied!');
} else {
  console.error('Frontend src directory not found');
}