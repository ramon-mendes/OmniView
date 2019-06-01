using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using OmniView.Cmd;

namespace OmniView
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)] // Info on this package for Help/About
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
	[Guid(PackageGuids.GUID_OmniViewPackageString)]
	public sealed class VSPackage : Package
	{
		public VSPackage()
		{
		}

		protected override void Initialize()
		{
			CmdToggleOmniView.Initialize(this);
			CmdToggleConsole.Initialize(this);
			base.Initialize();

			IVsRegisterPriorityCommandTarget ip = (IVsRegisterPriorityCommandTarget)GetService(typeof(SVsRegisterPriorityCommandTarget));
			uint cookie;
			ip.RegisterPriorityCommandTarget(0, new CommandTargetGlobal(), out cookie);
		}
	}

	class CommandTargetGlobal : IOleCommandTarget
	{
		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if(pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
			{
				if(nCmdID == (uint)VSConstants.VSStd97CmdID.SaveSolution)
					Utils.Post(SciterMargin.CmdSaveAll);
			}
			return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.MSOCMDERR_E_FIRST);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
		}
	}
}
