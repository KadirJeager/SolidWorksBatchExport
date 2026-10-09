@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 exit /b 1
dotnet publish SolidWorksBatchExport.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
set "publish_result=%errorlevel%"
popd
exit /b %publish_result%
