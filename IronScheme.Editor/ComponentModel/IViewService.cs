#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion

using System;
using System.Collections;
using System.Windows.Forms;
using IronScheme.Editor.Runtime;

namespace IronScheme.Editor.ComponentModel
{
	/// <summary>
	/// Provides services for managing views
	/// </summary>
	public interface IViewService : IService
	{
    /// <summary>
    /// Gets the outline panel
    /// </summary>
    Panel OutlinePanel {get;}

    /// <summary>
    /// Whether to show toolbar
    /// </summary>
    bool ShowToolbar {get;set;}

    string CurrentView { get; set; }
    bool ShowCommand { get; set; }
    bool ShowConsole { get; set; }
    bool ShowFileExplorer { get; set; }
    bool ShowOutline { get; set; }
    bool ShowProjectExplorer { get; set; }
    bool ShowProperties { get; set; }
    bool ShowResults { get; set; }

    void NextView();
  }

  [Menu("View")]
  sealed class ViewService : ServiceBase, IViewService
  {
    readonly Panel outlinepanel = new Panel();

    public Panel OutlinePanel
    {
      get {return outlinepanel;}
    }
    
    [MenuItem("Toolbar", Index = 0, Image = "View.Toolbar.png")]
    public bool ShowToolbar
    {
      get
      {
        return ServiceHost.ToolBar.ToolBarVisible;
      }
      set
      {
        //if (value) ToolStripManager.LoadSettings(ServiceHost.Window.MainForm, "Toolbar");
        ServiceHost.ToolBar.ToolBarVisible = value;

      }
    }

    class ViewConverter : MenuDescriptor
    {
      public override ICollection GetValues()
      {
        Document c = ServiceHost.File.CurrentDocument as Document;
        ArrayList vals = new ArrayList();
        if (c != null && c.Views != null)
        {
          foreach (IDocument v in c.Views)
          {
            NameAttribute na = Attribute.GetCustomAttribute(v.GetType(), typeof(NameAttribute)) as NameAttribute;
            if (na != null)
            {
              vals.Add(na.Name);
            }
            else
            {
              vals.Add( v.GetType().Name);
            }
          }
        }
        return vals;
      }
    }

    public void NextView()
    {
      Document c = ServiceHost.File.CurrentDocument;
      if (c != null && c.Views != null && c.Views.Length > 1)
      {
        Control found = null, newview = null;

        foreach (Control v in c.Views)
        {
          if (v == c.ActiveControl)
          {
            found = v;
            continue;
          }
          if (found != null)
          {
            newview = v;
            break;
          }
        }

        if (found != null)
        {
          if (newview == null)
          {
            newview = c.Views[0] as Control;
          }
          IDockContent dc = found.Tag as IDockContent;
          dc.Controls.Remove(found);
          newview.Dock = DockStyle.Fill;
          newview.Tag = dc;
          dc.Controls.Add(newview);
          c.SwitchView(newview as IDocument);
          return;
        }
      }
    }

    [MenuItem("Switch View", Index = 5, Converter = typeof(ViewConverter), State = ApplicationState.Document)]
    public string CurrentView
    {
      get
      {
        Control c = ServiceHost.File.CurrentControl;
        if (c != null)
        {
          NameAttribute na = Attribute.GetCustomAttribute(c.GetType(), typeof(NameAttribute)) as NameAttribute;
          if (na != null)
          {
            return na.Name;
          }
          return c.GetType().Name;
        }
        return null;
      }
      set
      {
        Document c = ServiceHost.File.CurrentDocument;
        if (c != null && c.Views != null && c.Views.Length > 1)
        {
          foreach (Control v in c.Views)
          {
            NameAttribute na = Attribute.GetCustomAttribute(v.GetType(), typeof(NameAttribute)) as NameAttribute;
            if ((na != null && na.Name == value) || (v.GetType().Name == value))
            {
              IDockContent dc = c.ActiveView.Tag as IDockContent;
              dc.Controls.Remove(c.ActiveView as Control);
              v.Dock = DockStyle.Fill;
              v.Tag = dc;
              dc.Controls.Add(v);
              c.SwitchView(v as IDocument);
              return;
            }
          }
        }
      }
    }

