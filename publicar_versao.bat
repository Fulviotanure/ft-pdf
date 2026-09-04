@echo off
chcp 65001 > nul
node scripts/publish-version.js
pause
