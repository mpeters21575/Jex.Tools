@echo off
REM Build script for JEX Tools CLI

echo Building JEX Tools CLI...

REM Clean previous builds
dotnet clean

REM Build the solution
dotnet build --configuration Release

REM Create the modules directory
if not exist ".\Jex.Tools.CLI\bin\Release\net9.0\modules" mkdir ".\Jex.Tools.CLI\bin\Release\net9.0\modules"

REM Copy module assemblies to the modules directory
copy ".\Jex.Tools.OpenPullrequests\bin\Release\net9.0\*.dll" ".\Jex.Tools.CLI\bin\Release\net9.0\modules\"

echo.
echo Build complete!
echo.
echo To run the CLI:
echo   dotnet .\Jex.Tools.CLI\bin\Release\net9.0\jex-tools.dll
echo.
echo Or install as global tool:
echo   dotnet pack Jex.Tools.CLI --configuration Release
echo   dotnet tool install --global --add-source .\Jex.Tools.CLI\nupkg jex-tools