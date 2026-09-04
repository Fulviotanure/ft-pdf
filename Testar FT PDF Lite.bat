@echo off
title FT PDF Lite - Teste dos Arquivos Brutos (.NET 10)
set "PATH=C:\Users\Fulvio\AppData\Local\Microsoft\dotnet;%PATH%"
cd /d "c:\Users\Fulvio\Documentos\antigravity\FT PDF\ft-pdf-1\ft-pdf-lite"
echo ================================================================
echo       FT PDF Lite (Edicao Ultraleve) - Teste Local dos Arquivos Brutos
echo ================================================================
echo.
dotnet run
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Ocorreu uma interrupcao na execucao.
    pause
)
