#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion


#region Includes
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;

using ToolStripMenuItem = IronScheme.Editor.Controls.ToolStripMenuItem;
#endregion

namespace IronScheme.Editor.ComponentModel
{
  /// <summary>
  /// Defines a toplevel menu name for service
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)] 
  public class MenuAttribute : Attribute
  {
    string text;

    /// <summary>
    /// Creates instance of MenuAttribute
    /// </summary>
    /// <param name="text">the name</param>
    public MenuAttribute(string text)
    {
      this.text = text;
    }

    /// <summary>
    /// Gets the name of the toplevel menu
    /// </summary>
    public string Text
    {
      get {return text;}
    }
  }

  /// <summary>
  /// Defines the current application state, will probably become internal
  /// </summary>
  [Flags]
  public enum ApplicationState
  {
    /// <summary>
    /// Application is uninitialized
    /// </summary>
    UnInitialized = -1,
    /// <summary>
    /// Normal
    /// </summary>
    Normal        = 0,
    /// <summary>
    /// File is active
    /// </summary>
    File          = 1,
    /// <summary>
    /// Project is active
    /// </summary>
    Project       = 2,
    /// <summary>
    /// Debugger active
    /// </summary>
    Debug         = 4,
    /// <summary>
    /// Breakpoint hit
    /// </summary>
    Break         = 8,
    /// <summary>
    /// Debugger active and breakpoint hit
    /// </summary>
    DebugBreak    = Debug | Break,
    /// <summary>
    /// AutoComplete active
    /// </summary>
    AutoComplete  = 16,
    /// <summary>
    /// Buffer (text editor) active
    /// </summary>
    Buffer        = 32,
    /// <summary>
    /// Buffer and project active
    /// </summary>
    ProjectBuffer = Project | Buffer,
    /// <summary>
    /// Grid active
    /// </summary>
    Grid          = 64,
    /// <summary>
    /// IEdit control active
    /// </summary>
    Edit          = 128,
    /// <summary>
    /// INavigate control active
    /// </summary>
    Navigate      = 256,
    /// <summary>
    /// IScroll control active
    /// </summary>
    Scroll        = 512,

    Build         = 1024,

    Document      = 2048,
  }

  /// <summary>
  /// Suppresses menu creation
  /// </summary>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple=false)] 
  public class SuppressMenuAttribute : Attribute
  {
  }

  /// <summary>
  /// Defines menuitems on services
  /// </summary>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple=false)] 
  public class MenuItemAttribute : Attribute, IComparable
  {
    string text, image;
    ApplicationState state = 0; 
    int index = -1;
    static int maxindex = -1;
    internal MemberInfo invoke;
    internal bool istogglemenu = false;
    internal ServiceBase ctr;
    internal ToolStripMenuItem mi;
    bool allowtoolbar = false;
  
    /// <summary>
    /// Creates an instance of MenuItemAttribute
    /// </summary>
    /// <param name="text">the name</param>
    public MenuItemAttribute(string text)
    {
      this.text = text;
      index = maxindex + 1;
    }

    /// <summary>
    /// Gets the name of the item
    /// </summary>
    public string Text
    {
      get {return text;}
    }

    /// <summary>
    /// Gets or sets the image
    /// </summary>
    public string Image
    {
      get {return image;}
      set {image = value;}
    }

    /// <summary>
    /// Gets or sets the state
    /// </summary>
    /// <remarks>Flagged usage</remarks>
    public ApplicationState State
    {
      get {return state;}
      set {state = value;}
    }

    /// <summary>
    /// Gets or sets the index
    /// </summary>
    /// <remarks>any 'gaps' in the number sequence will add seperator items</remarks>
    public int Index
    {
      get {return index;}
      set
      {
        index = value;
        maxindex = Math.Max(maxindex, value);
      }
    }

    Type conv;

    /// <summary>
    /// Gets or sets the converter.
    /// </summary>
    /// <value>The converter.</value>
    public Type Converter
    {
      get { return conv; }
      set { conv = value; }
    }

    /// <summary>
    /// Gets or sets whether to create a toolbar button
    /// </summary>
    public bool AllowToolBar
    {
      get {return allowtoolbar;}
      set {allowtoolbar = value;}
    }
    
    int IComparable.CompareTo(object obj)
    {
      MenuItemAttribute b = obj  as MenuItemAttribute;
      if (b == null)
      {
        return -1;
      }
      return Index - b.Index;
    }
  }

  /// <summary>
  /// Provides services for menu handling
  /// </summary>
	[Name("Menu service")]
	public interface IMenuService : IService
	{
    /// <summary>
    /// Gets the 'toplevelitem'
    /// </summary>
    ToolStripMenuItem this[string name] { get;}

    /// <summary>
    /// Gets the main menu of the hosting form
    /// </summary>
    MenuStrip MainMenu { get;}
	}

	sealed class MenuService : ServiceBase, IMenuService
	{
		readonly MenuStrip main;
    readonly Dictionary<string, ToolStripMenuItem> menus = new Dictionary<string, ToolStripMenuItem>();
    readonly Dictionary<ToolStripMenuItem, Hashtable> attrmapmap = new Dictionary<ToolStripMenuItem, Hashtable>();
    bool menuaccel = true;

    public MenuStrip MainMenu
		{
			get {return main;}
		}

    public bool MenuAccel
    {
      get {return menuaccel;}
      set 
      {
        if (menuaccel != value)
        {
          if (value)
          {
            foreach (ToolStripMenuItem mi in menus.Values)
            {
              mi.Text = mi.Tag.ToString();
            }
          }
          else
          {
            foreach (ToolStripMenuItem mi in menus.Values)
            {
              mi.Text = MnemonicEscape(mi.Tag.ToString());
            }
          }
        }
      }
    }

		public MenuService()
		{
      main = new MenuStrip();
      main.Dock = DockStyle.Top;

      ServiceHost.Window.MainForm.Controls.Add(main);

			AddTopLevel("&File");
			AddTopLevel("&Edit");
			AddTopLevel("&View");
			AddTopLevel("&Project");
      AddTopLevel("&Build");
      AddTopLevel("&Debug");
      AddTopLevel("&Script");
			AddTopLevel("&Window");
      AddTopLevel("&Help");

      ServiceHost.StateChanged +=new EventHandler(ServiceHost_StateChanged);
		}

    static string MnemonicEscape(string s)
    {
      return s.Replace("&&", "||").Replace("&", string.Empty).Replace("||", "&");
    }

    public Hashtable GetAttributeMap(ToolStripMenuItem toplevel)
    {
      return attrmapmap[toplevel] as Hashtable;
    }

    public ToolStripMenuItem AddTopLevel(string name)
		{
			return AddTopLevel( new ToolStripMenuItem(name));
		}

    public ToolStripMenuItem AddTopLevel(ToolStripMenuItem mi)
		{
      if (ServiceHost.ToolBar != null)
      {
        (ServiceHost.ToolBar as ToolBarService).Add(mi, null);
      }
      mi.DropDownOpening +=new EventHandler(toplevel_Popup);
			main.Items.Add(mi);
      string mit = MnemonicEscape(mi.Text);
			menus[mit] = mi;
//      mi.Tag = mi.Text;
      attrmapmap[mi] = new Hashtable();
			return mi;
		}

    public void RemoveTopLevel(ToolStripMenuItem mi)
		{
			main.Items.Remove(mi);
			menus.Remove(MnemonicEscape(mi.Text));
      attrmapmap.Remove(mi);
      mi.DropDownOpening -=new EventHandler(toplevel_Popup);
		}

    public ToolStripMenuItem this[string name]
		{
			get	
      {
        string escname = MnemonicEscape(name);
        if (menus.ContainsKey(escname))
        {
          return menus[escname];
        }
        else
        {
          return null;
        }
      }
		}

    void toplevel_Popup(object sender, EventArgs e)
    {
      ValidateMenuState(sender as ToolStripMenuItem); 
    }

    void ValidateMenuState(ToolStripMenuItem toplevel)
    {
      Hashtable attrmap = GetAttributeMap(toplevel);

      foreach (ToolStripItem pmi in toplevel.DropDownItems)
      {
        MenuItemAttribute mia = attrmap[pmi] as MenuItemAttribute;
        if (mia == null) // in case its a seperator or submenu
        {
          if (!(pmi is ToolStripSeparator))
          {
            foreach (ToolStripMenuItem spmi in (pmi as ToolStripMenuItem).DropDownItems)
            {
              MenuItemAttribute smia = attrmap[spmi] as MenuItemAttribute;

              ServiceBase ctr = smia.ctr;
              spmi.Enabled = ((ctr.MenuState & smia.State) == smia.State);

              if (smia.istogglemenu)
              {
                try
                {
                  spmi.Checked = (bool) ((PropertyInfo) smia.invoke).GetValue(smia.ctr, new object[0]);
                }
                catch
                {
                  //something not ready, sorts itself out
                }
              }
            }
          }
        }
        else
        {
          ServiceBase ctr = mia.ctr;
          pmi.Enabled = ((ctr.MenuState & mia.State) == mia.State);

          if (mia.istogglemenu)
          {
            try
            {
              (pmi as ToolStripMenuItem).Checked = (bool) ((PropertyInfo) mia.invoke).GetValue(mia.ctr, new object[0]);
            }
            catch
            {
              //something not ready, sorts itself out
            }
          }
        }
      }
    }

    void ServiceHost_StateChanged(object sender, EventArgs e)
    {
      foreach (ToolStripMenuItem mi in menus.Values)
      {
        ValidateMenuState(mi);
      }
    }
  }
}
