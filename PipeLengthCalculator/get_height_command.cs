using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PipeLengthCalculator
{
    [Transaction(TransactionMode.ReadOnly)]
    public class GetHeightFromBaseCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Get current selection
                Selection sel = uidoc.Selection;
                ICollection<ElementId> selectedIds = sel.GetElementIds();

                if (selectedIds.Count == 0)
                {
                    TaskDialog.Show("Get Height From Base", 
                        "Please select one or more elements first.");
                    return Result.Cancelled;
                }

                // Get project base point
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                BasePoint projectBasePoint = collector
                    .OfClass(typeof(BasePoint))
                    .Cast<BasePoint>()
                    .FirstOrDefault(bp => !bp.IsShared);

                double baseElevation = 0;
                if (projectBasePoint != null)
                {
                    Parameter elevParam = projectBasePoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                    if (elevParam != null && elevParam.HasValue)
                    {
                        baseElevation = elevParam.AsDouble();
                    }
                }

                // Process each selected element
                List<ElementHeightInfo> heightInfos = new List<ElementHeightInfo>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem == null) continue;

                    ElementHeightInfo info = new ElementHeightInfo();
                    info.ElementName = elem.Name;
                    info.Category = elem.Category?.Name ?? "Unknown";
                    info.ElementId = elem.Id.ToString();

                    // Try to get the elevation/height of the element
                    double? elementElevation = GetElementElevation(elem);

                    if (elementElevation.HasValue)
                    {
                        info.Elevation = elementElevation.Value;
                        info.HeightFromBase = elementElevation.Value - baseElevation;
                        info.Success = true;
                    }
                    else
                    {
                        info.Success = false;
                        info.ErrorMessage = "Could not determine elevation";
                    }

                    heightInfos.Add(info);
                }

                // Display results
                ShowResults(heightInfos, baseElevation, doc.GetUnits());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }

        private double? GetElementElevation(Element elem)
        {
            // Try different methods to get elevation based on element type

            // Method 1: Check for INSTANCE_ELEVATION_PARAM
            Parameter elevParam = elem.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevParam != null && elevParam.HasValue)
            {
                return elevParam.AsDouble();
            }

            // Method 2: Check for SCHEDULE_BASE_LEVEL_OFFSET_PARAM
            Parameter offsetParam = elem.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM);
            Parameter levelParam = elem.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
            
            if (offsetParam != null && offsetParam.HasValue && levelParam != null && levelParam.HasValue)
            {
                ElementId levelId = levelParam.AsElementId();
                Level level = elem.Document.GetElement(levelId) as Level;
                if (level != null)
                {
                    return level.Elevation + offsetParam.AsDouble();
                }
            }

            // Method 3: Try to get location point
            LocationPoint locPoint = elem.Location as LocationPoint;
            if (locPoint != null)
            {
                return locPoint.Point.Z;
            }

            // Method 4: Try to get bounding box center
            BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
            if (bbox != null)
            {
                return (bbox.Min.Z + bbox.Max.Z) / 2.0;
            }

            // Method 5: For MEP elements, try offset from level
            if (elem.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM) != null)
            {
                Parameter offsetP = elem.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                Parameter levelP = elem.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                
                if (offsetP != null && offsetP.HasValue && levelP != null && levelP.HasValue)
                {
                    ElementId levelId = levelP.AsElementId();
                    Level level = elem.Document.GetElement(levelId) as Level;
                    if (level != null)
                    {
                        return level.Elevation + offsetP.AsDouble();
                    }
                }
            }

            return null;
        }

        private void ShowResults(List<ElementHeightInfo> heightInfos, double baseElevation, Units units)
        {
            TaskDialog dialog = new TaskDialog("Height From Base Elevation");
            dialog.MainInstruction = "Element Heights";

            string details = $"Project Base Elevation: {FormatLength(baseElevation, units)}\n\n";

            foreach (var info in heightInfos)
            {
                details += $"Element: {info.ElementName}\n";
                details += $"  Category: {info.Category}\n";
                details += $"  ID: {info.ElementId}\n";

                if (info.Success)
                {
                    details += $"  Elevation: {FormatLength(info.Elevation, units)}\n";
                    details += $"  Height from Base: {FormatLength(info.HeightFromBase, units)}\n";
                }
                else
                {
                    details += $"  Error: {info.ErrorMessage}\n";
                }

                details += "\n";
            }

            dialog.MainContent = details;
            dialog.Show();
        }

        private string FormatLength(double lengthInFeet, Units units)
        {
            string formatted = UnitFormatUtils.Format(
                units,
                SpecTypeId.Length,
                lengthInFeet,
                false);

            // Also show in feet and inches for reference
            int feet = (int)lengthInFeet;
            double inches = (lengthInFeet - feet) * 12;
            
            return $"{formatted} ({feet}'-{inches:F2}\")";
        }
    }

    public class ElementHeightInfo
    {
        public string ElementName { get; set; }
        public string Category { get; set; }
        public string ElementId { get; set; }
        public double Elevation { get; set; }
        public double HeightFromBase { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}