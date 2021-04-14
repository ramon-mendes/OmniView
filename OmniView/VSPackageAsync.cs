using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using OmniView._2019;
using OmniView.Cmd;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace OmniView
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(PackageGuids.GUID_OmniViewPackageString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)] // Info on this package for Help/About
	public sealed class VSPackageAsync : AsyncPackage
	{
		/// <summary>
		/// VSPackageAsync GUID string.
		/// </summary>
		public const string PackageGuidString = "93b30e8d-b5da-41d7-b1ce-42c4afb68840";

		/// <summary>
		/// Initializes a new instance of the <see cref="VSPackageAsync"/> class.
		/// </summary>
		public VSPackageAsync()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			CmdToggleOmniView.Initialize(this);
			CmdToggleConsole.Initialize(this);
			base.Initialize();

			IVsRegisterPriorityCommandTarget ip = (IVsRegisterPriorityCommandTarget)GetService(typeof(SVsRegisterPriorityCommandTarget));
			uint cookie;
			ip.RegisterPriorityCommandTarget(0, new CommandTargetGlobal(), out cookie);
		    //await ToolWindow1Command.InitializeAsync(this);
		}

		#endregion

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
}
