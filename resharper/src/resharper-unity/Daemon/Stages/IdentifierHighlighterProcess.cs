using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages
{
    internal class IdentifierHighlighterProcess : ShaderLabDaemonStageProcessBase
    {
        private readonly DaemonProcessKind myProcessKind;
        private readonly bool myIdentifierHighlightingEnabled;
        private readonly VisualElementHighlighter myVisualElementHighlighter;

        public IdentifierHighlighterProcess(IDaemonProcess process, ResolveHighlighterRegistrar registrar,
            IContextBoundSettingsStore settingsStore, DaemonProcessKind processKind, IShaderLabFile file,
            ConfigurableIdentifierHighlightingStageService identifierHighlightingStageService)
            : base(process, settingsStore, file)
        {
            myProcessKind = processKind;
            myIdentifierHighlightingEnabled = identifierHighlightingStageService.ShouldHighlightIdentifiers(settingsStore);
            myVisualElementHighlighter = new VisualElementHighlighter(ShaderLabLanguage.Instance, settingsStore);
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
        {
            if (myProcessKind == DaemonProcessKind.VISIBLE_DOCUMENT)
            {
                if (myIdentifierHighlightingEnabled)
                {
                    var info = myVisualElementHighlighter.CreateColorHighlightingInfo(node);
                    if (info != null)
                        context.AddHighlighting(info);
                }
            }

            // TODO: Resolve problem highlighter

            // TODO: Move to ShaderLabSyntaxHighlightingStage
            // (Not Rider's syntax highlighting though!)
            // E.g. protobuf support has a SyntaxHighlightingStage that will do this,
            // plus look for simple syntax validation errors, e.g. enums must have at
            // least one value defined, correct value for `syntax "proto3"`, etc.
            // And then a separate identifier
            var errorElement = node as IErrorElement;
            if (errorElement != null)
            {
                var range = errorElement.GetDocumentRange();
                if (!range.IsValid())
                    range = node.Parent.GetDocumentRange();
                if (range.TextRange.IsEmpty)
                {
                    if (range.TextRange.EndOffset < range.Document.GetTextLength())
                        range = range.ExtendRight(1);
                    else
                        range = range.ExtendLeft(1);
                }
                context.AddHighlighting(new ShaderLabSyntaxError(errorElement.ErrorDescription, range));
            }

            base.VisitNode(node, context);
        }
    }
}