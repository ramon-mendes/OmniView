using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace OmniView.Cmd
{
	internal sealed class CmdToggleOmniView
	{
		private readonly Package package;
		private IServiceProvider ServiceProvider { get { return this.package; } }
		public static CmdToggleOmniView Instance { get; private set; }

		private CmdToggleOmniView(Package package)
		{
			this.package = package;

			OleMenuCommandService mcs = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if(mcs != null)
			{
				CommandID menuCommandID = new CommandID(PackageGuids.GUID_CmdOmniView, PackageIds.ID_CmdToggleOmniView);

				OleMenuCommand menuItem = new OleMenuCommand((s, e) =>
				{
					var view = Utils.GetActiveTextView();
					if(view != null && view.Properties.ContainsProperty(typeof(SciterMargin)))
					{
						var margin = view.Properties.GetProperty<SciterMargin>(typeof(SciterMargin));
						margin.CmdToggleView();
					}
				}, menuCommandID);

				menuItem.BeforeQueryStatus += (sender, evt) =>
				{
					OleMenuCommand item = (OleMenuCommand)sender;
					item.Visible = false;
				};

				mcs.AddCommand(menuItem);
			}
		}

		public static void Initialize(Package package)
		{
			Instance = new CmdToggleOmniView(package);
		}
	}
}