using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using NuGetVersionGlyphs.Models;
using NuGetVersionGlyphs.Parsers;
using NuGetVersionGlyphs.Services;
using NuGetVersionGlyphs.UI;

namespace NuGetVersionGlyphs.Adornments
{
    internal sealed class NuGetVersionAdornment
    {
        private readonly IAdornmentLayer _layer;
        private readonly IWpfTextView _view;
        private readonly NuGetService _nugetService;
        private readonly Dictionary<int, PackageReferenceInfo> _packagesByLine;
        private readonly Dictionary<int, UIElement> _adornmentsByLine;

        public NuGetVersionAdornment(IWpfTextView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layer = view.GetAdornmentLayer("NuGetVersionAdornmentLayer");
            _nugetService = new NuGetService();
            _packagesByLine = new Dictionary<int, PackageReferenceInfo>();
            _adornmentsByLine = new Dictionary<int, UIElement>();

            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnViewClosed;

            CreateAdornments();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnViewClosed;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                CreateVisualForLine(line);
            }
        }

        private async void CreateAdornments()
        {
            await Task.Run(async () =>
            {
                var snapshot = _view.TextSnapshot;
                var text = snapshot.GetText();
                var packages = CsprojParser.ParsePackageReferences(text);

                foreach (var package in packages)
                {
                    var latestVersion = await _nugetService.GetLatestVersionAsync(package.PackageId);
                    if (latestVersion != null)
                    {
                        package.LatestVersion = latestVersion.ToString();
                        package.IsUpToDate = package.CurrentVersion == package.LatestVersion;
                        _packagesByLine[package.LineNumber] = package;
                    }
                }

                await _view.VisualElement.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var line in _view.TextViewLines)
                    {
                        CreateVisualForLine(line);
                    }
                });
            });
        }

        private void CreateVisualForLine(ITextViewLine line)
        {
            var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position);

            if (!_packagesByLine.TryGetValue(lineNumber, out var package))
                return;

            if (_adornmentsByLine.ContainsKey(lineNumber))
                return;

            var glyph = CreateGlyph(package);
            if (glyph == null)
                return;

            glyph.MouseLeftButtonDown += (s, e) => OnGlyphClick(package);

            Canvas.SetLeft(glyph, line.Right + 5);
            Canvas.SetTop(glyph, line.Top);

            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, line.Extent, null, glyph, null);
            _adornmentsByLine[lineNumber] = glyph;
        }

        private UIElement CreateGlyph(PackageReferenceInfo package)
        {
            var canvas = new Canvas
            {
                Width = 16,
                Height = 16
            };

            if (package.IsUpToDate)
            {
                // Green checkmark for up-to-date packages
                var checkPath = new Path
                {
                    Data = Geometry.Parse("M 2,8 L 6,12 L 14,2"),
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    StrokeLineJoin = PenLineJoin.Round
                };
                canvas.Children.Add(checkPath);
            }
            else
            {
                // Blue "new" badge for packages with updates
                var ellipse = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = Brushes.DodgerBlue
                };
                canvas.Children.Add(ellipse);

                var text = new TextBlock
                {
                    Text = "N",
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(text, 4);
                Canvas.SetTop(text, 2);
                canvas.Children.Add(text);
            }

            canvas.Cursor = System.Windows.Input.Cursors.Hand;
            canvas.ToolTip = package.IsUpToDate
                ? $"{package.PackageId} is up-to-date (v{package.CurrentVersion})"
                : $"{package.PackageId} v{package.CurrentVersion} → v{package.LatestVersion} available";

            return canvas;
        }

        private async void OnGlyphClick(PackageReferenceInfo package)
        {
            var versions = await _nugetService.GetVersionsAroundAsync(package.PackageId, package.CurrentVersion);
            
            var popup = new VersionPopup(package, versions, _view);
            popup.VersionSelected += (selectedVersion) =>
            {
                UpdatePackageVersion(package, selectedVersion);
            };
            
            popup.Show();
        }

        private void UpdatePackageVersion(PackageReferenceInfo package, string newVersion)
        {
            var snapshot = _view.TextSnapshot;
            var line = snapshot.GetLineFromLineNumber(package.LineNumber);
            var lineText = line.GetText();

            var oldVersionPattern = $"Version=\"{package.CurrentVersion}\"";
            var newVersionText = $"Version=\"{newVersion}\"";
            var updatedLine = lineText.Replace(oldVersionPattern, newVersionText);

            var edit = snapshot.TextBuffer.CreateEdit();
            edit.Replace(line.Extent, updatedLine);
            edit.Apply();
        }
    }
}
