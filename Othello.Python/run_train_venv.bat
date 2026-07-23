@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "VENV_PYTHON=C:\venv\othelloai\Scripts\python.exe"
set "DEFAULT_KIFU_DIR=%SCRIPT_DIR%..\Othello.Console\bin\Debug\net10.0\data\selfplay"
set "DEFAULT_ONNX=%SCRIPT_DIR%models\policy_value_best.onnx"
set "DEFAULT_DEVICE=auto"

if not exist "%VENV_PYTHON%" (
  echo [ERROR] venv python not found: %VENV_PYTHON%
  echo create it first: python -m venv C:\venv\othelloai
  exit /b 1
)

set "DEVICE=%DEFAULT_DEVICE%"
if /I "%~1"=="--cuda" (
  set "DEVICE=cuda"
  shift
)

set "ARGS=%*"

if "%~1"=="" (
  echo [INFO] no arguments detected. use default options.
  "%VENV_PYTHON%" "%SCRIPT_DIR%train_policy_value_onnx.py" --kifu "%DEFAULT_KIFU_DIR%" --onnx "%DEFAULT_ONNX%" --epochs 16 --batch-size 256 --device %DEVICE%
  exit /b %errorlevel%
)

if /I "%~1"=="--cuda" (
  shift
  set "ARGS=%*"
)

if /I "%ARGS:~0,6%"=="--kifu" (
  "%VENV_PYTHON%" "%SCRIPT_DIR%train_policy_value_onnx.py" --device %DEVICE% %ARGS%
) else (
  "%VENV_PYTHON%" "%SCRIPT_DIR%train_policy_value_onnx.py" --kifu "%DEFAULT_KIFU_DIR%" --onnx "%DEFAULT_ONNX%" --device %DEVICE% %ARGS%
)
exit /b %errorlevel%