    // eish this code is so old and ugly :(

    [MenuItem("Project Explorer", Index = 10, Image="Project.Type.png")]
    public bool ShowProjectExplorer
    {
      get 
      { 
        IDockContent dc = ServiceHost.Project.ProjectTab;
        return dc.DockState != DockState.Hidden;
      }
      set 
      { 
        if (!ShowProjectExplorer)
        {
          ServiceHost.Project.ProjectTab.Activate();
        }
        else
        {
          ServiceHost.Project.ProjectTab.Hide();
        }
      }
    }

    [MenuItem("File Explorer", Index = 11, Image = "Project.Type.png")]
    public bool ShowFileExplorer
    {
      get
      {
        IDockContent dc = ServiceHost.File.FileTab;
        return dc.DockState != DockState.Hidden;
      }
      set
      {
        if (!ShowProjectExplorer)
        {
          ServiceHost.File.FileTab.Activate();
        }
        else
        {
          ServiceHost.File.FileTab.Hide();
        }
      }
    }

    [MenuItem("Outline", Index = 12, Image="View.Outline.png")]
    public bool ShowOutline
    {
      get 
      { 
        IDockContent dc = ServiceHost.Project.OutlineTab;
        return dc.DockState != DockState.Hidden;
      }
      set 
      { 
        if (!ShowOutline)
        {
          ServiceHost.Project.OutlineTab.Activate();
        }
        else
        {
          ServiceHost.Project.OutlineTab.Hide();
        }
      }
    }

    [MenuItem("Results", Index = 20, Image= "View.Results.png")]
    public bool ShowResults
    {
      get 
      { 
        IDockContent dc = (ServiceHost.Error as ErrorService).tbp;
        return dc.DockState != DockState.Hidden;
      }
      set 
      { 
        if (!ShowResults)
        {
          (ServiceHost.Error as ErrorService).tbp.Activate();
        }
        else
        {
          (ServiceHost.Error as ErrorService).tbp.Hide();
        }
      }
    }

    [MenuItem("Output", Index = 21, Image="View.Output.png")]
    public bool ShowConsole
    {
      get 
      { 
        IDockContent dc = (ServiceHost.Console as StandardConsole).tbp;
        return dc.DockState != DockState.Hidden;
      }
      set 
      { 
        if (!ShowConsole)
        {
          (ServiceHost.Console as StandardConsole).tbp.Activate();
        }
        else
        {
          (ServiceHost.Console as StandardConsole).tbp.Hide();
        }
      }
    }

    [MenuItem("REPL", Index = 22, Image= "IronScheme.png")]
    public bool ShowCommand
    {
      get 
      { 
        IDockContent dc = (ServiceHost.Scripting as ScriptingService).tbp;
        if (dc == null)
        {
          return false;
        }
        return dc.DockState != DockState.Hidden;
      }
      set 
      { 
        if (!ShowCommand)
        {
          (ServiceHost.Scripting as ScriptingService).tbp.Activate();
        }
        else
        {
          (ServiceHost.Scripting as ScriptingService).tbp.Hide();
        }
      }
    }

    [MenuItem("Properties", Index = 30, Image = "View.Properties.png")]
    public bool ShowProperties
    {
      get
      {
        IDockContent dc = (ServiceHost.Property as PropertyService).tbp;
        return dc.DockState != DockState.Hidden;
      }
      set
      {
        if (!ShowCommand)
        {
          (ServiceHost.Property as PropertyService).tbp.Activate();
        }
        else
        {
          (ServiceHost.Property as PropertyService).tbp.Hide();
        }
      }
    }
  }
}
