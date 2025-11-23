using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PipeLengthCalculator
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CalculatePipeLengthCommand : IExternalCommand
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
                    TaskDialog.Show("Pipe Length Calculator", 
                        "Please select one or more pipes first.");
                    return Result.Cancelled;
                }

                // Filter for pipe elements and calculate total length
                double totalLength = 0;
                int pipeCount = 0;
                int elbowCount = 0;
                List<string> nonPipeElements = new List<string>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    
                    if (elem is Pipe pipe)
                    {
                        // Get the length parameter
                        Parameter lengthParam = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                        
                        if (lengthParam != null && lengthParam.HasValue)
                        {
                            // Length is in internal units (feet)
                            totalLength += lengthParam.AsDouble();
                            pipeCount++;
                        }
                    }
                    else if (elem is FamilyInstance fi && 
                             elem.Category?.Id.Value == (long)BuiltInCategory.OST_PipeFitting)
                    {
                        // Check if this fitting is an elbow
                        string familyName = fi.Symbol.FamilyName.ToLower();
                        string typeName = fi.Name.ToLower();
                        
                        if (familyName.Contains("elbow") || typeName.Contains("elbow"))
                        {
                            elbowCount++;
                        }
                    }
                    else
                    {
                        nonPipeElements.Add($"{elem.Name} (ID: {elem.Id})");
                    }
                }

                // Display results
                if (pipeCount == 0)
                {
                    TaskDialog.Show("Pipe Length Calculator", 
                        "No pipes found in selection.");
                    return Result.Cancelled;
                }

                // Convert to display units (feet to desired unit)
                string resultMessage = FormatResults(doc, totalLength, pipeCount, elbowCount, nonPipeElements);
                
                TaskDialog dlg = new TaskDialog("Pipe Length Calculator");
                dlg.MainContent = resultMessage;
                dlg.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private string FormatResults(Document doc, double lengthInFeet, int pipeCount, int elbowCount, List<string> nonPipes)
        {
            // Get document units for length
            Units units = doc.GetUnits();
            FormatOptions formatOptions = units.GetFormatOptions(SpecTypeId.Length);
            
            // Format the total length in document units
            string formattedLength = UnitFormatUtils.Format(
                units, 
                SpecTypeId.Length, 
                lengthInFeet, 
                false);

            // Build result message
            string result = $"Total Pipe Length: {formattedLength}\n";
            result += $"Number of Pipes: {pipeCount}\n";
            result += $"Number of Elbows: {elbowCount}\n";
            
            // Also show in common units for reference
            result += $"\nAlternate Units:\n";
            result += $"  {lengthInFeet:F2} ft\n";
            result += $"  {lengthInFeet * 12:F2} in\n";
            result += $"  {lengthInFeet * 0.3048:F2} m\n";

            if (nonPipes.Count > 0)
            {
                result += $"\n⚠ {nonPipes.Count} non-pipe element(s) were skipped:\n";
                result += string.Join("\n", nonPipes.Take(5));
                if (nonPipes.Count > 5)
                {
                    result += $"\n... and {nonPipes.Count - 5} more";
                }
            }

            return result;
        }
    }
}