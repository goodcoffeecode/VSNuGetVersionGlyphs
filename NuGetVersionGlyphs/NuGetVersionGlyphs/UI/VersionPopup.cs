using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using NuGet.Versioning;
using NuGetVersionGlyphs.Models;

namespace NuGetVersionGlyphs.UI
{
    internal class VersionPopup
    {
        private const int PopupWidth = 250;
        private const int PopupMaxHeight = 300;
        private const int ItemPadding = 5;
        
        private readonly PackageReferenceInfo _package;
        private readonly List<NuGetVersion> _versions;
        private readonly IWpfTextView _view;
        private System.Windows.Controls.Primitives.Popup _popup;

        public event Action<string> VersionSelected;

        public VersionPopup(PackageReferenceInfo package, List<NuGetVersion> versions, IWpfTextView view)
        {
            _package = package;
            _versions = versions;
            _view = view;
        }

        public void Show()
        {
            var listBox = new ListBox
            {
                Width = PopupWidth,
                MaxHeight = PopupMaxHeight,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            foreach (var version in _versions)
            {
                var item = new ListBoxItem
                {
                    Content = version.ToString(),
                    Padding = new Thickness(ItemPadding),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                if (version.ToString() == _package.CurrentVersion)
                {
                    item.Background = new SolidColorBrush(Color.FromRgb(220, 240, 220));
                    item.FontWeight = FontWeights.Bold;
                    item.Content = $"{version} (current)";
                }

                item.MouseLeftButtonUp += (s, e) =>
                {
                    VersionSelected?.Invoke(version.ToString());
                    _popup?.IsOpen = false;
                };

                listBox.Items.Add(item);
            }

            var header = new TextBlock
            {
                Text = $"Versions for {_package.PackageId}",
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(ItemPadding),
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(header);
            stackPanel.Children.Add(listBox);

            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Child = stackPanel
            };

            _popup = new System.Windows.Controls.Primitives.Popup
            {
                Child = border,
                StaysOpen = false,
                AllowsTransparency = true,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse
            };

            _popup.IsOpen = true;
        }
    }
}
