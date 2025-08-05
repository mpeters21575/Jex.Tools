# JEX Tools CLI

A powerful, modular command-line interface for developer productivity tools. Built with .NET 9, JEX Tools provides an extensible framework for creating and managing CLI modules.

## 🚀 Features

- **Modular Architecture**: Easily add and manage independent tool modules
- **Auto-Discovery**: Automatically detects and loads available modules
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Azure DevOps Integration**: Built-in pull request scanner for Azure DevOps
- **Extensible**: Simple interface for creating custom modules
- **Modern .NET**: Built with .NET 9 and C# 12 features

## 📦 Installation

### Prerequisites

- .NET 9 SDK or later ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))

### Global Tool Installation

The easiest way to install JEX Tools is as a global .NET tool.

#### Windows

```powershell
# Install from NuGet (when published)
dotnet tool install --global jex-tools

# Or install from local package
cd path\to\Jex.Tools.CLI
dotnet pack -c Release
dotnet tool install --global --add-source .\nupkg jex-tools
```

#### macOS / Linux

```bash
# Install from NuGet (when published)
dotnet tool install --global jex-tools

# Or install from local package
cd path/to/Jex.Tools.CLI
dotnet pack -c Release
dotnet tool install --global --add-source ./nupkg jex-tools
```

### Alternative Installation Methods

#### 1. Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/jex-tools.git
cd jex-tools

# Build the solution
dotnet build -c Release

# Run directly
dotnet run --project Jex.Tools.CLI -- help
```

#### 2. Create Platform-Specific Executable

##### Windows
```powershell
dotnet publish -c Release -r win-x64 --self-contained
# Executable will be in: bin\Release\net9.0\win-x64\publish\jex-tools.exe
```

##### macOS (Intel)
```bash
dotnet publish -c Release -r osx-x64 --self-contained
# Executable will be in: bin/Release/net9.0/osx-x64/publish/jex-tools
```

##### macOS (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained
# Executable will be in: bin/Release/net9.0/osx-arm64/publish/jex-tools
```

##### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained
# Executable will be in: bin/Release/net9.0/linux-x64/publish/jex-tools
```

## 🔧 Usage

Once installed, you can use `jex-tools` from anywhere in your terminal.

### Basic Commands

```bash
# Show available commands
jex-tools

# Get help
jex-tools help

# Get help for a specific command
jex-tools <command> --help
```

### Available Modules

#### Azure DevOps Pull Request Scanner

Scans all projects in your Azure DevOps organization for open pull requests.

```bash
# Basic usage (reads from appsettings.json)
jex-tools pr-scan

# Specify organization and PAT
jex-tools pr-scan --org https://dev.azure.com/yourorg --pat your-token

# Show all pull requests (not just yours)
jex-tools pr-scan --all-prs

# Get help
jex-tools pr-scan --help
```

#### System Information

Displays system and environment information.

```bash
# Show system info
jex-tools sysinfo

# Include environment variables
jex-tools sysinfo --env

# Get help
jex-tools sysinfo --help
```

## ⚙️ Configuration

### Azure DevOps Pull Request Scanner

Create an `appsettings.json` file in your working directory:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-personal-access-token",
    "ShowOnlyMyPullRequests": true
  }
}
```

Or use environment variables:

```bash
# Windows
set AZDEVOPS_ORG_URL=https://dev.azure.com/yourorg
set AZDEVOPS_PAT=your-token
set AZDEVOPS_SHOW_ONLY_MY_PRS=false

# macOS/Linux
export AZDEVOPS_ORG_URL="https://dev.azure.com/yourorg"
export AZDEVOPS_PAT="your-token"
export AZDEVOPS_SHOW_ONLY_MY_PRS="false"
```

### Creating a Personal Access Token (PAT)

1. Sign in to Azure DevOps
2. Click on User settings → Personal access tokens
3. Click "New Token"
4. Set the following permissions:
    - Code (Read)
    - Identity (Read)
    - Project and Team (Read)
5. Copy the generated token

## 🔌 Creating Custom Modules

Creating a new module is simple:

1. Create a new class library project
2. Reference `Jex.Tools.CLI.Core`
3. Implement the `ICliModule` interface

```csharp
using Jex.Tools.CLI.Core;

public class MyCustomModule : ICliModule
{
    public string Name => "My Custom Module";
    public string Description => "Does something amazing";
    public string Command => "my-module";
    public string Version => "1.0.0";
    
    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Contains("--help"))
        {
            ShowHelp();
            return 0;
        }
        
        Console.WriteLine("Hello from my custom module!");
        return 0;
    }
    
    public void ShowHelp()
    {
        Console.WriteLine($"{Name} v{Version}");
        Console.WriteLine("Usage: jex-tools my-module [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --help    Show this help message");
    }
}
```

4. Build your module
5. Copy the DLL to the `modules` directory in the JEX Tools installation folder
6. The module will be automatically discovered on next run

## 🛠️ Development

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/jex-tools.git
   cd jex-tools
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run tests:
   ```bash
   dotnet test
   ```

4. Create a package:
   ```bash
   dotnet pack -c Release
   ```

### Project Structure

```
jex-tools/
├── Jex.Tools.CLI.Core/          # Core interfaces and module loader
├── Jex.Tools.CLI/               # Main CLI application
├── Jex.Tools.OpenPullrequests/  # Azure DevOps PR scanner module
├── modules/                     # External modules directory
└── docs/                        # Documentation
```

## 🐛 Troubleshooting

### Common Issues

#### "Command not found" after installation
- Ensure .NET tools are in your PATH
- Try restarting your terminal
- Run `dotnet tool list --global` to verify installation

#### Module not loading
- Check the module is in the correct directory
- Verify the DLL implements `ICliModule`
- Check for missing dependencies

#### Configuration not found
- Ensure `appsettings.json` is in the current directory
- Check JSON syntax is valid
- Verify environment variables are set correctly

### Debug Mode

Run with verbose output:
```bash
set DOTNET_CLI_CONTEXT_VERBOSE=true
jex-tools <command>
```

## 📝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/jex-tools/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/jex-tools/discussions)
- **Email**: support@jextools.com

## 🎯 Roadmap

- [ ] Additional Azure DevOps modules (work items, builds, releases)
- [ ] GitHub integration modules
- [ ] AWS CLI wrapper modules
- [ ] Docker management modules
- [ ] Package manager integration
- [ ] Interactive mode
- [ ] Configuration profiles
- [ ] Plugin marketplace

## 🙏 Acknowledgments

- Built with [.NET 9](https://dotnet.microsoft.com/)
- Uses [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)
- Azure DevOps integration via [Azure DevOps .NET SDK](https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/)

---

Made with ❤️ by the JEX Tools team
