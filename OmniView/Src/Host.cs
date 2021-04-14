using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OmniView
{
	class OmniViewHost : SciterHost
	{
		/*
		PROBLEM that happens:
		OmniCode normally is loaded before OmniView, so is sciter.dll, which might be outdated
		
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", SetLastError = true)]
		[PreserveSig]
		public static extern uint GetModuleFileName([In]IntPtr hModule, [Out]StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetDllDirectory(string lpPathName);

		static OmniViewHost()
		{
			var dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(OmniViewHost)).Location);
			SetDllDirectory(dir);

			var a = GetModuleHandle("sciter.dll");
			StringBuilder sb = new StringBuilder(2048);
			var p = GetModuleFileName(a, sb, 2048);
		}*/


		private static SciterX.ISciterAPI _api = SciterX.API;
		private static SciterArchive _archive;
		//private static SciterWindow _vm_wnd;// keep the same VM alive

		public static void Setup()
		{
			if(_archive == null)
			{
				_archive = new SciterArchive();
				_archive.Open(SciterAppResource.ArchiveResource.resources);
			}

			/*if(_vm_wnd == null)
			{
				_vm_wnd = new SciterWindow();
				_vm_wnd.CreateMainWindow(0, 0, SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_MAIN | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TITLEBAR | SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG);
			}*/
		}

		private SciterDOH _doh;
		public readonly SciterWindow _wnd;

		public OmniViewHost(SciterWindow wnd)
		{
			//Debug.Assert(wnd.VM == _vm_wnd.VM);

			SetupWindow(wnd._hwnd);

			_wnd = wnd;
			_doh = new SciterDOH(this);

			SciterValue sv = new SciterValue();
			sv["omniview"] = new SciterValue(true);
			_wnd.SetMediaVars(sv);

			AttachEvh(new OmniViewEVH(wnd));
		}

		public void SetupPage(string page_from_res_folder)
		{
			string url = "archive://app/" + page_from_res_folder;
			bool res = _wnd.LoadPage(url);
			Debug.Assert(res);
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("archive://app/"))
			{
				Debug.WriteLine("Load: " + sld.uri);

				// load resource from SciterArchive
				string path = sld.uri.Substring(14);
				byte[] data = _archive.Get(path);

				Debug.Assert(data != null);
				if(data != null)
					_api.SciterDataReady(_wnd._hwnd, sld.uri, data, (uint)data.Length);
			}
			return base.OnLoadData(sld);
		}
	}

	class OmniViewEVH : SciterEventHandler
	{
		private readonly KeystrokeThief _thief;
		private readonly SciterWindow _wnd;

		[DllImport("user32.dll")]
		private static extern IntPtr GetFocus();

		public OmniViewEVH(SciterWindow wnd)
		{
			_wnd = wnd;
			_thief = new KeystrokeThief();
		}

		protected override void Detached(SciterElement se)
		{
			if(_thief.IsStealing)
				_thief.StopStealing();
		}

		protected override void Subscription(SciterElement se, out SciterXBehaviors.EVENT_GROUPS event_groups)
		{
			event_groups = SciterXBehaviors.EVENT_GROUPS.HANDLE_FOCUS | SciterXBehaviors.EVENT_GROUPS.HANDLE_SCRIPTING_METHOD_CALL;
			event_groups = SciterXBehaviors.EVENT_GROUPS.HANDLE_ALL;
		}

		protected override bool OnFocus(SciterElement se, SciterXBehaviors.FOCUS_PARAMS prms)
		{
			bool hasFocus = GetFocus() == _wnd._hwnd;
			if(_thief.IsStealing)
			{
				if(!hasFocus)
					_thief.StopStealing();
			}
			else
			{
				if(hasFocus)
					_thief.StartStealing();
			}

			return false;
		}

		protected override bool OnScriptCall(SciterElement se, string name, SciterValue[] args, out SciterValue result)
		{
			result = null;
#if DEBUG
			if(name == "Host_Dbg1")
			{
				return true;
			}
			if(name == "Host_Dbg2")
			{
				return true;
			}
			if(name == "Host_Dbg3")
			{
				return true;
			}
#endif

			return false;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint GetModuleFileName([In] IntPtr hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);
	}

	class SciterDOH : SciterDebugOutputHandler
	{
		private OmniViewHost _host;
		private bool _in_post = false;
		private List<Tuple<string, uint>> _lines = new List<Tuple<string, uint>>();

		public SciterDOH(OmniViewHost host)
		//: base(host._wnd._hwnd)// FIX ME!!!!!!!!!!!!!!
		{
			Debug.Assert(host._wnd._hwnd != IntPtr.Zero);
			_host = host;
		}

		protected override void OnOutput(SciterXDef.OUTPUT_SUBSYTEM subsystem, SciterXDef.OUTPUT_SEVERITY severity, string text)
		{
			Debug.Write(text);

			_lines.Add(Tuple.Create(text, (uint)severity));
			if(_in_post)
				return;

			_in_post = true;

			_host.InvokePost(() =>
			{
				var list = _lines.Select(tp =>
				{
					var sv = new SciterValue();
					sv[0] = new SciterValue(tp.Item1);
					sv[1] = new SciterValue((int)tp.Item2);
					return sv;
				}).ToList();
				Debug.Assert(list.Count != 0);

				_host.CallFunction("View_AppendConsole", new SciterValue(list));
				_lines.Clear();
				_in_post = false;
			});
		}
	}
}