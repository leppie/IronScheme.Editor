#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion


#region Includes
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Reflection;
using ST = System.Threading; // dont include, messes up timers, this doesnt work on pnet
using System.Diagnostics;
using IronScheme.Editor.ComponentModel;
using IronScheme.Editor.Controls;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;


using ToolStripMenuItem = IronScheme.Editor.Controls.ToolStripMenuItem;

using IronScheme.Editor.Runtime;

#endregion

namespace IronScheme.Editor.Configuration
{
  /// <summary>
  /// Start up parameter for xacc.ide
  /// </summary>
  public class IdeArgs : Utils.GetArgs
  {
    /// <summary>
    /// Starts IDE in listermode (maximized and close on ESC)
    /// </summary>
    public bool listermode = false;

    /// <summary>
    /// Starts IDE in debug mode
    /// </summary>
    public bool debug = 
#if DEBUG
      true
#else
      false
#endif
      ;

    public FormWindowState form = FormWindowState.Normal;

    /// <summary>
    /// List of files to open
    /// </summary>
    [Utils.DefaultArg(AllowName=true)]
    public string[] open;
  }

  public class ServerService : MarshalByRefObject
  {
    public void OpenFile(string filename)
    {
      ServiceHost.File.Open(filename);
    }

    public bool IsShuttingDown
    {
      get { return ServiceHost.isshuttingdown; }
    }
  }


	/// <summary>
	/// Support to bootstrap the IDE
	/// </summary>
	public class IdeSupport
	{
    static TextWriter tracelog;
    static IdeArgs args;

    internal static AboutForm about;

    static ST.Mutex real = null;

