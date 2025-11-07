using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace NuGetVersionGlyphs.Adornments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("xml")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class NuGetVersionAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("NuGetVersionAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            // Only activate for .csproj files
            var document = textView.TextBuffer.Properties.TryGetProperty<Microsoft.VisualStudio.Text.ITextDocument>(
                typeof(Microsoft.VisualStudio.Text.ITextDocument), out var doc) ? doc : null;

            if (document != null && document.FilePath.EndsWith(".csproj", System.StringComparison.OrdinalIgnoreCase))
            {
                new NuGetVersionAdornment(textView);
            }
        }
    }
}
