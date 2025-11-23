# Pipe Length Calculator - Revit Plugin

A comprehensive Revit plugin for MEP pipe system analysis and visualization, designed for Mitsubishi Diamond System Builder workflows.

## Features

### 1. Calculate Pipe Length
- Calculates total length of selected pipes
- Counts elbows and T-fittings
- Shows results in multiple units (feet, inches, meters)

### 2. Show Pipe Diagram
- Generates hierarchical tree diagram of piping systems
- Shows pipe diameters in fractional format (e.g., 4", 2-1/2")
- Accumulates lengths through elbows (including consecutive elbows)
- Displays elbow count for each run
- Marks endpoints with ► symbol
- Groups connected pipes of same diameter
- Perfect for Mitsubishi Diamond System Builder input

### 3. Get Height From Base
- Measures vertical distance from project base point
- Works with pipes, equipment, and fixtures
- Shows elevation in project units

## Installation

1. Download the latest release
2. Copy `PipeLengthCalculator.dll` and `PipeLengthCalculator.addin` to:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\[YEAR]\
   ```
3. Edit the `.addin` file and generate a new GUID for `<ClientId>`
4. Restart Revit

## Requirements

- Autodesk Revit 2024 or later
- .NET Framework 4.8
- Windows 10/11

## Usage

### Calculate Pipe Length
1. Select pipes and fittings in your model
2. Click **Pipe Tools** dropdown in the Add-Ins tab
3. Select **Calculate Pipe Length**
4. View total length, pipe count, and fitting counts

### Show Pipe Diagram
1. Select all pipes and fittings in a system
2. Click **Pipe Tools** > **Show Pipe Diagram**
3. View the tree diagram showing:
   - Main header pipes
   - Branch connections at T-fittings
   - Pipe sizes and lengths
   - Elbow counts per run
   - Endpoint indicators (►)

Example output:
```
● 4" ø  |  21.2 ft  (2 elbows)
    ├── 2" ø  |  4.3 ft  ►
    ├── 2" ø  |  6.9 ft  (1 elbow)  ►
    └── 4" ø  |  10.6 ft
```

### Get Height From Base
1. Select one or more elements
2. Click **Pipe Tools** > **Get Height From Base**
3. View elevation and height from project base point

## Building from Source

### Prerequisites
- Visual Studio 2019 or later
- Revit 2024 SDK

### Build Steps
1. Clone the repository
2. Open `PipeLengthCalculator.sln` in Visual Studio
3. Add references to:
   - `RevitAPI.dll`
   - `RevitAPIUI.dll`
   - `WindowsBase.dll`
   - `PresentationCore.dll`
   - `PresentationFramework.dll`
4. Build the solution (Release or Debug)
5. Output will be in `bin/Release/` or `bin/Debug/`

## Configuration

The `.addin` manifest file should look like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Assembly>PipeLengthCalculator.dll</Assembly>
    <ClientId>YOUR-GUID-HERE</ClientId>
    <FullClassName>PipeLengthCalculator.Application</FullClassName>
    <Name>Pipe Length Calculator</Name>
    <VendorId>Pearce O'Connor</VendorId>
    <VendorDescription>MEP Tools</VendorDescription>
  </AddIn>
</RevitAddIns>
```

## Key Features

### Intelligent Pipe Accumulation
The plugin intelligently combines pipe segments of the same diameter that are connected by elbows, showing the total run length and elbow count. This includes handling:
- Single elbows
- Consecutive elbows (elbow-to-elbow connections)
- Multiple elbows in a run

### Fitting-to-Fitting Connections
Properly handles complex piping configurations where fittings connect directly to other fittings without intervening pipe segments.

### Hierarchical Tree Display
Uses an easy-to-read tree structure with:
- ASCII art connectors (├──, └──, │)
- Fractional diameter display
- Endpoint markers
- Grouped pipe runs

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - see LICENSE file for details

## Author

Pearce O'Connor

## Acknowledgments

- Designed for Mitsubishi Diamond System Builder workflows
- Built with Revit API
- Supports complex MEP piping system analysis
