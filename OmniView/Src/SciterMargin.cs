using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace OmniView
{
	class SciterMargin : MarginBase
	{
		private static List<SciterMargin> _all_instances = new List<SciterMargin>();

		private SciterControlHost _sciter_ctrl;
		private ITextDocument Document;
		private bool _has_header = false;
		private bool _active = false;
		private bool _html = false;
		private bool _script = false;
		private bool _cfg_console = true;

		public SciterMargin(ITextView textView, ITextDocument document)
		{
			_all_instances.Add(this);

			textView.Properties.AddProperty(typeof(SciterMargin), this);
			textView.Closed += (s, e) =>
			{
				if(_sciter_ctrl != null)// previewer was not shown
					_sciter_ctrl.Destroy();
				_all_instances.Remove(this);
			};

			Document = document;
			document.FileActionOccurred += Doc_FileActionOccurred;

			Debug.Assert(document.FilePath != null, "PUTZ");
			if(document.FilePath.EndsWith(".tis"))
				_script = true;
			else
				_html = true;

			CheckUserEnabled();
		}

		public void CmdToggleView()
		{
			CheckUserEnabled();
			WriteToggleHeader(true, false);
		}

		public void CmdToggleConsole()
		{
			CheckUserEnabled();
			WriteToggleHeader(false, true);
		}

		public static void CmdSaveAll()
		{
			foreach(var item in _all_instances)
				item.ReloadControl();
		}

		// Internal ----------------------------------------------------------------
		private void CheckUserEnabled()
		{
			string header = Document.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText().Trim();

			if(header.Contains("OmniView:off"))
			{
				_has_header = true;
				_active = false;
			}
			else if(_html)
			{
				_has_header = header.StartsWith("<!--") && header.EndsWith("-->");
				_active = Path.GetFileName(Document.FilePath).Contains("unittest") || (_has_header && header.Contains("OmniView"));
			}
			else
			{
				_has_header = header.StartsWith("//") && (header.Contains("OmniView") || header.Contains("HtmlView"));
				_active = Path.GetFileName(Document.FilePath).Contains("unittest") || (_has_header && header.Contains("OmniView"));
			}

			if(_active)
			{
				_cfg_console = !header.Contains("console:off");
			}

			SetVisible(_active);
		}

		private void WriteToggleHeader(bool active, bool console)
		{
			var edit = Document.TextBuffer.CreateEdit();
			string linebreak = "\r\n";
			string html_view = string.Empty;

			if(_has_header)
			{
				var line = Document.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
				linebreak = line.GetLineBreakText();
				edit.Delete(line.ExtentIncludingLineBreak.Span);

				string linetxt = line.GetText();
				if(linetxt.Contains("HtmlView"))
				{
					html_view = linetxt.Contains("HtmlView:off") ? ", HtmlView:off" : ", HtmlView:on";
				}
			}

			if(active)
				_active = !_active;
			if(console)
			{
				_cfg_console = !_cfg_console;
				_sciter_ctrl._host.CallFunction("View_SetConsoleView", new SciterValue(_cfg_console));
			}

			string txt;
			if(_script)
				txt = "// OmniView:" + (_active ? "on" : "off") + (_cfg_console == false ? ", console:off" : string.Empty);
			else
				txt = "<!-- OmniView:" + (_active ? "on" : "off") + (_cfg_console == false ? ", console:off" : string.Empty) + html_view + " -->";
			edit.Insert(0, txt + linebreak);
			edit.Apply();

			CheckUserEnabled();
		}

		private void ReloadControl()
		{
			Utils.DebugOutputString("ReloadControl - " + Document.FilePath + " - " + _all_instances.Count + " - " + Utils.GetCallerName());

			if(_active)
			{
				_sciter_ctrl.Destroy();
				RecreatePreview();// will call OnControlCreated() after window created
			}
		}

		private void OnControlCreated()
		{
			Utils.Post(() =>
			{
				if(_active)
				{
#if DEBUG
					var ver1 = SciterX.API.SciterVersion(1);
					var ver2 = SciterX.API.SciterVersion(0);
#endif

					if(_html)
						_sciter_ctrl._host.CallFunction("View_LoadPage", new SciterValue("file://" + Document.FilePath));
					else
						_sciter_ctrl._host.CallFunction("View_LoadScript", new SciterValue(Document.TextBuffer.CurrentSnapshot.GetText()));

					_sciter_ctrl._host.CallFunction("View_SetConsoleView", new SciterValue(_cfg_console));
				}
				else if(_sciter_ctrl != null && _sciter_ctrl._host != null)
				{
					_sciter_ctrl._host.CallFunction("View_UnloadPage");
				}
			});
		}

		private void Doc_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
		{
			switch(e.FileActionType)
			{
				case FileActionTypes.ContentSavedToDisk:
					CheckUserEnabled();
					ReloadControl();
					break;

				case FileActionTypes.DocumentRenamed:
					ReloadControl();
					break;

				case FileActionTypes.ContentLoadedFromDisk:
					// user edited the file using another editor
					CheckUserEnabled();
					ReloadControl();
					break;
			}
		}

		protected override FrameworkElement CreatePreviewControl()
		{
			_sciter_ctrl = new SciterControlHost();
			_sciter_ctrl.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			_sciter_ctrl.VerticalAlignment = VerticalAlignment.Stretch;
			_sciter_ctrl.OnCreated += (o, s) => OnControlCreated();
			return _sciter_ctrl;
		}

		private class SciterControlHost : HwndHost
		{
			public SciterWindow _wnd;
			public OmniViewHost _host;
			public event EventHandler OnCreated;

			public void Destroy()
			{
				if(_wnd != null)
				{
					_wnd.Destroy();
					_wnd = null;
					_host = null;
				}
			}

			protected override HandleRef BuildWindowCore(HandleRef hwndParent)
			{
				Debug.Assert(_wnd == null);

				OmniViewHost.Setup();// ensures creating the dummy window before the child one
				_wnd = new SciterWindow();
				_wnd.CreateChildWindow(hwndParent.Handle, SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_CHILD | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_OWNS_VM | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG);
				_wnd.ModifyStyleEx(PInvokeWindows.SetWindowLongFlags.WS_EX_CLIENTEDGE, 0);
				_wnd.SetSciterOption(SciterXDef.SCITER_RT_OPTIONS.SCITER_SET_DEBUG_MODE, new IntPtr(1));// SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG don't work
				Utils.DebugOutputString("BuildWindowCore - " + _wnd._hwnd.ToString());

				_host = new OmniViewHost(_wnd);
				_host.SetupPage("host.html");

				if(OnCreated != null)
					OnCreated(null, null);

				return new HandleRef(this, _wnd._hwnd);
			}

			protected override void DestroyWindowCore(HandleRef hwnd)
			{
				// called only on GC
				Debug.Assert(_wnd == null, "MOOO3");
			}
		}
	}

	abstract class MarginBase : DockPanel, IWpfTextViewMargin
	{
		private Grid _grid;
		private FrameworkElement _previewControl;
		private bool _created = false;
		private bool _isDisposed = false;

		/*protected MarginBase()
		{
			//CreateMarginControls();
			//Dispatcher.CurrentDispatcher.BeginInvoke(new Action(CreateMarginControls), DispatcherPriority.ApplicationIdle, null);// why WebEssentials used this?
		}*/

		protected void SetVisible(bool show)
		{
			if(show)
				CreateMarginControls();
			Visibility = show ? Visibility.Visible : Visibility.Collapsed;
		}

		protected void RecreatePreview()
		{
			Debug.Assert(_created);
			_grid.Children.Remove(_previewControl);

			_previewControl = CreatePreviewControl();
			Debug.Assert(_previewControl != null);
			_grid.Children.Add(_previewControl);

			Grid.SetColumn(_previewControl, 2);
			Grid.SetRow(_previewControl, 0);
		}

		protected abstract FrameworkElement CreatePreviewControl();

		private void CreateMarginControls()
		{
			if(_created)
				return;
			_created = true;

			const int WIDTH = 400;

			_grid = new Grid();
			_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
			_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
			_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(WIDTH, GridUnitType.Pixel) });
			_grid.RowDefinitions.Add(new RowDefinition());

			_previewControl = CreatePreviewControl();
			Debug.Assert(_previewControl != null);
			_grid.Children.Add(_previewControl);

			Grid.SetColumn(_previewControl, 2);
			Grid.SetRow(_previewControl, 0);

			GridSplitter splitter = new GridSplitter();
			splitter.Width = 5;
			splitter.ResizeDirection = GridResizeDirection.Columns;
			splitter.VerticalAlignment = VerticalAlignment.Stretch;
			splitter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			//splitter.DragCompleted += splitter_DragCompleted;

			_grid.Children.Add(splitter);
			Grid.SetColumn(splitter, 1);
			Grid.SetRow(splitter, 0);

			Children.Add(_grid);
		}

		private void ThrowIfDisposed()
		{
			if(_isDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		#region IWpfTextViewMargin Members

		public FrameworkElement VisualElement { get { return this; } }

		#endregion

		#region ITextViewMargin Members

		public double MarginSize
		{
			// Since this is a horizontal margin, its width will be bound to the width of the text view.
			// Therefore, its size is its height.
			get
			{
				ThrowIfDisposed();
				return ActualHeight;
			}
		}

		public bool Enabled { get { ThrowIfDisposed(); return true; } }

		public ITextViewMargin GetTextViewMargin(string marginName)
		{
			ThrowIfDisposed();
			return (marginName == GetType().Name) ? this : null;
		}


		///<summary>Releases all resources used by the MarginBase.</summary>
		public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

		///<summary>Releases the unmanaged resources used by the MarginBase and optionally releases the managed resources.</summary>
		///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
			}
		}
		#endregion
	}
}