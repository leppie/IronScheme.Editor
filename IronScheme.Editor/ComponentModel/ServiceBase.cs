#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion



using System;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

using System.Text.RegularExpressions;
using System.Runtime.Remoting;

using RealTrace = IronScheme.Editor.Diagnostics.Trace;
using ToolStripMenuItem = IronScheme.Editor.Controls.ToolStripMenuItem;

namespace IronScheme.Editor.ComponentModel
{

  /// <summary>
  /// Provides an abstract base class for Service implementations.
  /// </summary>
  [Image("Service.Default.png")]
  [LicenseProvider(typeof(LicFileLicenseProvider))]
  public abstract class ServiceBase : RemoteDisposable, IService, IComponent, ISynchronizeInvoke, ISupportInitialize, System.IServiceProvider
  {
    readonly License license = null;
    ToolStripMenuItem toplevel;
    Hashtable attrmap;
   
    readonly string propname;
    readonly ServiceTrace trace;
    readonly Hashtable submenus = new Hashtable();

    //readonly Hashtable contents = new Hashtable();

    /// <summary>
    /// Gets the property name if the service if available.
    /// </summary>
    public string PropertyName
    {
      get {return propname;}
    }

    ISite IComponent.Site
    {
      get {return ServiceHost.INSTANCE; }
      set {;}
    }

    #region Tracing
    /// <summary>
    /// Context bound trace class
    /// </summary>
    protected sealed class ServiceTrace
    {
      readonly ServiceBase ctr;

      /// <summary>
      /// Creates an instance of ServiceTrace
      /// </summary>
      /// <param name="ctr">the service container</param>
      public ServiceTrace(ServiceBase ctr)
      {
        this.ctr = ctr;
      }

      /// <summary>
      /// Write a line to the trace listeners
      /// </summary>
      /// <param name="format">the string format</param>
      /// <param name="args">the string format parameters</param>
      [Conditional("TRACE")]
      public void WriteLine(string format, params object[] args)
      {
        RealTrace.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " " + ctr.Name, format, args);
      }

      /// <summary>
      /// Write a line to the trace listeners
      /// </summary>
      /// <param name="value">the value</param>
      [Conditional("TRACE")]
      public void WriteLine(object value)
      {
        RealTrace.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " " + ctr.Name, value.ToString());
      }

