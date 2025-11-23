using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PipeLengthCalculator
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowPipeDiagramCommand : IExternalCommand
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
                    TaskDialog.Show("Pipe Diagram", 
                        "Please select pipes and fittings first.");
                    return Result.Cancelled;
                }

                // Collect pipes and fittings
                List<Pipe> pipes = new List<Pipe>();
                List<FamilyInstance> fittings = new List<FamilyInstance>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    
                    if (elem is Pipe pipe)
                    {
                        pipes.Add(pipe);
                    }
                    else if (elem is FamilyInstance fi && 
                             elem.Category?.Id.Value == (long)BuiltInCategory.OST_PipeFitting)
                    {
                        fittings.Add(fi);
                    }
                }

                if (pipes.Count == 0)
                {
                    TaskDialog.Show("Pipe Diagram", 
                        "No pipes found in selection.");
                    return Result.Cancelled;
                }

                // Build pipe system data
                PipeSystemData systemData = BuildPipeSystemData(doc, pipes, fittings);

                // Show the diagram window
                PipeDiagramWindow window = new PipeDiagramWindow(systemData);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message + "\n\n" + ex.StackTrace);
                return Result.Failed;
            }
        }

        private PipeSystemData BuildPipeSystemData(Document doc, List<Pipe> pipes, List<FamilyInstance> fittings)
        {
            PipeSystemData data = new PipeSystemData();

            // Process pipes
            foreach (Pipe pipe in pipes)
            {
                PipeSegment segment = new PipeSegment();
                segment.Id = pipe.Id;
                
                // Get diameter
                Parameter diamParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diamParam != null && diamParam.HasValue)
                {
                    double diamInFeet = diamParam.AsDouble();
                    segment.Diameter = diamInFeet * 12; // Convert to inches
                }

                // Get length
                Parameter lengthParam = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                if (lengthParam != null && lengthParam.HasValue)
                {
                    segment.Length = lengthParam.AsDouble(); // In feet
                }

                // Get connectors and find connected elements
                ConnectorSet connectors = pipe.ConnectorManager?.Connectors;
                if (connectors != null)
                {
                    foreach (Connector conn in connectors)
                    {
                        // Get all connected connectors
                        ConnectorSet allRefs = conn.AllRefs;
                        if (allRefs != null)
                        {
                            foreach (Connector refConn in allRefs)
                            {
                                // Skip the connector itself
                                if (refConn.Owner.Id == pipe.Id)
                                    continue;

                                Element connectedElement = refConn.Owner;
                                
                                // Check if it's a fitting we're tracking
                                if (connectedElement is FamilyInstance fitting && 
                                    fittings.Any(f => f.Id == fitting.Id))
                                {
                                    if (!segment.ConnectedFittings.Contains(fitting.Id))
                                    {
                                        segment.ConnectedFittings.Add(fitting.Id);
                                    }
                                }
                                // Check if it's another pipe we're tracking
                                else if (connectedElement is Pipe connectedPipe && 
                                         pipes.Any(p => p.Id == connectedPipe.Id))
                                {
                                    if (!segment.ConnectedPipes.Contains(connectedPipe.Id))
                                    {
                                        segment.ConnectedPipes.Add(connectedPipe.Id);
                                    }
                                }
                            }
                        }
                    }
                }

                data.Pipes.Add(segment);
            }

            // Process fittings
            foreach (FamilyInstance fitting in fittings)
            {
                FittingData fittingData = new FittingData();
                fittingData.Id = fitting.Id;
                fittingData.Name = fitting.Symbol.FamilyName;
                fittingData.Type = GetFittingType(fitting);

                // Get connected pipes for this fitting
                ConnectorSet connectors = null;
                if (fitting.MEPModel != null)
                {
                    connectors = fitting.MEPModel.ConnectorManager?.Connectors;
                }

                if (connectors != null)
                {
                    fittingData.ConnectionCount = connectors.Size;
                    
                    foreach (Connector conn in connectors)
                    {
                        ConnectorSet allRefs = conn.AllRefs;
                        if (allRefs != null)
                        {
                            foreach (Connector refConn in allRefs)
                            {
                                // Skip the connector itself
                                if (refConn.Owner.Id == fitting.Id)
                                    continue;

                                Element connectedElement = refConn.Owner;
                                
                                // Track connected pipes
                                if (connectedElement is Pipe connectedPipe && 
                                    pipes.Any(p => p.Id == connectedPipe.Id))
                                {
                                    if (!fittingData.ConnectedPipes.Contains(connectedPipe.Id))
                                    {
                                        fittingData.ConnectedPipes.Add(connectedPipe.Id);
                                    }
                                }
                                // Track connected fittings
                                else if (connectedElement is FamilyInstance connectedFitting &&
                                         fittings.Any(f => f.Id == connectedFitting.Id))
                                {
                                    if (!fittingData.ConnectedFittings.Contains(connectedFitting.Id))
                                    {
                                        fittingData.ConnectedFittings.Add(connectedFitting.Id);
                                    }
                                }
                            }
                        }
                    }
                }

                data.Fittings.Add(fittingData);
            }

            return data;
        }

        private string GetFittingType(FamilyInstance fitting)
        {
            string familyName = fitting.Symbol.FamilyName.ToLower();
            string typeName = fitting.Name.ToLower();
            
            if (familyName.Contains("tee") || typeName.Contains("tee"))
                return "T";
            else if (familyName.Contains("elbow") || typeName.Contains("elbow"))
                return "Elbow";
            else if (familyName.Contains("coupling") || typeName.Contains("coupling"))
                return "Coupling";
            else if (familyName.Contains("cap") || typeName.Contains("cap"))
                return "Cap";
            else
                return "Fitting";
        }
    }

    // Data classes
    public class PipeSystemData
    {
        public List<PipeSegment> Pipes { get; set; } = new List<PipeSegment>();
        public List<FittingData> Fittings { get; set; } = new List<FittingData>();
    }

    public class PipeSegment
    {
        public ElementId Id { get; set; }
        public double Length { get; set; } // In feet
        public double Diameter { get; set; } // In inches
        public List<ElementId> ConnectedFittings { get; set; } = new List<ElementId>();
        public List<ElementId> ConnectedPipes { get; set; } = new List<ElementId>();
    }

    public class FittingData
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int ConnectionCount { get; set; }
        public List<ElementId> ConnectedPipes { get; set; } = new List<ElementId>();
        public List<ElementId> ConnectedFittings { get; set; } = new List<ElementId>();
    }
}