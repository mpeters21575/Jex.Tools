#!/bin/bash
# Build script for JEX Tools CLI

echo "Building JEX Tools CLI..."

# Clean previous builds
dotnet clean

# Build the solution
dotnet build --configuration Release

# Create the modules directory
mkdir -p ./Jex.Tools.CLI/bin/Release/net9.0/modules

# Copy module assemblies to the modules directory
cp ./Jex.Tools.OpenPullrequests/bin/Release/net9.0/*.dll ./Jex.Tools.CLI/bin/Release/net9.0/modules/

echo "Build complete!"
echo ""
echo "To run the CLI:"
echo "  dotnet ./Jex.Tools.CLI/bin/Release/net9.0/jex-tools.dll"
echo ""
echo "Or install as global tool:"
echo "  dotnet pack Jex.Tools.CLI --configuration Release"
echo "  dotnet tool install --global --add-source ./Jex.Tools.CLI/nupkg jex-tools"