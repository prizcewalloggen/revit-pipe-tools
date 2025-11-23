using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;

using WpfGrid = System.Windows.Controls.Grid;

namespace PipeLengthCalculator
{
    public class PipeDiagramWindow : Window
    {
        private PipeSystemData systemData;

        public PipeDiagramWindow(PipeSystemData data)
        {
            this.systemData = data;
            
            // Window properties
            Title = "Pipe System Diagram - Mitsubishi Diamond Builder";
            Width = 600;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Create layout
            WpfGrid mainGrid = new WpfGrid();
            mainGrid.Background = Brushes.White;

            // Add header
            TextBlock header = new TextBlock
            {
                Text = "Pipe System Schematic",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Create scrollviewer for tree view
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 50, 10, 60)
            };

            // Tree text
            TextBlock treeText = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.NoWrap
            };

            scrollViewer.Content = treeText;

            // Summary panel
            StackPanel summaryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            double totalLength = systemData.Pipes.Sum(p => p.Length);
            int teeCount = systemData.Fittings.Count(f => f.Type == "T");
            int elbowCount = systemData.Fittings.Count(f => f.Type == "Elbow");

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"Total Length: {totalLength:F2} ft  |  ",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5)
            });
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"Pipes: {systemData.Pipes.Count}  |  ",
                Margin = new Thickness(5)
            });
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"T-Fittings: {teeCount}  |  ",
                Margin = new Thickness(5)
            });
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"Elbows: {elbowCount}",
                Margin = new Thickness(5)
            });

            // Add all to grid
            mainGrid.Children.Add(header);
            mainGrid.Children.Add(scrollViewer);
            mainGrid.Children.Add(summaryPanel);

            Content = mainGrid;

            // Build the tree text
            treeText.Text = BuildTreeText();
        }

        private string BuildTreeText()
        {
            StringBuilder sb = new StringBuilder();
            
            // Add debug information
            sb.AppendLine("=== DEBUG INFORMATION ===");
            sb.AppendLine($"Total Pipes: {systemData.Pipes.Count}");
            sb.AppendLine($"Total T-Fittings: {systemData.Fittings.Count(f => f.Type == "T")}");
            sb.AppendLine();

            // Show all T-fittings and their connections
            sb.AppendLine("T-Fittings and their connected pipes:");
            var tFittings = systemData.Fittings.Where(f => f.Type == "T").ToList();
            for (int i = 0; i < tFittings.Count; i++)
            {
                var tee = tFittings[i];
                sb.AppendLine($"  T-Fitting {i + 1}: connects {tee.ConnectedPipes.Count} pipes, {tee.ConnectedFittings.Count} fittings");
                foreach (var pipeId in tee.ConnectedPipes)
                {
                    var pipe = systemData.Pipes.FirstOrDefault(p => p.Id == pipeId);
                    if (pipe != null)
                    {
                        sb.AppendLine($"    - Pipe: {FormatDiameterAsFraction(pipe.Diameter)} ø  |  {pipe.Length:F1} ft");
                    }
                }
                foreach (var fittingId in tee.ConnectedFittings)
                {
                    var fitting = systemData.Fittings.FirstOrDefault(f => f.Id == fittingId);
                    if (fitting != null)
                    {
                        sb.AppendLine($"    - Fitting: {fitting.Type}");
                    }
                }
            }
            sb.AppendLine();

            // Show all pipes and what they connect to
            sb.AppendLine("All Pipes:");
            foreach (var pipe in systemData.Pipes.OrderByDescending(p => p.Diameter))
            {
                sb.AppendLine($"  {FormatDiameterAsFraction(pipe.Diameter)} ø | {pipe.Length:F1} ft - Connects to {pipe.ConnectedFittings.Count} fittings, {pipe.ConnectedPipes.Count} pipes");
            }
            sb.AppendLine();

            // Show all fittings and their connections
            sb.AppendLine("All Fittings:");
            foreach (var fitting in systemData.Fittings.OrderBy(f => f.Type))
            {
                sb.AppendLine($"  {fitting.Type}: connects {fitting.ConnectedPipes.Count} pipes, {fitting.ConnectedFittings.Count} fittings");
            }
            sb.AppendLine();
            sb.AppendLine("=== PIPE SYSTEM TREE ===");
            sb.AppendLine();

            // Find the main header pipe - start with largest diameter, but prefer endpoints (1 fitting)
            var headerPipe = systemData.Pipes
                .OrderByDescending(p => p.Diameter)
                .ThenBy(p => p.ConnectedFittings.Count) // Prefer fewer fittings (endpoints)
                .ThenByDescending(p => p.Length)
                .First();

            HashSet<ElementId> drawnPipes = new HashSet<ElementId>();

            // Build tree starting from header
            BuildPipeTreeV2(headerPipe, "", true, null, sb, drawnPipes);

            // Add any remaining pipes that weren't connected to the main tree
            var remainingPipes = systemData.Pipes
                .Where(p => !drawnPipes.Contains(p.Id))
                .OrderByDescending(p => p.Diameter)
                .ThenByDescending(p => p.Length)
                .ToList();

            if (remainingPipes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== Additional Pipes (not connected to main tree) ===");
                foreach (var pipe in remainingPipes)
                {
                    sb.AppendLine($"● {FormatDiameterAsFraction(pipe.Diameter)} ø  |  {pipe.Length:F1} ft");
                    
                    // Show what this pipe connects to
                    if (pipe.ConnectedFittings.Count > 0)
                    {
                        sb.AppendLine($"  Connected to {pipe.ConnectedFittings.Count} fitting(s)");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Total pipes drawn in tree: {drawnPipes.Count}/{systemData.Pipes.Count}");

            return sb.ToString();
        }

        private void BuildPipeTreeV2(PipeSegment pipe, string indent, bool isLast, ElementId cameFromFittingId,
                                     StringBuilder sb, HashSet<ElementId> drawnPipes)
        {
            if (drawnPipes.Contains(pipe.Id))
                return;

            // Accumulate length through elbows and same-diameter pipes
            double totalLength = pipe.Length;
            double diameter = pipe.Diameter;
            int elbowCount = 0;
            PipeSegment currentPipe = pipe;
            drawnPipes.Add(pipe.Id);

            // Follow through elbows and non-branching fittings with same diameter
            bool continueAccumulating = true;
            ElementId lastFittingId = cameFromFittingId;

            while (continueAccumulating)
            {
                continueAccumulating = false;

                var connectedFittings = systemData.Fittings
                    .Where(f => f.ConnectedPipes.Contains(currentPipe.Id) &&
                               (lastFittingId == null || f.Id != lastFittingId))
                    .ToList();

                // Look for non-branching fittings (elbows, couplings) that continue the line
                var nonBranchingFittings = connectedFittings
                    .Where(f => f.Type != "T" && f.ConnectedPipes.Count == 2)
                    .ToList();

                foreach (var fitting in nonBranchingFittings)
                {
                    // Count elbows
                    if (fitting.Type == "Elbow")
                    {
                        elbowCount++;
                    }

                    // Traverse through this fitting to find the next pipe (might go through more fittings)
                    var result = TraverseThroughFittings(currentPipe.Id, fitting.Id, ref elbowCount);
                    
                    if (result.NextPipe != null && !drawnPipes.Contains(result.NextPipe.Id))
                    {
                        if (Math.Abs(result.NextPipe.Diameter - diameter) < 0.1)
                        {
                            // Same diameter, accumulate length
                            totalLength += result.NextPipe.Length;
                            drawnPipes.Add(result.NextPipe.Id);
                            currentPipe = result.NextPipe;
                            lastFittingId = result.LastFittingId;
                            continueAccumulating = true;
                            break;
                        }
                    }
                }
            }

            // Now find fittings on the final pipe in the accumulated segment
            var finalFittings = systemData.Fittings
                .Where(f => f.ConnectedPipes.Contains(currentPipe.Id) &&
                           (lastFittingId == null || f.Id != lastFittingId))
                .ToList();

            // Check if this is an endpoint (no more connections)
            bool isEndpoint = finalFittings.Count == 0;

            // Draw accumulated pipe info with elbow count and endpoint indicator
            string connector = isLast ? "└── " : "├── ";
            if (indent == "")
                connector = "● ";

            string diameterStr = FormatDiameterAsFraction(diameter);
            string elbowInfo = elbowCount > 0 ? $"  ({elbowCount} elbow{(elbowCount > 1 ? "s" : "")})" : "";
            string endpointSymbol = isEndpoint ? "  ►" : "";
            sb.AppendLine($"{indent}{connector}{diameterStr} ø  |  {totalLength:F1} ft{elbowInfo}{endpointSymbol}");

            if (finalFittings.Count == 0)
                return;

            // Get new indent
            string newIndent = indent + (isLast ? "    " : "│   ");

            // Separate T-fittings from other fittings
            var tFittings = finalFittings.Where(f => f.Type == "T").ToList();
            var otherFittings = finalFittings.Where(f => f.Type != "T").ToList();

            // Process T-fittings first (they create branches)
            foreach (var tee in tFittings)
            {
                var branchPipes = tee.ConnectedPipes
                    .Where(pId => pId != currentPipe.Id)
                    .Select(pId => systemData.Pipes.FirstOrDefault(p => p.Id == pId))
                    .Where(p => p != null && !drawnPipes.Contains(p.Id))
                    .OrderByDescending(p => p.Diameter)
                    .ThenByDescending(p => p.Length)
                    .ToList();

                if (branchPipes.Count == 0)
                    continue;

                var mainContinuation = branchPipes[0];
                var sideBranches = branchPipes.Skip(1).ToList();

                // Draw side branches first
                for (int i = 0; i < sideBranches.Count; i++)
                {
                    bool isLastSide = (i == sideBranches.Count - 1) && (mainContinuation == null || drawnPipes.Contains(mainContinuation.Id));
                    BuildPipeTreeV2(sideBranches[i], newIndent, isLastSide, tee.Id, sb, drawnPipes);
                }

                // Continue down main line
                if (mainContinuation != null && !drawnPipes.Contains(mainContinuation.Id))
                {
                    BuildPipeTreeV2(mainContinuation, newIndent, true, tee.Id, sb, drawnPipes);
                }
            }

            // Process reducers and other diameter-changing fittings
            foreach (var fitting in otherFittings)
            {
                var nextPipes = fitting.ConnectedPipes
                    .Where(pId => pId != currentPipe.Id)
                    .Select(pId => systemData.Pipes.FirstOrDefault(p => p.Id == pId))
                    .Where(p => p != null && !drawnPipes.Contains(p.Id))
                    .OrderByDescending(p => p.Diameter)
                    .ThenByDescending(p => p.Length)
                    .ToList();

                foreach (var nextPipe in nextPipes)
                {
                    BuildPipeTreeV2(nextPipe, newIndent, true, fitting.Id, sb, drawnPipes);
                }
            }
        }

        private (PipeSegment NextPipe, ElementId LastFittingId) TraverseThroughFittings(ElementId startPipeId, ElementId startFittingId, ref int elbowCount)
        {
            ElementId currentFittingId = startFittingId;
            ElementId previousFittingId = null;
            HashSet<ElementId> visitedFittings = new HashSet<ElementId> { startFittingId };

            while (true)
            {
                var currentFitting = systemData.Fittings.FirstOrDefault(f => f.Id == currentFittingId);
                if (currentFitting == null || currentFitting.Type == "T")
                    return (null, currentFittingId);

                // Find connected pipes (excluding the starting pipe if this is the first fitting)
                var connectedPipes = currentFitting.ConnectedPipes
                    .Where(id => id != startPipeId)  // Exclude the pipe we came from
                    .Select(id => systemData.Pipes.FirstOrDefault(p => p.Id == id))
                    .Where(p => p != null)
                    .ToList();

                if (connectedPipes.Count > 0)
                {
                    // Found a pipe - return it
                    return (connectedPipes[0], currentFittingId);
                }

                // No pipe found, check for connected fittings
                var connectedFittings = currentFitting.ConnectedFittings
                    .Where(id => id != previousFittingId && !visitedFittings.Contains(id))
                    .Select(id => systemData.Fittings.FirstOrDefault(f => f.Id == id))
                    .Where(f => f != null && f.Type != "T")
                    .ToList();

                if (connectedFittings.Count == 0)
                {
                    // Dead end
                    return (null, currentFittingId);
                }

                // Move to the next fitting
                var nextFitting = connectedFittings[0];
                
                // Count elbows
                if (nextFitting.Type == "Elbow")
                {
                    elbowCount++;
                }

                visitedFittings.Add(nextFitting.Id);
                previousFittingId = currentFittingId;  // Track where we came from
                currentFittingId = nextFitting.Id;
            }
        }

        private bool IsEndpointRecursive(ElementId pipeId, ElementId cameFromFittingId, 
                                         HashSet<ElementId> drawnPipes, HashSet<ElementId> checkedFittings)
        {
            // Find fittings connected to this pipe (excluding where we came from)
            var connectedFittings = systemData.Fittings
                .Where(f => f.ConnectedPipes.Contains(pipeId) &&
                           (cameFromFittingId == null || f.Id != cameFromFittingId) &&
                           !checkedFittings.Contains(f.Id))
                .ToList();

            if (connectedFittings.Count == 0)
                return true; // No more fittings = endpoint

            // Check each fitting
            foreach (var fitting in connectedFittings)
            {
                checkedFittings.Add(fitting.Id);

                // If it's a T-fitting with undrawn pipes, it's not an endpoint
                if (fitting.Type == "T")
                {
                    var branchPipes = fitting.ConnectedPipes
                        .Where(pId => pId != pipeId && !drawnPipes.Contains(pId))
                        .ToList();
                    
                    if (branchPipes.Count > 0)
                        return false;
                }

                // Find pipes through this fitting (excluding current pipe)
                var nextPipes = fitting.ConnectedPipes
                    .Where(pId => pId != pipeId)
                    .Select(pId => systemData.Pipes.FirstOrDefault(p => p.Id == pId))
                    .Where(p => p != null)
                    .ToList();

                foreach (var nextPipe in nextPipes)
                {
                    // If there's an undrawn pipe, it's not an endpoint (continues somewhere)
                    if (!drawnPipes.Contains(nextPipe.Id))
                        return false;

                    // For already drawn pipes, only recurse if they might lead to undrawn content
                    // This prevents false positives from looking backward at accumulated pipes
                    var nextFittings = systemData.Fittings
                        .Where(f => f.ConnectedPipes.Contains(nextPipe.Id) && 
                                   f.Id != fitting.Id &&
                                   !checkedFittings.Contains(f.Id))
                        .ToList();
                    
                    // Only recurse if there are unchecked fittings
                    if (nextFittings.Count > 0)
                    {
                        if (!IsEndpointRecursive(nextPipe.Id, fitting.Id, drawnPipes, checkedFittings))
                            return false;
                    }
                }
            }

            // All paths checked, this is an endpoint
            return true;
        }

        private string FormatDiameterAsFraction(double diameter)
        {
            int wholePart = (int)diameter;
            double fractionalPart = diameter - wholePart;

            // If very close to a whole number, just return the whole number
            if (Math.Abs(fractionalPart) < 0.05)
            {
                return wholePart + "\"";
            }

            // Common fractions for pipe sizes: 1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8
            string fraction = "";
            if (Math.Abs(fractionalPart - 0.125) < 0.05) fraction = "1/8";
            else if (Math.Abs(fractionalPart - 0.25) < 0.05) fraction = "1/4";
            else if (Math.Abs(fractionalPart - 0.375) < 0.05) fraction = "3/8";
            else if (Math.Abs(fractionalPart - 0.5) < 0.05) fraction = "1/2";
            else if (Math.Abs(fractionalPart - 0.625) < 0.05) fraction = "5/8";
            else if (Math.Abs(fractionalPart - 0.75) < 0.05) fraction = "3/4";
            else if (Math.Abs(fractionalPart - 0.875) < 0.05) fraction = "7/8";
            else
            {
                // If not a common fraction, use decimal
                return diameter.ToString("F2") + "\"";
            }

            if (wholePart > 0)
            {
                return wholePart + "-" + fraction + "\"";
            }
            else
            {
                return fraction + "\"";
            }
        }

    }
}