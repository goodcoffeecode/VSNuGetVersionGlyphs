using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const double GlyphLeftOffset = 5;
        private const int GlyphSize = 16;
        
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

            _ = InitializeAdornmentsAsync();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnViewClosed;
            if (_nugetService != null)
            {
                _nugetService.Dispose();
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                CreateVisualForLine(line);
            }
        }

        private async Task InitializeAdornmentsAsync()
        {
            try
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
            }
            catch (Exception)
            {
                // Silently fail if adornment initialization fails
            }
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

            glyph.MouseLeftButtonDown += (s, e) => _ = OnGlyphClickAsync(package);

            Canvas.SetLeft(glyph, line.Right + GlyphLeftOffset);
            Canvas.SetTop(glyph, line.Top);

            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, line.Extent, null, glyph, null);
            _adornmentsByLine[lineNumber] = glyph;
        }

        private UIElement CreateGlyph(PackageReferenceInfo package)
        {
            var canvas = new Canvas
            {
                Width = GlyphSize,
                Height = GlyphSize
            };

            if (package.IsUpToDate)
            {
                // Green checkmark for up-to-date packages
                var ellipse = new Ellipse
                {
                    Width = GlyphSize,
                    Height = GlyphSize,
                    Fill = Brushes.Green
                };
                canvas.Children.Add(ellipse);
            }
            else
            {
                // Blue "new" badge for packages with updates
                var ellipse = new Ellipse
                {
                    Width = GlyphSize,
                    Height = GlyphSize,
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

        private async Task OnGlyphClickAsync(PackageReferenceInfo package)
        {
            try
            {
                var versions = await _nugetService.GetVersionsAroundAsync(package.PackageId, package.CurrentVersion);
                
                await _view.VisualElement.Dispatcher.InvokeAsync(() =>
                {
                    var popup = new VersionPopup(package, versions);
                    popup.VersionSelected += (selectedVersion) =>
                    {
                        UpdatePackageVersion(package, selectedVersion);
                    };
                    
                    popup.Show();
                });
            }
            catch (Exception)
            {
                // Silently fail if version retrieval fails
            }
        }

        private void UpdatePackageVersion(PackageReferenceInfo package, string newVersion)
        {
            var snapshot = _view.TextSnapshot;
            var line = snapshot.GetLineFromLineNumber(package.LineNumber);
            var lineText = line.GetText();

            // Use regex for more precise replacement
            var versionRegex = new Regex(@"Version\s*=\s*""[^""]+""", RegexOptions.IgnoreCase);
            var updatedLine = versionRegex.Replace(lineText, $"Version=\"{newVersion}\"", 1);

            var edit = snapshot.TextBuffer.CreateEdit();
            edit.Replace(line.Extent, updatedLine);
            edit.Apply();
        }
    }
}
