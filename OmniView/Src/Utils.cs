using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
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
	static class Utils
	{
		public const string VSIX_NAME = "OmniView";

		static Utils()
		{
			DTE = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
		}

		public static readonly DTE2 DTE;

		public static ITextView GetActiveTextView()
		{
			if(DTE.ActiveDocument.ActiveWindow != DTE.ActiveWindow)
				return null;

			IServiceProvider sp = ServiceProvider.GlobalProvider;
			IVsTextManager mngr = sp.GetService(typeof(VsTextManagerClass)) as IVsTextManager;
			IVsTextView textViewActive;

			int res = mngr.GetActiveView(0, null, out textViewActive);
			if(textViewActive == null)
				return null;

			IComponentModel container = sp.GetService(typeof(SComponentModel)) as IComponentModel;
			IVsEditorAdaptersFactoryService adapter = container.DefaultExportProvider.GetExport<IVsEditorAdaptersFactoryService>().Value;
			return adapter.GetWpfTextView(textViewActive);
		}

		public static void Post(Action f)
		{
			Dispatcher.CurrentDispatcher.InvokeAsync(() =>
			{
				f();
			}, DispatcherPriority.Normal);
		}

		[Conditional("DEBUG")]
		public static void DebugOutputString(string output)
		{
#if DEBUG
			ReleaseOutputString(output);
#endif
		}

		public static void ReleaseOutputString(string output)
		{
			try
			{
				Debug.WriteLine(output);

				if(DTE == null || DTE.Windows.Count == 0)
					return;

				const string OUTPUT_WINDOW_NAME = "General";
				Window window = DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
				OutputWindow outputWindow = (OutputWindow)window.Object;
				OutputWindowPane outputWindowPane = null;

				for(uint i = 1; i <= outputWindow.OutputWindowPanes.Count; i++)
				{
					if(outputWindow.OutputWindowPanes.Item(i).Name.Equals(OUTPUT_WINDOW_NAME, StringComparison.CurrentCultureIgnoreCase))
					{
						outputWindowPane = outputWindow.OutputWindowPanes.Item(i);
						break;
					}
				}

				if(outputWindowPane == null)
					outputWindowPane = outputWindow.OutputWindowPanes.Add(OUTPUT_WINDOW_NAME);

				outputWindowPane.Activate();
				outputWindowPane.OutputString("[" + VSIX_NAME + "] " + output + "\n");

				//Microsoft.VisualStudio.Shell.ActivityLog.LogWarning(VSIX_NAME, "[" + VSIX_NAME + "] " + output);
			}
			catch(Exception)
			{
			}
		}

		public static string GetCallerName()
		{
			StackTrace stackTrace = new StackTrace();
			return stackTrace.GetFrame(2).GetMethod().Name + "()";
		}
	}
}