      /// <summary>
      /// Asserts the specified condition.
      /// </summary>
      /// <param name="condition">if set to <c>true</c> [condition].</param>
      /// <param name="message">The message.</param>
      [Conditional("TRACE")]
      public void Assert(bool condition, string message)
      {
        if (!condition)
        {
          RealTrace.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " " + ctr.Name, "Assert failed: {0}", message);
        }
      }
    }

    /// <summary>
    /// Gets context bound trace
    /// </summary>
    protected ServiceTrace Trace
    {
      get { return trace; }
    }

    protected sealed class StatusTrace
    {
      public void Write(string format, params object[] args)
      {
        ServiceHost.StatusBar.StatusText = string.Format(format, args);
      }
    }

    readonly StatusTrace statustrace = new StatusTrace();

    protected StatusTrace Status
    {
      get { return statustrace; }
    }



    #endregion

    /// <summary>
    /// Checks if service has a toplevel menu
    /// </summary>
    public bool HasMenu
    {
      get {return toplevel != null;}
    }

    /// <summary>
    /// Gets the name of the service
    /// </summary>
    public string Name
    {
      get { return NameAttribute.GetName(GetType()); }
    }

    /// <summary>
    /// Gets the toplevelmenu
    /// </summary>
    protected ToolStripMenuItem TopLevelMenu
    {
      get { return toplevel; }
    }

    /// <summary>
    /// Called when object is disposed
    /// </summary>
    /// <param name="disposing">true is Dispose() was called</param>
    protected override void Dispose(bool disposing)
    {

      Trace.WriteLine("Dispose({0})", disposing.ToString().ToLower());
      if(disposing)
      {
        if (license != null) 
        {
          license.Dispose();
        }

        if (remoteobject != null)
        {
          RemotingServices.Unmarshal(remoteobject);
        }

      }
    }


    /// <summary>
    /// Creates an instance of a service
    /// </summary>
    protected ServiceBase() : this(null)
    {
    }

    /// <summary>
    /// Creates an instance of a service
    /// </summary>
    /// <param name="t">The type of the service to register</param>
    ServiceBase(Type t)
    {
      trace = new ServiceTrace(this);
      Type tt = GetType();
      try
      {
        if ( t == null)
        {
          foreach(Type it in tt.GetInterfaces())
          {
            if (it != typeof(IService))
            {
              if (typeof(IService).IsAssignableFrom(it))
              {
                t = it;
                break;
              }
            }
          }
        }

        if (t == null)
        {
          throw new ArgumentException("No service interfaces has been defined", "t");
        }

        license = LicenseManager.Validate(t, this);
        //if (license == null) //no license...

        propname = ServiceHost.GetPropertyName(t);
        ServiceHost.INSTANCE.Add(t, this);

        remoteobject = RemotingServices.Marshal(this, null, t);

      }
      catch (Exception ex)
      {
        Trace.WriteLine("{1}() : {0}", ex, tt);
      }
    }

    /// <summary>
    /// Gets called after all processes has been loaded
    /// </summary>
    [SuppressMenu]
    protected virtual void Initialize()
    {

    }

    /// <summary>
    /// Gets or sets the menu state, if used
    /// </summary>
    internal ApplicationState MenuState
    {
      get {return ServiceHost.State; }
    }


    #region ISynchronizeInvoke Members

    ///<include file='C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.Windows.Forms.xml' 
    ///	path='doc/members/member[@name="M:System.Windows.Forms.Control.EndInvoke(System.IAsyncResult)"]/*'/>
    public object EndInvoke(IAsyncResult asyncResult)
    {
      return ServiceHost.Window.MainForm.EndInvoke(asyncResult);
    }

    ///<include file='C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.Windows.Forms.xml' 
    ///	path='doc/members/member[@name="M:System.Windows.Forms.Control.Invoke(System.Delegate, System.Object[])"]/*'/>
    public object Invoke(Delegate method, object[] args)
    {
      return ServiceHost.Window.MainForm.Invoke(method, args);
    }

    ///<include file='C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.Windows.Forms.xml' 
    ///	path='doc/members/member[@name="P:System.Windows.Forms.Control.InvokeRequired"]/*'/>
    [SuppressMenu]
    public bool InvokeRequired
    {
      get { return ServiceHost.Window.MainForm.InvokeRequired; }
    }

    ///<include file='C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.Windows.Forms.xml' 
    ///	path='doc/members/member[@name="M:System.Windows.Forms.Control.BeginInvoke(System.Delegate, System.Object[])"]/*'/>
    public IAsyncResult BeginInvoke(Delegate method, object[] args)
    {
      return ServiceHost.Window.MainForm.BeginInvoke(method, args);
    }

    #endregion

    #region ISupportInitialize Members

    readonly static Regex PPTEXT = new Regex(@"\{(?<name>[^\}\s]+)\}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    string GetText(string input, out bool haderror)
    {
      haderror = false;
      Hashtable map = new Hashtable();
      foreach (Match m in PPTEXT.Matches(input))
      {
        string name = m.Groups["name"].Value;

        string value = GetType().GetProperty(name, 
          BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this, null) as string;

        if (value == null)
        {
          value = "<null>";
          haderror = true;
        }

        map.Add("{" + name + "}", value);
      }

      foreach (string k in map.Keys)
      {
        input = input.Replace(k, map[k] as string);
      }

      return input;
    }

    void ISupportInitialize.BeginInit()
    {
      const BindingFlags BF = BindingFlags.Public | BindingFlags.DeclaredOnly 
              | BindingFlags.NonPublic | BindingFlags.Instance;

      Type tt = GetType();
      MenuService ms = ServiceHost.Menu as MenuService;

      if (Attribute.IsDefined(tt, typeof(MenuAttribute), false))
      {
        MenuAttribute mat = tt.GetCustomAttributes(typeof(MenuAttribute), false)[0] as MenuAttribute;

        toplevel = ms[mat.Text];

        if (toplevel == null)
        {
          toplevel = new ToolStripMenuItem(mat.Text);
          ms.AddTopLevel(toplevel);
        }

        attrmap = ms.GetAttributeMap(toplevel);

        EventHandler ev = new EventHandler(DefaultHandler); 

        ArrayList submenus = new ArrayList();

        foreach (MethodInfo mi in tt.GetMethods(BF))
        {
          if (mi.GetParameters().Length == 0)
          {
            bool hasat = Attribute.IsDefined(mi, typeof(MenuItemAttribute));

            if (mi.ReturnType == typeof(void) && (!mi.IsPrivate || hasat) 
              && !Attribute.IsDefined(mi, typeof(SuppressMenuAttribute), true))
            {
              (ServiceHost.Keyboard as KeyboardHandler).AddTarget(this, mi);
            }
            if (hasat)
            {
              MenuItemAttribute mia = mi.GetCustomAttributes(typeof(MenuItemAttribute), false)[0] as MenuItemAttribute;
              mia.invoke = mi;
              mia.ctr = this;
              submenus.Add(mia);
            }
          }
        }

        foreach (PropertyInfo pi in tt.GetProperties(BF))
        {
          if (pi.PropertyType == typeof(bool) && pi.CanRead && pi.CanWrite)
          {
            bool hasat = Attribute.IsDefined(pi, typeof(MenuItemAttribute));

            if (!Attribute.IsDefined(pi, typeof(SuppressMenuAttribute), true))
            {
              (ServiceHost.Keyboard as KeyboardHandler).AddToggle(this, pi);
            }

            if (hasat)
            {
              MenuItemAttribute mia = pi.GetCustomAttributes(typeof(MenuItemAttribute), false)[0] as MenuItemAttribute;
              mia.invoke = pi;
              mia.istogglemenu = true;
              mia.ctr = this;
              submenus.Add(mia);
            }
          }
          else if (pi.PropertyType == typeof(string) && pi.CanRead && pi.CanWrite)
          {
            if (Attribute.IsDefined(pi, typeof(MenuItemAttribute)))
            {
              MenuItemAttribute mia = pi.GetCustomAttributes(typeof(MenuItemAttribute), false)[0] as MenuItemAttribute;
              mia.invoke = pi;
              mia.ctr = this;
              submenus.Add(mia);
            }
          }
        }

        foreach (ToolStripMenuItem mi in toplevel.DropDownItems)
        {
          object mia = attrmap[mi];
          if (mia != null)
          {
            submenus.Add(mia);
          }
        }

        submenus.Sort();
        int previdx = -1;

        int counter = 0;

        ToolBarService tbs = ServiceHost.ToolBar as ToolBarService;

        foreach (MenuItemAttribute mia in submenus)
        {
          ToolStripMenuItem pmi = null;
          if (mia.mi == null)
          {
            if (mia.Converter == null)
            {
              pmi = new ToolStripMenuItem(mia.Text);
              pmi.Click += ev;
            }
            else
            {
              pmi = new ToolStripMenuItem(mia.Text);

              PropertyInfo pi = mia.invoke as PropertyInfo;

              string v = pi.GetValue(this, null) as string;

              pmi.DropDownOpening += new EventHandler(pmi_DropDownOpening);
              TypeConverter tc = Activator.CreateInstance(mia.Converter) as TypeConverter;

              foreach (string file in tc.GetStandardValues())
              {
                ToolStripMenuItem smi = new ToolStripMenuItem(file);
                pmi.DropDownItems.Add(smi);

                if (file == v)
                {
                  smi.Checked = true;
                }

                smi.Click += new EventHandler(pmi_Click);
              }
            }
          }
          else
          {
            pmi = mia.mi as ToolStripMenuItem;
          }

          if (mia.istogglemenu)
          {
            PropertyInfo pi = mia.invoke as PropertyInfo;
            try
            {
              bool v = (bool) pi.GetValue(this, new object[0]);
              pmi.Checked = v;
            }
            catch
            {
              //something not ready, sorts itself out
              Debugger.Break();
            }
          }
        
          if (previdx != -1 && mia.Index > previdx + 1)
          {
            toplevel.DropDownItems.Add("-");
            counter++;
          }
          int imgidx = -1;
          if (mia.Image != null)
          {
            pmi.Tag = mia.Image;
            imgidx = ServiceHost.ImageListProvider[mia.Image];
          }
          mia.mi = pmi;

          ToolStripMenuItem top = toplevel;

          string miaText = mia.Text;

          //if (miaText.IndexOf(':') > 0)
          //{
          //  string[] quals = miaText.Split(':');

          //  if (quals.Length > 0)
          //  {
          //    top = ServiceHost.Menu[quals[0]];
          //    miaText = miaText.Replace(quals[0] + ":", string.Empty);
          //  }
          //}

          // check text
          string[] tokens = miaText.Split('\\');
          if (tokens.Length > 1)
          {

            ToolStripMenuItem sub = this.submenus[tokens[0]] as ToolStripMenuItem;
            if (sub == null)
            {
              this.submenus[tokens[0]] = sub = new ToolStripMenuItem(tokens[0]);
              top.DropDownItems.Add(sub);
            }
            top = sub;

            pmi.Text = tokens[1];
            top.DropDownItems.Add(pmi);
            counter--;
          }
          else
          {
            string miatext = miaText;
            top.DropDownOpening += delegate(object sender, EventArgs e)
            {
              bool haderror;
              string t =  GetText(miatext, out haderror);
              if (haderror)
              {
                pmi.Enabled = false;
              }
              pmi.Text = t;
            };
            top.DropDownItems.Add(pmi);
          }

          attrmap[pmi] = mia;
          
          if (SettingsService.idemode)
          {
            if (mia.AllowToolBar)
            {
              tbs.Add(toplevel, mia);
            }
          }

          previdx = mia.Index;
          counter++;
        }
      }

      Initialize();

    }

    void pmi_Click(object sender, EventArgs e)
    {
      ToolStripMenuItem pmi = sender as ToolStripMenuItem;

      string v = pmi.Text;

      MenuItemAttribute mia = attrmap[((ToolStripMenuItem) pmi.OwnerItem).clonedfrom] as MenuItemAttribute;

      PropertyInfo pi = mia.invoke as PropertyInfo;

      try
      {
        pi.SetValue(this, v, null);
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex);
      }
    }

    void pmi_DropDownOpening(object sender, EventArgs e)
    {
      ToolStripMenuItem pmi = sender as ToolStripMenuItem;
      pmi.DropDownItems.Clear();

      MenuItemAttribute mia = attrmap[pmi.clonedfrom] as MenuItemAttribute; // eek??

      PropertyInfo pi = mia.invoke as PropertyInfo;

      string v = pi.GetValue(this, null) as string;

      TypeConverter tc = Activator.CreateInstance(mia.Converter) as TypeConverter;

      foreach (string file in tc.GetStandardValues())
      {
        ToolStripMenuItem smi = new ToolStripMenuItem(file);
        pmi.DropDownItems.Add(smi);

        smi.Enabled = pmi.Enabled;

        if (file == v)
        {
          smi.Checked = true;
        }

        smi.Click+=new EventHandler(pmi_Click);
      }
    }

    internal void DefaultHandler(object sender, EventArgs e)
    {
      if (InvokeRequired)
      {
        Invoke( new EventHandler(DefaultHandler), new object[] {sender, e});
        return;
      }
      try
      {
        ToolStripMenuItem menu = sender as ToolStripMenuItem;
        MenuItemAttribute mia = attrmap[menu] as MenuItemAttribute;
      
        if (mia == null)
        {
          menu = menu.clonedfrom;
          mia = attrmap[menu] as MenuItemAttribute;
        }

        if (mia.istogglemenu)
        {
          PropertyInfo pi = mia.invoke as PropertyInfo;
          menu.Checked = !menu.Checked;
          pi.SetValue(this, menu.Checked, new object[0]);
        }
        else
        {
          MethodInfo mi = mia.invoke as MethodInfo;
          mi.Invoke(this, new object[0]);
        }
      }
      catch (Exception ex)
      {
#if DEBUG
        Trace.WriteLine(ex);
#else
        throw ex;
#endif
      }
    }

    ObjRef remoteobject;

    void ISupportInitialize.EndInit()
    {
      
    }

    #endregion

    #region IServiceProvider Members

    object System.IServiceProvider.GetService(Type serviceType)
    {
      return ServiceHost.INSTANCE.GetService(serviceType);
    }

    #endregion
  }
}
