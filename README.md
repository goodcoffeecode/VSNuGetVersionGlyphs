# VSNuGetVersionGlyphs

A Visual Studio extension (VSIX) that displays inline glyphs in .csproj files showing the version status of NuGet packages with interactive version switching.

## Features

- **Visual Indicators**: Displays glyphs at the end of each `<PackageReference>` line in .csproj files
  - 🟢 **Green checkmark**: Package is up-to-date with the latest version
  - 🔵 **Blue "N" badge**: A newer version of the package is available
  
- **Interactive Version Popup**: Click on any glyph to see a popup showing:
  - Up to 5 versions above the current version
  - Up to 5 versions below the current version
  - The current version is highlighted
  
- **Quick Version Switching**: Click any version in the popup to instantly update the package to that version

## Requirements

- Visual Studio 2022 (version 17.0 or later)
- .NET Framework 4.7.2 or later

## Installation

1. Download the latest `.vsix` file from the releases page
2. Double-click the `.vsix` file to install
3. Restart Visual Studio

## Usage

1. Open any `.csproj` file in Visual Studio
2. Look for the glyphs at the end of `<PackageReference>` lines
3. Hover over a glyph to see version information in a tooltip
4. Click on a glyph to open the version selection popup
5. Click on a version in the popup to update the package

## Example

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" /> 🔵
    <PackageReference Include="AutoMapper" Version="13.0.1" /> 🟢
  </ItemGroup>
</Project>
```

## How It Works

The extension:
1. Monitors `.csproj` files opened in Visual Studio
2. Parses `<PackageReference>` elements to extract package IDs and versions
3. Queries nuget.org API to fetch available versions
4. Displays visual indicators based on version comparison
5. Provides an interactive UI for version selection and updates

## Building from Source

### Prerequisites

- Visual Studio 2022 with VSIX development workload
- .NET Framework 4.7.2 SDK

### Build Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/goodcoffeecode/VSNuGetVersionGlyphs.git
   ```

2. Open `NuGetVersionGlyphs.sln` in Visual Studio

3. Restore NuGet packages

4. Build the solution (F6)

5. The `.vsix` file will be created in the `bin\Debug` or `bin\Release` folder

## Technical Details

### Architecture

- **NuGetService**: Communicates with nuget.org API using NuGet.Protocol
- **CsprojParser**: Extracts PackageReference information using regex
- **NuGetVersionAdornment**: Creates and manages visual glyphs in the text editor
- **VersionPopup**: WPF-based popup UI for version selection
- **MEF Components**: Integrates with Visual Studio's extensibility framework

### Dependencies

- Microsoft.VisualStudio.SDK (17.14.40265)
- NuGet.Protocol (6.11.1)
- NuGet.Versioning (6.11.1)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

[Add license information here]

## Acknowledgments

Built with the Visual Studio SDK and NuGet client libraries.
