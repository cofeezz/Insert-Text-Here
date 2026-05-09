@echo off
:: ProcessSuspender — Complet build 
:: Requirement: .NET 8 SDK (https://dotnet.microsoft.com/download)

title ProcessSuspender - Build
color 0B

echo.
echo  =====================================================
echo   ProcessSuspender - Build
echo  =====================================================
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  [ERRO] .NET SDK nao encontrado!
    echo  Instale em: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set VER=%%v
echo  [OK] .NET SDK %VER%
echo.

echo  [1/2] Compilando ProcessSuspender (App)...
dotnet publish App\ProcessSuspender.csproj ^
    -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "dist\app" --nologo -v q

if errorlevel 1 ( echo  [ERRO] Falha ao compilar App. & pause & exit /b 1 )
echo  [OK] App compilado.
echo.

echo  [2/2] Compilando Launcher...
dotnet publish Launcher\Launcher.csproj ^
    -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "dist\launcher" --nologo -v q

if errorlevel 1 ( echo  [ERRO] Falha ao compilar Launcher. & pause & exit /b 1 )
echo  [OK] Launcher compilado.
echo.

echo  =====================================================
echo   Arquivos gerados:
echo  =====================================================
echo.
echo   dist\launcher\Launcher.exe
echo     -> Sobe para o GitHub Releases como "Launcher.exe"
echo     -> E o arquivo que o usuario baixa UMA VEZ
echo.
echo   dist\app\ProcessSuspender.exe
echo     -> Sobe para o GitHub Releases como "ProcessSuspender.exe"
echo     -> Baixado e atualizado automaticamente pelo Launcher
echo.
pause
