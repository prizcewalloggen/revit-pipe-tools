using System;
using System.Reflection;
using System.IO;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

namespace PipeLengthCalculator
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create a ribbon panel
                RibbonPanel ribbonPanel = application.CreateRibbonPanel("Pipe Tools");

                // Get the assembly path
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create a pulldown button (dropdown menu)
                PulldownButtonData pulldownData = new PulldownButtonData("PipeToolsDropdown", "Pipe Tools");
                PulldownButton pulldownButton = ribbonPanel.AddItem(pulldownData) as PulldownButton;

                // Set icon for the pulldown button
                pulldownButton.LargeImage = GetEmbeddedImage("icon.png");
                pulldownButton.ToolTip = "Pipe calculation and visualization tools";

                // Add first button: Calculate Pipe Length
                PushButtonData calcButtonData = new PushButtonData(
                    "PipeLengthCalc",
                    "Calculate Pipe Length",
                    assemblyPath,
                    "PipeLengthCalculator.CalculatePipeLengthCommand");
                calcButtonData.ToolTip = "Calculate total length of selected pipes and count elbows";
                calcButtonData.LargeImage = GetEmbeddedImage("icon.png");
                pulldownButton.AddPushButton(calcButtonData);

                // Add second button: Show Pipe Diagram
                PushButtonData diagramButtonData = new PushButtonData(
                    "PipeDiagram",
                    "Show Pipe Diagram",
                    assemblyPath,
                    "PipeLengthCalculator.ShowPipeDiagramCommand");
                diagramButtonData.ToolTip = "Display schematic diagram of pipe system with T-fittings and branches";
                diagramButtonData.LongDescription = "Shows a visual diagram of the piping system including pipe lengths, diameters, and branch connections at T-fittings. Designed for Mitsubishi Diamond System Builder.";
                diagramButtonData.LargeImage = GetEmbeddedImage("icon.png");
                pulldownButton.AddPushButton(diagramButtonData);

                // Add third button: Get Height From Base
                PushButtonData heightButtonData = new PushButtonData(
                    "GetHeightFromBase",
                    "Get Height From Base",
                    assemblyPath,
                    "PipeLengthCalculator.GetHeightFromBaseCommand");
                heightButtonData.ToolTip = "Get the height of selected elements from project base elevation";
                heightButtonData.LongDescription = "Calculates the vertical distance from the project base point to selected elements.";
                heightButtonData.LargeImage = GetEmbeddedImage("icon.png");
                pulldownButton.AddPushButton(heightButtonData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Clean up if needed
            return Result.Succeeded;
        }

        private BitmapImage GetEmbeddedImage(string imageName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"PipeLengthCalculator.Resources.{imageName}";
                
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
                
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}