#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

function fixNotificationCalls(filePath) {
    try {
        let content = fs.readFileSync(filePath, 'utf8');
        let modified = false;

        // Fix addNotification calls from object format to parameter format
        const notificationPattern = /addNotification\(\{\s*type:\s*['"](\w+)['"],\s*title:\s*['"]([^'"]+)['"],\s*message:\s*([^}]+)\s*\}\);?/gs;

        const newContent = content.replace(notificationPattern, (match, type, title, message) => {
            // Clean up the message part - remove quotes if it's a string literal
            let cleanMessage = message.trim();
            if (cleanMessage.startsWith("'") && cleanMessage.endsWith("'")) {
                cleanMessage = cleanMessage.slice(1, -1);
            } else if (cleanMessage.startsWith('"') && cleanMessage.endsWith('"')) {
                cleanMessage = cleanMessage.slice(1, -1);
            }

            // If it's a template literal or variable, keep as is
            if (cleanMessage.includes('`') || cleanMessage.includes('${') || !cleanMessage.includes("'") && !cleanMessage.includes('"')) {
                return `addNotification('${type}', '${title}', ${message.trim()});`;
            } else {
                return `addNotification('${type}', '${title}', '${cleanMessage}');`;
            }
        });

        if (newContent !== content) {
            fs.writeFileSync(filePath, newContent);
            console.log(`Fixed notification calls in: ${filePath}`);
            modified = true;
        }

        return modified;
    } catch (error) {
        console.error(`Error processing ${filePath}:`, error.message);
        return false;
    }
}

// Fix the useExport.ts file
const useExportPath = path.join(__dirname, 'frontend', 'src', 'hooks', 'useExport.ts');
if (fs.existsSync(useExportPath)) {
    fixNotificationCalls(useExportPath);
} else {
    console.error('useExport.ts not found');
}