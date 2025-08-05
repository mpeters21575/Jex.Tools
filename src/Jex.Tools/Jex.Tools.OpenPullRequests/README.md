# Azure DevOps Pull Request Scanner

A .NET 9 console application that recursively scans all projects in your Azure DevOps organization and displays all open pull requests.

## Features

- 🔍 Automatically discovers all projects in your organization
- 📁 Recursively scans all repositories in each project
- 📋 Lists all open pull requests with detailed information
- 👤 Shows pull request author, dates, branches, and reviewers
- 🎨 Clean console output with organized project/repository grouping
- ⚡ Fast and efficient with parallel processing
- 🛡️ Graceful error handling - skips unavailable resources without crashing
- ⚙️ Configuration via appsettings.json with environment variable overrides

## Prerequisites

- .NET 9 SDK
- Azure DevOps account with access to the organization
- Personal Access Token (PAT) with appropriate permissions

## Installation

1. Clone the repository:
```bash
git clone https://github.com/your-repo/azuredevops-pr-scanner.git
cd azuredevops-pr-scanner
```

2. Build the project:
```bash
dotnet build
```

## Configuration

### Quick Start

1. **Copy the example configuration file**:
```bash
cp appsettings.example.json appsettings.json
```

2. **Edit appsettings.json** with your values:
```json
{
   "AzureDevOps": {
      "OrganizationUrl": "https://dev.azure.com/your-organization",
      "PersonalAccessToken": "your-personal-access-token-here",
      "ShowOnlyMyPullRequests": true  // Set to false to see all pull requests
   }
}
```

3. **Run the application**:
```bash
dotnet run
```

### Creating a Personal Access Token (PAT)

1. Sign in to Azure DevOps (https://dev.azure.com/your-organization)
2. Click on User settings icon → Personal access tokens
3. Click "New Token"
4. Configure the token:
   - **Name**: Give it a descriptive name (e.g., "PR Scanner")
   - **Organization**: Select your organization
   - **Expiration**: Set an appropriate expiration date
   - **Scopes**: Select the following permissions:
      - Code (Read)
      - Identity (Read)
      - Project and Team (Read)
5. Click "Create" and copy the generated token immediately

### Configuration Options

The application reads configuration from `appsettings.json` by default. You can optionally override these values using environment variables.

#### Configuration Settings

- **OrganizationUrl**: Your Azure DevOps organization URL (e.g., `https://dev.azure.com/your-organization`)
- **PersonalAccessToken**: Your Azure DevOps PAT with appropriate permissions
- **ShowOnlyMyPullRequests**:
   - `true` (default): Shows only pull requests created by the authenticated user
   - `false`: Shows all open pull requests across the organization

#### Primary Configuration (appsettings.json)

Create an `appsettings.json` file in the project root:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-personal-access-token",
    "ShowOnlyMyPullRequests": true
  }
}
```

**IMPORTANT**: Never commit `appsettings.json` with real tokens to source control. The `.gitignore` file is configured to exclude it.

#### Environment Variable Overrides (Optional)

You can override the appsettings.json values using environment variables:

##### Windows (PowerShell)
```powershell
# Override for current session
$env:AZDEVOPS_ORG_URL = "https://dev.azure.com/different-organization"
$env:AZDEVOPS_PAT = "different-personal-access-token"
$env:AZDEVOPS_SHOW_ONLY_MY_PRS = "false"  # Show all PRs, not just yours

# Run the application
dotnet run
```

##### macOS/Linux (Terminal)
```bash
# Override for current session
export AZDEVOPS_ORG_URL="https://dev.azure.com/different-organization"
export AZDEVOPS_PAT="different-personal-access-token"
export AZDEVOPS_SHOW_ONLY_MY_PRS="false"  # Show all PRs, not just yours

