@echo off
chcp 65001 >nul
echo ==========================================
echo   SDGVM - Система Динамической Генерации
echo   Виртуальных Миров
echo ==========================================
echo.
echo Проверка ComfyUI сервера...

REM Проверяем, запущен ли уже сервер
curl -s http://127.0.0.1:8188/system_stats >nul 2>&1
if %ERRORLEVEL%==0 (
    echo ComfyUI уже запущен!
) else (
    echo Запуск ComfyUI (CPU режим)...
    start "" /B "%~dp0Assets\ComfyUI\run_cpu.bat"
    echo Ожидание запуска сервера (30 сек)...
    timeout /t 30 /nobreak >nul
)

echo.
echo Запуск игры...
start "" "%~dp0SDGVM.exe"
echo.
echo Игра запущена! Это окно можно закрыть.
timeout /t 5
