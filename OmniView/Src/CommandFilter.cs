using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OmniView
{
	internal sealed class CommandFilter : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandTarget;
		private IWpfTextView TextView;
		private SVsServiceProvider Service;

		public CommandFilter(IWpfTextView textView, IVsTextView textViewAdapter, SVsServiceProvider service)
		{
			Dispatcher.CurrentDispatcher.InvokeAsync(() =>
			{
				ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(this, out _nextCommandTarget));
			}, DispatcherPriority.ApplicationIdle);

			TextView = textView;
			Service = service;
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if(VsShellUtilities.IsInAutomationFunction(Service))
				return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			if(pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
			{
				if(nCmdID == (uint)VSConstants.VSStd97CmdID.SaveSolution)
				{

				}
			}

			return _nextCommandTarget.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandTarget.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
	}
}