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
#pragma warning disable 649
        public AdornmentLayerDefinition editorAdornmentLayer;
#pragma warning restore 649

        public void TextViewCreated(IWpfTextView textView)
        {
            // Only activate for .csproj files
            Microsoft.VisualStudio.Text.ITextDocument document;
            if (textView.TextBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out document))
            {
                if (document != null && document.FilePath.EndsWith(".csproj", System.StringComparison.OrdinalIgnoreCase))
                {
                    new NuGetVersionAdornment(textView);
                }
            }
        }
    }
}