# Run the application
dotnet run
```

### Configuration Priority

The application loads configuration in this order (later sources override earlier ones):
1. appsettings.json (required)
2. Environment variables (optional)

## Usage

After configuration, simply run:

```bash
dotnet run
```

The application will:
1. Load configuration from appsettings.json
2. Apply any environment variable overrides
3. Connect to your Azure DevOps organization
4. Scan all projects and repositories
5. Display all open pull requests

## Security Best Practices

1. **Never commit sensitive data**:
   - The `.gitignore` file excludes `appsettings.json`
   - Only commit `appsettings.example.json` with dummy values

2. **Use secure token storage**:
   - For development: Use appsettings.json (excluded from git)
   - For production: Use environment variables or Azure Key Vault

3. **Minimal permissions**:
   - Only grant necessary PAT permissions
   - Use short expiration periods

4. **Rotate tokens regularly**:
   - Set calendar reminders for token expiration
   - Update tokens before they expire

## Platform Support

### Windows
- Windows 10/11
- Windows Server 2019+
- Both x64 and ARM64

### macOS
- macOS 10.14+ (Mojave and later)
- Native support for Apple Silicon (M1/M2/M3)
- Intel processors supported

### Linux
- Ubuntu 20.04+
- Debian 10+
- RHEL 8+
- Other distributions with .NET 9 support

## Output Example

```
Azure DevOps Pull Request Scanner
Mode: My Pull Requests Only

Fetching all projects in the organization...

Found 5 projects

Scanning project: CompanyName.Core
Scanning project: CompanyName.Web
Scanning project: CompanyName.Mobile

==== PROJECT: CompanyName.Core ====

Repository: CompanyName.Core.Api
--------------------------------------------------
PR #123: Add user authentication feature
   Created: 2024-01-15 14:30
   Author: John Doe
   Source: feature/authentication
   Target: main
   URL: https://dev.azure.com/company/Core/_git/Api/pullrequest/123
   Reviewers: Jane Smith (Approved), Bob Johnson (Waiting for author)

==================================================
Organization: https://dev.azure.com/company
Filter: My Pull Requests Only
Total projects scanned: 5
Projects with open PRs: 2
Total open pull requests: 4
```

## Project Structure

```
Jex.Tools.OpenPullrequests/
├── Configuration/
│   ├── AzureDevOpsConfiguration.cs
│   ├── ConfigurationLoader.cs
│   └── ConfigurationValidator.cs
├── Display/
│   ├── IDisplayService.cs
│   └── ConsoleDisplayService.cs
├── Models/
│   └── ScanResult.cs
├── Services/
│   ├── IProjectService.cs
│   ├── IRepositoryService.cs
│   ├── IPullRequestService.cs
│   ├── OrganizationScanner.cs
│   ├── ProjectService.cs
│   ├── PullRequestService.cs
│   └── RepositoryService.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Program.cs
├── appsettings.example.json
├── appsettings.json (created by user, git-ignored)
├── Jex.Tools.OpenPullrequests.csproj
├── .gitignore
└── README.md
```

## Architecture

The application follows SOLID principles with clean architecture:

- **Services**: Separate services for projects, repositories, and pull requests
- **Display**: Abstracted display logic with interface-based design
- **Configuration**: Centralized configuration management
- **Dependency Injection**: Full DI support with Microsoft.Extensions.DependencyInjection

### Key Components

- **OrganizationScanner**: Orchestrates the scanning process
- **ProjectService**: Handles project discovery
- **RepositoryService**: Manages repository operations
- **PullRequestService**: Fetches and filters pull requests
- **ConsoleDisplayService**: Manages console output
- **Configuration**: Type-safe configuration with validation

## Troubleshooting

### Configuration Issues

1. **"Please set the Azure DevOps organization URL" error**:
   - Ensure `appsettings.json` exists in the project directory
   - Verify the JSON is valid (no syntax errors)
   - Check that `OrganizationUrl` is set correctly

2. **"File not found" error**:
   - Make sure you're running from the project directory
   - Ensure `appsettings.json` is copied to the output directory

3. **Invalid JSON error**:
   - Validate your JSON syntax (use a JSON validator)
   - Ensure all quotes are properly closed
   - Check for trailing commas

### Authentication Issues

1. **401 Unauthorized**:
   - Verify your PAT is still valid
   - Check PAT has correct permissions
   - Ensure organization URL matches the PAT's organization

2. **404 Not Found**:
   - Verify the organization URL is correct
   - Check you have access to the organization

### Pull Request Filtering

1. **Not seeing expected pull requests**:
   - Check `ShowOnlyMyPullRequests` setting
   - If `true`, only your PRs will be shown
   - Set to `false` to see all PRs in the organization

2. **Too many pull requests**:
   - Set `ShowOnlyMyPullRequests` to `true` to filter

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.