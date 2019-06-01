using System;
using System.Linq;
using System.Collections;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace OmniView
{
	[Export(typeof(IWpfTextViewMarginProvider))]
	[Name("OmniViewMargin")]
	[Order(After = PredefinedMarginNames.VerticalScrollBarContainer)]
	[MarginContainer(PredefinedMarginNames.Right)]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	internal sealed class OmniViewFactory : IWpfTextViewMarginProvider
	{
		[Import]
		ITextDocumentFactoryService TextDocumentFactoryService = null;

		/*[Import]
		IVsEditorAdaptersFactoryService AdaptersFactory = null;

		[Import]
		SVsServiceProvider ServiceProvider = null;*/

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
		{
			ITextDocument document;
			if(!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
				return null;
			
			var ct = document.TextBuffer.ContentType;
			if(ct.IsOfType("htmlx"))// || document.FilePath.EndsWith(".tis"))
			{
				IWpfTextView textView = wpfTextViewHost.TextView;
				if(textView.Roles.Any(r => r.Contains("DIFF")))
					return null;

				var props = textView.Properties.PropertyList;
				if(props.Count(p => p.Value.GetType().FullName.Contains("Difference")) != 0)
					return null;

				//IVsTextView textViewAdapter = AdaptersFactory.GetViewAdapter(textView);
				//textView.Properties.GetOrCreateSingletonProperty(() => new CommandFilter(textView, textViewAdapter, ServiceProvider));

				return new SciterMargin(textView, document);
			}
			return null;
		}
	}
}