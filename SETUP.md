# Setup Instructions for GitHub

## Steps to publish this plugin to GitHub:

### 1. Initialize Git Repository
Open Command Prompt or PowerShell in this directory and run:
```bash
git init
git add .
git commit -m "Initial commit - Pipe Length Calculator Revit Plugin"
```

### 2. Create GitHub Repository
1. Go to https://github.com/new
2. Repository name: `revit-pipe-length-calculator`
3. Description: "Revit plugin for MEP pipe system analysis and visualization"
4. Choose Public or Private
5. **Do NOT** initialize with README, .gitignore, or license (we already have these)
6. Click "Create repository"

### 3. Push to GitHub
Copy the commands from GitHub's "push an existing repository" section:
```bash
git remote add origin https://github.com/YOUR-USERNAME/revit-pipe-length-calculator.git
git branch -M main
git push -u origin main
```

### 4. Copy Your Code Files
Copy your compiled plugin files to this directory:
- Copy all `.cs` files from your Visual Studio project to `PipeLengthCalculator/`
- Copy your `icon.png` to `PipeLengthCalculator/Resources/`
- Copy your `.csproj` file to `PipeLengthCalculator/`
- Copy your solution file (`.sln`) to the root directory

### 5. Create a Release
1. Go to your GitHub repository
2. Click "Releases" → "Create a new release"
3. Tag version: `v1.0.0`
4. Release title: "Initial Release - v1.0.0"
5. Description: List the features
6. Attach files:
   - `PipeLengthCalculator.dll` (compiled)
   - `PipeLengthCalculator.addin`
7. Click "Publish release"

### 6. Add Screenshots
1. Take screenshots of your plugin in action
2. Save them to the `Images/` folder
3. Update README.md to reference the images
4. Commit and push the images

### 7. Optional: Add Topics
Add GitHub topics to make your repo discoverable:
- revit
- revit-api
- revit-plugin
- mep
- bim
- csharp
- dotnet

## Files to Copy from Visual Studio

From your Visual Studio project, copy these files to `PipeLengthCalculator/`:
- [ ] Application.cs
- [ ] CalculatePipeLengthCommand.cs
- [ ] ShowPipeDiagramCommand.cs
- [ ] PipeDiagramWindow.cs
- [ ] GetHeightFromBaseCommand.cs
- [ ] PipeLengthCalculator.csproj
- [ ] Resources/icon.png

From your solution folder:
- [ ] PipeLengthCalculator.sln

## Next Steps After Publishing

1. Add a GitHub Actions workflow for automated builds (optional)
2. Create issues for future enhancements
3. Add a CHANGELOG.md file to track versions
4. Consider adding unit tests
5. Document the code with XML comments

## Notes
- Remember to generate a new GUID for the .addin file ClientId
- Test the plugin installation from the release files before announcing
- Consider creating a demo video showing the plugin in action