    static bool InvokeClient()
    {
      IpcClientChannel client = new IpcClientChannel();
      ChannelServices.RegisterChannel(client, false);

      RemotingConfiguration.RegisterWellKnownClientType(typeof(ServerService), "ipc://XACCIDE/ss");

      try
      {

        ServerService s = new ServerService();
        if (s.IsShuttingDown)
        {
          return false;
        }
        if (args.open != null)
        {
          foreach (string fn in args.open)
          {
            StringBuilder sb = new StringBuilder(256);
            int len = kernel32.GetLongPathName(fn, sb, 255);
            try
            {
              s.OpenFile(sb.ToString());
            }
            catch (Exception ex)
            {
              Trace.WriteLine("Could not load file: " + sb + @" Message:
" + ex);
            }

          }
          return true;
        }
        return false;
      }
      finally
      {
        ChannelServices.UnregisterChannel(client);
      }
    }


    /// <summary>
    /// Starts the IDE
    /// </summary>
    /// <param name="f">the hosting form</param>
    public static bool KickStart(Form f)
    {
      args = new IdeArgs();

      if (args.listermode)
      {
        ST.Mutex m = null;
        try
        {
          m = ST.Mutex.OpenExisting("XACCIDE");
        }
        catch
        {
          // must assign, else GC will clean up
          real = new System.Threading.Mutex(true, "XACCIDE");
        }

        if (m != null)
        {
          if (InvokeClient())
          {
            return false;
          }
          else
          {
            real.WaitOne();
          }
        }
      }

#if !DEBUG

      Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      try
      {
#endif
      //System.Threading.Thread.CurrentThread.CurrentCulture =
      //    System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        if (args.listermode)
        {
          IpcServerChannel server = new IpcServerChannel("XACCIDE");
          ChannelServices.RegisterChannel(server, false);

          RemotingConfiguration.RegisterWellKnownServiceType(typeof(ServerService), "ss", WellKnownObjectMode.Singleton);
        }

      Application.EnableVisualStyles();
      SettingsService.idemode = true;
      Application.ApplicationExit +=new EventHandler(AppExit);
      AppDomain.CurrentDomain.AssemblyResolve +=new ResolveEventHandler(CurrentDomain_AssemblyResolve);
      AppDomain.CurrentDomain.SetupInformation.LoaderOptimization = LoaderOptimization.MultiDomainHost;

      about = new AboutForm();
      about.StartPosition = f.StartPosition = FormStartPosition.CenterScreen;
      about.Show();
      Application.DoEvents();

      f.KeyPreview = true;
      
      IWindowService ws = new WindowService(f);

      about.progressBar1.Value += 10;

      Assembly thisass = typeof(IdeSupport).Assembly;

      new PluginManager().LoadAssembly(thisass);

      ((SettingsService) ServiceHost.Settings).args = args;

      IMenuService ms = ServiceHost.Menu;

      MenuStrip mm = ms.MainMenu;

      f.Font = SystemInformation.MenuFont;
      f.Closing +=new CancelEventHandler(f_Closing);
      f.AllowDrop = true;

      f.DragEnter+=new DragEventHandler(f_DragEnter);
      f.DragDrop+=new DragEventHandler(f_DragDrop);

      Version ver = typeof(IdeSupport).Assembly.GetName().Version;

      string verstr = ver.ToString(4);

      f.Text = "IronScheme.Editor";// "xacc.ide " + verstr + (args.debug ? " - DEBUG MODE" : string.Empty) ;
      f.Size = new Size(900, 650);

      ToolStripMenuItem view = ms["View"];

      f.Icon = new Icon(
        thisass.GetManifestResourceStream(
#if VS
        "IronScheme.Editor.Resources." + 
#endif
        "ironscheme.ico"));

      ServiceHost.Window.Document.AllowDrop = true;
      ServiceHost.Window.Document.DragEnter +=new DragEventHandler(f_DragEnter);
      ServiceHost.Window.Document.DragDrop +=new DragEventHandler(f_DragDrop);

      ServiceHost.Window.Document.BringToFront();
      var vs = new ViewService();
      about.progressBar1.Value += 5;
      new KeyboardHandler();

      ServiceHost.State = ApplicationState.Normal;

      about.progressBar1.Value += 5; //65


      //after everything has been loaded
      ServiceHost.Initialize();

      about.progressBar1.Value += 5;

      try
      {
        ServiceHost.Scripting.InitCommand();
        //ServiceHost.Shell.InitCommand();
      }
      catch (Exception ex) // MONO
      {
        Trace.WriteLine(ex);
      }

      if (args.open != null)
      {
        foreach (string of in args.open)
        {
          if (File.Exists(of))
          {
            StringBuilder sb = new StringBuilder(256);
            int len = kernel32.GetLongPathName(of, sb, 255);
            try
            {
              ServiceHost.File.Open(sb.ToString());
            }
            catch (Exception ex)
            {
              Trace.WriteLine("Could not load file: " + sb + @" Message:
" + ex);
            }
          }
          else
          {
            Trace.WriteLine("Could not open missing file: " + of);
          }
        }
      }
#if !DEBUG
      }
      catch (Exception ex)
      {
        HandleException(ex, true);
        return false;
      }
#endif
      f.WindowState = args.form;

      if (args.listermode)
      {
        f.WindowState = FormWindowState.Maximized;
      }

      about.Close();
      return true;
    }



		static void f_Closing(object sender, CancelEventArgs e)
		{
      ToolStripManager.SaveSettings(ServiceHost.Window.MainForm, "Toolbar");
      //ServiceHost.ToolBar.Save();

      IToolsService its = ServiceHost.Tools;
      if (its != null)
      {
        //(its as ToolsService).SaveView();
      }

      IProjectManagerService pms = ServiceHost.Project;
      if (pms != null)
      {
        pms.CloseAll();
      }

			IFileManagerService fms = ServiceHost.File;
			if (fms != null)
			{
        if (!fms.CloseAll())
        {
          e.Cancel = true;
          return;
        }
			}

      Application.Exit();
		}

		static void AppExit(object sender, EventArgs e)
		{
			((IDisposable) ServiceHost.INSTANCE).Dispose();
      if (tracelog != null)
      {
        Trace.Close();
      }
		}

    static void f_DragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        e.Effect = DragDropEffects.All;
      }
    }

    static void f_DragDrop(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] filenames = (e.Data.GetData(DataFormats.FileDrop) as string[]);
        foreach (string fn in filenames)
        {
          ServiceHost.File.Open(fn);
        }
      }
    }

    static string LINE = "================================================================================";
    static string EXCP = "Unhandled exception";

    static void HandleException(Exception ex, bool appstateinvalid)
    {
      Trace.WriteLine(LINE, EXCP);
      Trace.WriteLine(ex, EXCP);
      Trace.WriteLine(LINE, EXCP);

      if (!(ex is System.Threading.ThreadAbortException))
      {
        ExceptionForm exform = new ExceptionForm();
        exform.Exception = ex;
        exform.ApplicationStateInvalid = appstateinvalid;
        exform.ShowDialog(ServiceHost.Window.MainForm);
      }
    }

    static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
      HandleException(e.Exception, false);
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (!e.IsTerminating)
      {
        Exception ex = e.ExceptionObject as Exception;
        if (ex.Message != "Safe handle has been closed")
        {
          HandleException(ex, false);
        }
      }
    }

    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      if (args.Name.StartsWith("xacc"))
      {
        return typeof(IdeSupport).Assembly;
      }
      return null;
    }
  }
}

