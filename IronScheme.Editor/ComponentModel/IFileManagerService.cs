#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion


#region Includes
using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using IronScheme.Editor.Controls;
using System.Drawing.Design;
using System.Windows.Forms.Design;

using IronScheme.Editor.Runtime;
using ToolStripMenuItem = IronScheme.Editor.Controls.ToolStripMenuItem;
#endregion

namespace IronScheme.Editor.ComponentModel
{
  public class Document
  {
    IDocument control;
    List<IDocument> views = new List<IDocument>();

    public Document()
    {
      AddView(control = CreateControl());
    }

    public Document(IDocument mainview)
    {
      AddView(control = mainview);
    }


    public virtual IDocument CreateControl()
    {
      return new AdvancedTextBox();
    }

    internal void SwitchView(IDocument newview)
    {
      IDocument oldview = control;
      control = newview;
      SwitchView(newview, oldview);

      (ServiceHost.File as FileManager).RefreshState();
    }

    protected virtual void SwitchView(IDocument newview, IDocument oldview)
    {
      
    }

    protected void AddView(IDocument view)
    {
      ISelectObject so = view as ISelectObject;
      if (so != null)
      {
        so.SelectObjectChanged += delegate(object sender, EventArgs e)
        {
          ServiceHost.Property.Grid.SelectedObject = so.SelectedObject;
          ServiceHost.Property.Grid.Refresh();
        };
      }
      views.Add(view);
    }

    internal Control ActiveControl
    {
      get { return control as Control; }
    }

    public IDocument ActiveView
    {
      get { return control; }
    }

    public IDocument[] Views 
    {
      get { return views.ToArray(); }
    }

    public bool IsDirty
    {
      get
      {
        bool dirty = false;
        foreach (IDocument doc in views)
        {
          IFile file = doc as IFile;
          if (file != null)
          {
            dirty |= file.IsDirty;
          }
        }
        return dirty;
      }
    }

    public void Close()
    {
      foreach (IDocument doc in views)
      {
        doc.Close();
      }
    }
  }
  /// <summary>
  /// Base interface for documents
  /// </summary>
  public interface IDocument
  {
    /// <summary>
    /// Opens a file
    /// </summary>
    /// <param name="filename">the file to open</param>
    void Open(string filename);

    /// <summary>
    /// Closes the file
    /// </summary>
    void Close();

    /// <summary>
    /// Gets or sets the DockContent
    /// </summary>
    object Tag {get;set;}

    /// <summary>
    /// Gets the current document info
    /// </summary>
    string Info {get;}

  }

  /// <summary>
  /// Defines a saveable document
  /// </summary>
  public interface IFile : IDocument
  {
    /// <summary>
    /// Gets whether the document needs saving
    /// </summary>
    bool IsDirty    {get;}

    /// <summary>
    /// Saves a files
    /// </summary>
    /// <param name="filename">the filename to save to</param>
    void Save(string filename);
  }

  /// <summary>
  /// Provides service for managing files
  /// </summary>
	[Name("File manager")]
	public interface IFileManagerService : IService
	{
    /// <summary>
    /// Registers a control type to file extentions
    /// </summary>
    /// <param name="control">the type of the control</param>
    /// <param name="exts">the extentions</param>
    void Register(Type control, params string[] exts);

    /// <summary>
    /// Registers a control type to file extentions
    /// </summary>
    /// <param name="control">the type of the control</param>
    /// <param name="language">the language to bind to</param>
    void Register(Type control, Languages.Language language);

    /// <summary>
    /// Gets an array of open files
    /// </summary>
		string[]				OpenFiles							{get;}

    /// <summary>
    /// Gets the number of open files
    /// </summary>
    int				      OpenFileCount					{get;}

    /// <summary>
    /// Gets an array of dirty files
    /// </summary>
		string[]				DirtyFiles						{get;}

    /// <summary>
    /// Checks whether a file is dirty
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <returns>true if dirty</returns>
		bool						IsDirty								(string filename);

    /// <summary>
    /// Gets the Control hosting the file
    /// </summary>
		Control					this									[string filename] {get;}

    /// <summary>
    /// Gets the currently displayed file
    /// </summary>
		string					Current								{get;}


    /// <summary>
    /// Gets the currently displayed file
    /// </summary>
    Control					CurrentControl				{get;}

    /// <summary>
    /// Gets the current document.
    /// </summary>
    /// <value>The current document.</value>
    Document CurrentDocument { get;}


    /// <summary>
    /// Opens a file
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <returns>the Control hosting the file</returns>
		Control					Open									(string filename);

    /// <summary>
    /// Opens a file
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="state">the state</param>
    /// <returns>the Control hosting the file</returns>
    Control					Open									(string filename, DockState state);
    T               Open<T>               (string filename, DockState state) where T : Control, new();

    /// <summary>
    /// Closes a file
    /// </summary>
    /// <param name="filename">the filename</param>
		void						Close									(string filename);

    /// <summary>
    /// Closes all open files
    /// </summary>
    /// <returns>true if all files were closed</returns>
		bool						CloseAll							();

    /// <summary>
    /// Saves a file
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <returns>true if successfull</returns>
		bool						Save									(string filename);

    /// <summary>
    /// Brings the Control to the front (current tab)
    /// </summary>
    /// <param name="c">the Control</param>
    void            BringToFront          (Control c);

    /// <summary>
    /// Moves to the next window
    /// </summary>
    void            NextWindow();

    /// <summary>
    /// Moves to the previous window
    /// </summary>
    void            PreviousWindow();

    /// <summary>
    /// Fire when a file starts opening
    /// </summary>
		event	FileManagerEventHandler					Opening;

    /// <summary>
    /// Fire when a file has been opened
    /// </summary>
    event	FileManagerEventHandler					Opened;
		
    /// <summary>
    /// Fire when a file starts saving
    /// </summary>
    event	FileManagerEventHandler					Saving;
		
    /// <summary>
    /// Fire when a file has been saved
    /// </summary>
    event	FileManagerEventHandler					Saved;
		
    /// <summary>
    /// Fire when a file starts closing
    /// </summary>
    event	FileManagerEventHandler					Closing;
		
    /// <summary>
    /// Fire when a file has been closed
    /// </summary>
    event	FileManagerEventHandler					Closed;


    /// <summary>
    /// Gets the file tab.
    /// </summary>
    /// <value>The file tab.</value>
    IDockContent FileTab { get;}
	}

  /// <summary>
  /// EventArgs for file related events
  /// </summary>
	public class FileEventArgs : CancelEventArgs
	{
		string filename;

    /// <summary>
    /// Creates an instance of FileEventArgs
    /// </summary>
    /// <param name="filename">the filename</param>
		public FileEventArgs(string filename)
		{
			this.filename = filename;
		}

    /// <summary>
    /// Gets the filename
    /// </summary>
		public string FileName
		{
			get {return filename;}
		}
	}

  sealed class MRUFile : IComparable
  {
    DateTime lastuse;
    internal string filename;

    public MRUFile(string filename, int priority)
    {
      lastuse = DateTime.FromFileTime(priority);
      this.filename = filename;
    }

    public MRUFile(string filename)
    {
      lastuse = DateTime.Now;
      this.filename = filename;
    }
    public int CompareTo(object obj)
    {
      MRUFile com = obj as MRUFile;
      return com.lastuse.CompareTo(lastuse);
    }

    public void Update()
    {
      lastuse = DateTime.Now;
    }
  }

  /// <summary>
  /// EventHandler for file related events
  /// </summary>
	public delegate void FileManagerEventHandler(object sender, FileEventArgs e);

  [Menu("File")]
	sealed class FileManager : ServiceBase, IFileManagerService
	{
    readonly internal Dictionary<string, Document> buffers = new Dictionary<string, Document>();
    readonly Hashtable controlmap = new Hashtable();
    readonly Hashtable langmap = new Hashtable();
		ArrayList recentfiles = new ArrayList();

    // this is unreliable
		internal string current = null;

    IDockContent filetab = Runtime.DockFactory.Content();

    public IDockContent FileTab
    {
      get { return filetab; }
    }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				TextWriter writer = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + 
          "recent.ini", false, Encoding.Default);

				//reverse the list so it gets loaded in reverse
				string[] rc = RecentFiles;
				Array.Reverse(rc);

				foreach (string file in rc)
				{
					writer.WriteLine(file);
				}

				writer.Flush();
				writer.Close();
			}
			base.Dispose (disposing);
		}

    public void Register(Type control, params string[] exts)
    {
      foreach (string ext in exts)
      {
        controlmap[ext] = control;
      }
    }

    public void Register(Type control, IronScheme.Editor.Languages.Language language)
    {
      langmap[language] = control;
    }

    Document GetDocument<T>(string filename) where T : Control, new()
    {
      string ext = Path.GetExtension(filename).TrimStart('.');

      if (typeof(T) == typeof(Control))
      {
        Type ct = controlmap[ext] as Type;
        if (ct == null)
        {
          Languages.Language l = ServiceHost.Language.Suggest(filename);
          ct = langmap[l] as Type;
          if (ct == null)
          {
            return new Document();
          }
        }
        if (ct.IsSubclassOf(typeof(Document)))
        {
          return Activator.CreateInstance(ct) as Document;
        }
        else
        {
          return new Document(Activator.CreateInstance(ct) as Control as IDocument);
        }
      }
      else
      {
        return new Document(new T() as IDocument);
      }
    }

		public event	FileManagerEventHandler	Opening;
		public event	FileManagerEventHandler	Opened;
		public event	FileManagerEventHandler	Saving;
		public event	FileManagerEventHandler	Saved;
		public event	FileManagerEventHandler	Closing;
		public event	FileManagerEventHandler	Closed;

		public string[] RecentFiles
		{
			get
			{
				recentfiles.Sort();
				
				if (recentfiles.Count > 10)
				{
					recentfiles.RemoveRange(10, recentfiles.Count - 10);
				}
				string[] rec = new string[recentfiles.Count];
				for (int i = 0; i < recentfiles.Count; i++)
				{
					rec[i] = (recentfiles[i] as MRUFile).filename;
				}
				return rec;
			}
		}

    static string Normalize(string filename)
    {
      return char.ToLower(filename[0])+filename.Substring(1);
    }

		public bool IsDirty(string filename)
		{
      filename = Normalize(filename);
			return (filename != null && buffers.ContainsKey(filename)
				&& (this[filename] is IFile)
        && (this[filename] as IFile).IsDirty);
		}
#if TRACE

    static bool IsNotShellFile(string filename)
    {
      filename = Path.GetFileName(filename);
      return filename != "shell" && filename != "command.ls";
    }

		void TraceOpening(object sender, FileEventArgs e)
		{
			Trace.WriteLine(string.Format("Opening({0})", e.FileName));
		}
		void TraceOpened(object sender, FileEventArgs e)
		{
      if (IsNotShellFile(e.FileName))
      {
        Status.Write("Opened({0})", e.FileName);
      }
			Trace.WriteLine(string.Format("Opened({0})", e.FileName));
		}
		void TraceSaving(object sender, FileEventArgs e)
		{
			Trace.WriteLine(string.Format("Saving({0})", e.FileName));
		}
		void TraceSaved(object sender, FileEventArgs e)
		{
      if (IsNotShellFile(e.FileName))
      {
        Status.Write("Saved({0})", e.FileName);
      }
			Trace.WriteLine(string.Format("Saved({0})", e.FileName));
		}
		void TraceClosing(object sender, FileEventArgs e)
		{
			Trace.WriteLine(string.Format("Closing({0})", e.FileName));
		}
		void TraceClosed(object sender, FileEventArgs e)
		{
      if (IsNotShellFile(e.FileName))
      {
        Status.Write("Closed({0})", e.FileName);
      }
			Trace.WriteLine(string.Format("Closed({0})", e.FileName));
		}

#endif

    ToolStripMenuItem next, prev, sep;

		public FileManager()
		{
			string recfn = Application.StartupPath + Path.DirectorySeparatorChar + "recent.ini";
			if (File.Exists(recfn))
			{
				TextReader reader = new StreamReader(recfn, Encoding.Default, true);
				string rf;
				//fast CPU hack ;p
				int priority = 0;

				while ((rf = reader.ReadLine()) != null)
				{
					if (File.Exists(rf))
					{
						recentfiles.Add(new MRUFile(rf, priority++));
					}
				}
				reader.Close();
			}

      //FileExplorer fe = new FileExplorer();
      //fe.Folder = Application.StartupPath;
      //fe.Dock = DockStyle.Fill;

      //filetab.Controls.Add(fe);

      IWindowService ws = ServiceHost.Window;
      filetab.Show(ws.Document, DockState.DockRightAutoHide);
      filetab.Hide();
      filetab.HideOnClose = true;

      filetab.Text = "File Explorer";

      ToolStripMenuItem wins = ServiceHost.Menu["Window"];

      next = new ToolStripMenuItem("Next window");
      next.Click +=new EventHandler(pmi_Click);
      next.Tag = "Window.Next.png";
      wins.DropDownItems.Add(next);

      prev = new ToolStripMenuItem("Previous window");
      prev.Click +=new EventHandler(pmi_Click2);
      prev.Tag = "Window.Previous.png";
      wins.DropDownItems.Add(prev);

      wins.DropDownItems.Add(sep = new ToolStripMenuItem("-"));

      next.Visible = 
        prev.Visible = 
        sep.Visible = false;

      if (SettingsService.idemode)
      {
        ServiceHost.Window.Document.ActiveContentChanged +=new EventHandler(this.tc_SelectedIndexChanged);
      }

      Register(typeof(GridDocument), "gls");
      Register(typeof(AssemblyBrowser), "dll", "exe");

      //Register(typeof(XmlDocument), ServiceHost.Language["xml"]);

#if TRACE
			Opening		+=	new FileManagerEventHandler(TraceOpening);
			Opened		+=	new FileManagerEventHandler(TraceOpened);
			Saving		+=	new FileManagerEventHandler(TraceSaving);
			Saved			+=	new FileManagerEventHandler(TraceSaved);
			Closing		+=	new FileManagerEventHandler(TraceClosing);
			Closed		+=	new FileManagerEventHandler(TraceClosed);
#endif
		}

    internal void HackCurrent(AdvancedTextBox c)
    {
      //buffers[c.Name] = c;
      //current = c.Name;
    }

    internal void ToggleCurrent(AdvancedTextBox c)
    {
      current = c.Name;
    }

    public Control CurrentControl
    {
      get 
      {
        if (current == null || CurrentDocument == null)
        {
          return null;
        }
        return CurrentDocument.ActiveControl; 
      }
    }

    public Document CurrentDocument
    {
      get
      {
        if (current == null)
        {
          return null;
        }
        return buffers[current];
      }
    }



    class RecentFilesConvertor : MenuDescriptor
    {
      public override ICollection  GetValues()
      {
        return (ServiceHost.File as FileManager).RecentFiles;
      }
    }


    [MenuItem("Recent Files", Index = 900, Converter=typeof(RecentFilesConvertor))]
    string RecentFile
    {
      get { return string.Empty; }
      set
      {
        Open(value);
      }
    }

    class RecentProjectsConvertor : MenuDescriptor
    {
      public override ICollection GetValues()
      {
        return (ServiceHost.Project as ProjectManager).RecentProjects;
      }
    }


    [MenuItem("Recent Projects", Index = 901, Converter = typeof(RecentProjectsConvertor))]
    string RecentProject
    {
      get { return string.Empty; }
      set
      {
        ServiceHost.Project.Open(value);
      }
    }

    [MenuItem("Exit", Index = 1000, Image = "File.Exit.png")]
		void Exit()
		{
			ServiceHost.Window.MainForm.Close();
		}

    [MenuItem("New\\Blank File", Index = 0, Image = "File.New.png")]
    void NewTextFile()
    {
      string filename = "untitled.txt";
      int i = 1;
      while (File.Exists(filename))
      {
        filename = "untitled" + i++ + ".txt";
      }
      Open(filename);
    }


    [MenuItem("New\\File...", Index = 1, Image = "File.New.png", AllowToolBar = true)]
		void NewFile()
		{
      NewFileWizard nfw = new NewFileWizard();
      Hashtable lnames = new Hashtable();

      Collections.Set s = new IronScheme.Editor.Collections.Set(controlmap.Values);

      foreach (Type doc in s)
      {
        if (typeof(IFile).IsAssignableFrom(doc))
        {
          lnames.Add(doc.Name, doc);
        }
      }

      foreach (Languages.Language l in ServiceHost.Language.Languages)
      {
        lnames.Add(l.Name, l);
      }

      ArrayList ll = new ArrayList(lnames.Keys);
      ll.Sort();

      foreach (string lname in ll)
      {
        nfw.prjtype.Items.Add(lnames[lname]);
      }

    TRYAGAIN:

      if (DialogResult.OK == nfw.ShowDialog(ServiceHost.Window.MainForm))
      {
        Languages.Language l = nfw.prjtype.SelectedItem as Languages.Language;
        string name = nfw.name.Text;
        string loc = nfw.loc.Text;

        if (l == null)
        {

          if (!Path.HasExtension(name))
          {
            Type t = nfw.prjtype.SelectedItem as Type;
            string ext = "";
            foreach (DictionaryEntry de in controlmap)
            {
              if (de.Value == t)
              {
                ext = de.Key as string;
                break;
              }
            }
            name = Path.ChangeExtension(name, ext);
          }
        }
        else
        {
          if (!Path.HasExtension(name) && l.DefaultExtension != "*")
          {
            name = Path.ChangeExtension(name, l.DefaultExtension);
          }
        }

        string fname = loc + Path.DirectorySeparatorChar + name;

        if (File.Exists(fname))
        {
          if (MessageBox.Show(ServiceHost.Window.MainForm, "File exists, would you like to overwrite?", "Confirmation",
            MessageBoxButtons.YesNo) == DialogResult.No)
          {
            goto TRYAGAIN;
          }
        }

        AdvancedTextBox atb = Open(fname) as AdvancedTextBox;
        if (atb != null)
        {
          atb.Buffer.InsertString(l.DefaultFileContent);
        }
      }
      nfw.Dispose();
		}

    //[MenuItem("New\\Project...", Index = 2, Image = "Project.New.png", AllowToolBar = true)]
    //void NewProject()
    //{
    //  //(ServiceHost.Project as ProjectManager).Create();
    //  MessageBox.Show(ServiceHost.Window.MainForm, "Coming soon!");
    //}


    public void BringToFront(Control c)
    {
      AdvancedTextBox atb = c as AdvancedTextBox;
      if (atb != null)
      {
        (atb.Tag as IDockContent).Activate();
        atb.Invalidate();
      }
    }

    [MenuItem("Open\\File...", Index = 2, Image = "File.Open.png", AllowToolBar = true)]
    void OpenFile()
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Multiselect = true;
      ofd.RestoreDirectory = true;

      StringBuilder sb = new StringBuilder();
      StringBuilder ex = new StringBuilder();
      StringBuilder ab = new StringBuilder();

      ab.Append("All supported files|");

      IProjectManagerService pms = ServiceHost.Project;

      foreach (string ext in controlmap.Keys)
      {
        ab.AppendFormat("*.{0};", ext);
        sb.AppendFormat("{0} ({1})|{1}|", (controlmap[ext] as Type).Name, "*." + ext);
      }

      foreach (Languages.Language l in ServiceHost.Language.Languages)
      {
        string[] exts = l.Extensions;

        if (exts.Length > 0)
        {
          if (exts[0] != "*")
          {
            ex.AppendFormat("*.{0};", exts[0]);
            ab.AppendFormat("*.{0};", exts[0]);
          }

          for (int i = 1; i < exts.Length; i++)
          {
            if (exts[i] != "*")
            {
              ex.AppendFormat("*.{0};", exts[i]);
              ab.AppendFormat("*.{0};", exts[i]);
            }
          }
        }

        if (ex.Length > 0)
        {
          ex.Length--;
          sb.AppendFormat("{0} ({1})|{1}|", l.Name, ex.ToString());
          ex.Length = 0;
        }
      }

      ab.Length--;
      ab.Append("|");

      ab.AppendFormat("{0}All files (*.*)|*.*", sb);

      ofd.Filter = ab.ToString();

      if (ofd.ShowDialog(ServiceHost.Window.MainForm) == DialogResult.OK)
      {
        Open(ofd.FileName);
      }
    }

    [MenuItem("Open\\Project...", Index = 3, Image = "Project.Open.png", AllowToolBar = true)]
    void Open()
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.CheckFileExists = true;
      ofd.CheckPathExists = true;
      ofd.AddExtension = true;
      ofd.Filter = "MSBuild Project files|*.sln;*.*proj;";
      ofd.Multiselect = false;
      ofd.RestoreDirectory = true;

      ProjectManager pm = ServiceHost.Project as ProjectManager;

      if (DialogResult.OK == ofd.ShowDialog(ServiceHost.Window.MainForm))
      {
        pm.CloseAll();
        Application.DoEvents();
        pm.Open(ofd.FileName);
      }
    }

    bool closeresult = false;

		public bool CloseAll()
		{
      closeresult = true;
			CloseAllFiles();
      return closeresult;
		}

    [MenuItem("Close All", Index = 21, State = ApplicationState.File)]
		void CloseAllFiles()
		{
			foreach (string file in OpenFiles)
			{
        string ffile = file;
        Document doc = buffers[file];
        if (doc.IsDirty)
        {
          foreach (IDocument view in doc.Views)
          {
            IFile atb = view as IFile;
            if (atb != null && atb.IsDirty)
            {
              DialogResult dr = MessageBox.Show(ServiceHost.Window.MainForm, file + " has been modified.\nWould you like to save it?",
                "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

              switch (dr)
              {
                case DialogResult.Yes:
                  ffile = SaveFileInternal(file);
                  closeresult = true;
                  break;
                case DialogResult.No:
                  closeresult = true;
                  break;
                case DialogResult.Cancel:
                  closeresult = false;
                  return;
              }
            }
          }
        }
				Close(ffile);
			}
			GC.Collect();
		}

    [MenuItem("Close", Index = 20, State = ApplicationState.File, Image = "File.Close.png", AllowToolBar = true)]
    void CloseFile()
    {
      string current = this.current;
      if (current == null)
      {
        return;
      }


      Document doc = buffers[current];
      if (doc.IsDirty)
      {
        foreach (IDocument view in doc.Views)
        {
          IFile atb = view as IFile;

          if (atb != null && atb.IsDirty)
          {
            DialogResult dr = MessageBox.Show(ServiceHost.Window.MainForm, current + " has been modified.\nWould you like to save it?",
              "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

            switch (dr)
            {
              case DialogResult.Yes:
                current = SaveFileInternal(current);
                break;
              case DialogResult.No:
                break;
              case DialogResult.Cancel:
                return;
            }
          }
        }
      }


      Close(current);
      //GC.Collect();
    }

    [MenuItem("Copy FullPath", Index = 22, State = ApplicationState.File, Image = "Edit.Copy.png")]
    void CopyFullPath()
    {
      Clipboard.SetText(Path.GetFullPath(Current));
    }

    void DocClose(object sender, CancelEventArgs e)
    {
      // note the local var, dont merge with above code
      string current = (sender as Control).Tag as string;

      if (current == null)
      {
        return;
      }

      Document doc = buffers[current];
      if (doc.IsDirty)
      {
        foreach (IDocument view in doc.Views)
        {
          IFile atb = view as IFile;

          if (atb != null && atb.IsDirty)
          {
            DialogResult dr = MessageBox.Show(ServiceHost.Window.MainForm, current + " has been modified.\nWould you like to save it?",
              "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

            switch (dr)
            {
              case DialogResult.Yes:
                current = SaveFileInternal(current);
                break;
              case DialogResult.No:
                break;
              case DialogResult.Cancel:
                e.Cancel = true;
                return;
            }
          }
        }
      }

      Close(current, false);
      //GC.Collect();
    }


    [MenuItem("Save", Index = 10, State = ApplicationState.File, Image = "File.Save.png", AllowToolBar = true)]
		void SaveFile()
		{
			if (current != null && this[current] is IFile)
			{
        SaveFileInternal(current);
			}
		}

    string SaveFileInternal(string filename)
    {
      if (Path.GetFileName(filename).ToLower().StartsWith("untitled") && !File.Exists(filename))
      {
        return SaveFileAs(true, filename);
      }
      else
      {
        Save(filename);
        return filename;
      }
    }

    [MenuItem("Save File As...", Index = 11, State = ApplicationState.File, Image = "File.SaveAs.png")]
		void SaveFileAs()
		{
			if (current != null)
			{
        SaveFileAs(true, current);
			}
		}

    string SaveFileAs(bool reopen, string filename)
    {
      SaveFileDialog sfd = new SaveFileDialog();
      sfd.CheckPathExists = true;

      string ext = Path.GetExtension(filename).TrimStart('.');

      StringBuilder sb = new StringBuilder();
      StringBuilder ex = new StringBuilder();

      Languages.Language l = ServiceHost.Language[ext == string.Empty ? "*" : ext];
      if (l != null)
      {
        string[] exts = l.Extensions;

        ex.AppendFormat("*.{0}", exts[0]);

        for (int i = 1; i < exts.Length; i++)
        {
          ex.AppendFormat(";*.{0}", exts[i]);
        }

        sb.AppendFormat("{0} ({1})|{1}|", l.Name, ex);
      }

      sb.Append("All files (*.*)|*.*");

      sfd.FileName = filename;
      sfd.Filter = sb.ToString();
      sfd.RestoreDirectory = true;

      if (sfd.ShowDialog(ServiceHost.Window.MainForm) == DialogResult.OK)
      {
        if (filename != null && this[filename] is IFile)
        {
          IFile atb = this[filename] as IFile;
          atb.Save(sfd.FileName);

          //buffers.Remove(filename);

          //string newfilename = Normalize(sfd.FileName);

          //buffers.Add(newfilename, atb);

          Close(filename);
          if (reopen)
          {
            Open(sfd.FileName);
          }

          return sfd.FileName;
        }
      }
      return null;
    }

    public int OpenFileCount
    {
      get {return buffers.Count;}
    }

		public string[] OpenFiles
		{
			get	
			{
				ArrayList open = new ArrayList(buffers.Keys);
				open.Sort();
				return open.ToArray(typeof(string)) as string[];
			}
		}

    public void SaveDirtyFiles()
    {
      foreach (string df in DirtyFiles)
      {
        IFile f = this[df] as IFile;

        if (f != null)
        {
          f.Save(df);
        }
      }
    }

		public string[] DirtyFiles
		{
			get
			{
				ArrayList dirty = new ArrayList();
				foreach (string file in buffers.Keys)
				{
					if (this[file] is IFile)
					{
						if ((this[file] as IFile).IsDirty)
						{
							dirty.Add(file);
						}
					}
				}
				return dirty.ToArray(typeof(string)) as string[];
			}
		}

		public string Current
		{
			get {return current;}
		}

    void fsw_Changed(object sender, FileSystemEventArgs e)
    {
      if (ServiceHost.Window.MainForm.InvokeRequired)
      {
        try
        {
          ServiceHost.Window.MainForm.Invoke(new FileSystemEventHandler(fsw_Changed), new object[] { sender, e });
        }
        catch (ObjectDisposedException)
        {
          //stupid parking window on shutdown...
        }
        return;
      }

      AdvancedTextBox atb = this[e.FullPath] as AdvancedTextBox;

      if (atb != null && !atb.AutoSave)
      {
        try
        {
          if (atb.LastSaveTime < File.GetLastWriteTime(e.FullPath))
          {
            // this is bad, but it retains the undo buffer, kind off
            string txt = File.ReadAllText(e.FullPath);
            int i = atb.SelectionStart;
            atb.SelectAll();
            atb.Buffer.InsertString(txt);
            atb.SelectionStart = i;
            atb.SelectionLength = 0;
            atb.Invalidate();
          }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex);
        }
      }
    }

    public Control Open(string filename, DockState state)
    {
      return Open<Control>(filename, state);
    }

    public Control Open(
      [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
         string filename)
    {
      return Open(filename, DockState.Document);
    }

    delegate Control OpenFunc(string filename, DockState ds);

    readonly Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

    //TODO: Redesign this recursive crap...
    public T Open<T>(
      [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
      string filename, DockState ds) where T : Control, new()
    {
      if (InvokeRequired)
      {
        return Invoke(new OpenFunc(Open<T>), new object[] { filename, ds }) as T;
      }

      if (filename == null || filename == string.Empty)
      {
        return null;
      }

      filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      // hmmmm why does .NET check filename, Path is meant to be utility function?
      filename = Path.GetFullPath(filename);
      filename = Normalize(filename);

      string ext = Path.GetExtension(filename).TrimStart('.').ToLower();

      IMenuService ms = ServiceHost.Menu;

      if (buffers.ContainsKey(filename))
      {
        T c = this[filename] as T;
        (c.Tag as IDockContent).Activate();
        c.Focus();
        current = filename;
        
        if (Opened != null)
        {
          FileEventArgs fe = new FileEventArgs(filename);
          Opened(this, fe);
        }
        return c;
      }
      else
      {
        if (Opening != null)
        {
          FileEventArgs fe = new FileEventArgs(filename);
          Opening(this, fe);
          if (fe.Cancel)
          {
            return null;
          }
        }

        string fn = Path.GetFileName(filename);

        if (!watchers.ContainsKey(filename) && fn != "shell" && fn != "command.ls")
        {
          FileSystemWatcher fsw = new FileSystemWatcher(Path.GetDirectoryName(filename), Path.GetFileName(filename));
          fsw.Changed += fsw_Changed;
          fsw.NotifyFilter |= NotifyFilters.Attributes;
          fsw.EnableRaisingEvents = true;
          fsw.Error += new ErrorEventHandler(fsw_Error);
          watchers.Add(filename, fsw);
        }

        IDockContent tp = Runtime.DockFactory.Content();
        tp.Text = Path.GetFileName(filename);
        if (tp.Text == "command.ls")
        {
          tp.Text = "Scratch Pad";
        }

        tp.Tag = filename;
        tp.Closing += new CancelEventHandler(DocClose);

        ToolStripMenuItem mi = ServiceHost.Menu["File"];

        ContextMenuStrip contextmenu = new ContextMenuStrip();

        Hashtable attrmap = (ServiceHost.Menu as MenuService).GetAttributeMap(mi);

        foreach (ToolStripItem m in mi.DropDownItems)
        {
          ToolStripMenuItem pmi = m as ToolStripMenuItem;
          if (pmi != null)
          {
            MenuItemAttribute mia = attrmap[pmi] as MenuItemAttribute;
            if (mia == null)
            {
            }
            else
              if ((mia.State & (ApplicationState.File)) != 0)
              {
                ToolStripMenuItem cmi = pmi.Clone();
                cmi.Enabled = true;
                contextmenu.Items.Add(cmi);
              }
          }
        }

        tp.TabPageContextMenuStrip = contextmenu;

        Document doc = GetDocument<T>(filename); 
        T c = doc.ActiveView as T;

        tp.Controls.Add(c);
        c.Dock = DockStyle.Fill;

        c.Tag = tp;

        UpdateMRU(filename);  

        buffers.Add(filename, doc);
        current = filename;
        doc.ActiveView.Open(filename);

        AdvancedTextBox atb = c as AdvancedTextBox;
        if (atb != null)
        {
          if (atb.Buffer.Language.SupportsNavigation)
          {
            NavigationBar navb = new NavigationBar();
            navb.Dock = DockStyle.Top;

            tp.Controls.Add(navb);
            navb.SendToBack();

            atb.navbar = navb;

          }
        }

        tp.Show(ServiceHost.Window.Document, ds);

        if (ms != null)
        {
          ToolStripMenuItem pmi = new ToolStripMenuItem(filename, null,
            new EventHandler(OpenWindow));

          foreach (ToolStripMenuItem mmi in ms["Window"].DropDownItems)
          {
            mmi.Checked = false;
          }

          pmi.Checked = true;
          ms["Window"].DropDownItems.Add(pmi);
        }

        return Open<T>(filename, ds);
      }
    }

    void UpdateMRU(string filename)
    {
      if (Path.GetFileName(filename) != "command.ls" && Path.GetFileName(filename) != "shell" && File.Exists(filename))
      {
        foreach (MRUFile mru in recentfiles)
        {
          if (mru.filename == filename)
          {
            mru.Update();
            return;
          }
        }
        recentfiles.Add(new MRUFile(filename));
      }
    }

    void fsw_Error(object sender, ErrorEventArgs e)
    {
      Console.WriteLine(e);
    }

		void OpenWindow(object sender, EventArgs e)
		{
      ToolStripMenuItem pmi = sender as ToolStripMenuItem;
			
			string file = pmi.Tag as string;

			IMenuService ms = ServiceHost.Menu;

      foreach (ToolStripMenuItem mi in ms["Window"].DropDownItems)
			{
				mi.Checked = false;
			}

			pmi.Checked = true;

			if (file == null)
			{
				file = pmi.Text;
			}

      Control c = this[file];

      if (c == null)
      {
        c = Open(file);
      }

      BringToFront(c);
		}

    class SaveFileNameEditor : UITypeEditor
    {
      SaveFileDialog saveFileDialog;
      
      public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
      {
        if ((provider != null) && 
          (((IWindowsFormsEditorService) provider.GetService(typeof(IWindowsFormsEditorService))) != null))
        {
          if (this.saveFileDialog == null)
          {
            this.saveFileDialog = new SaveFileDialog();
            this.InitializeDialog(this.saveFileDialog);
          }
          if (value is string)
          {
            this.saveFileDialog.FileName = (string) value;
          }
          if (this.saveFileDialog.ShowDialog() == DialogResult.OK)
          {
            value = this.saveFileDialog.FileName;
          }
        }
        return value;
      }
      
      public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
      {
        return System.Drawing.Design.UITypeEditorEditStyle.Modal;
      }

      protected virtual void InitializeDialog(SaveFileDialog saveFileDialog)
      {
      }
    }

		public bool Save(
      [Editor(typeof(SaveFileNameEditor), typeof(UITypeEditor))]
      string filename)
		{
      Trace.Assert(filename != null, "filename != null");
      if (filename == null)
      {
        return false;
      }

      filename = Normalize(filename);

			if (Saving != null)
			{
				FileEventArgs fe = new FileEventArgs(filename);
				Saving(this, fe);
				if (fe.Cancel)
				{
					return false;
				}
			}

      if (File.Exists(filename))
      {
		        //this is safe, nothing else to save here right now, maybe later...
        FileAttributes fa = File.GetAttributes(filename);

        if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
          DialogResult dr =	MessageBox.Show(ServiceHost.Window.MainForm, filename + 
            " is read-only.\nWould you like to save anyways?",
            "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

          switch (dr)
          {
            case DialogResult.Yes:
              File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
              break;
            case DialogResult.No:
              return true;
            case DialogResult.Cancel:
              return false;
          }
        }
      }

      FileSystemWatcher fsw;
      if (watchers.TryGetValue(filename, out fsw))
      {
        fsw.EnableRaisingEvents = false;
      }

      (this[current] as IFile).Save(filename);

      if (fsw != null)
      {
        fsw.EnableRaisingEvents = true;
      }

      UpdateMRU(filename);

			if (Saved != null)
			{
				FileEventArgs fe = new FileEventArgs(filename);
				Saved(this, fe);
			}
			return true;
		}

    public void Close(string filename)
    {
      Close(filename, true);
    }

		void Close(string filename, bool closecontainer)
		{
      filename = Normalize(filename);

			if (Closing != null)
			{
				FileEventArgs fe = new FileEventArgs(filename);
				Closing(this, fe);
				if (fe.Cancel)
				{
					return;
				}
			}

			if (buffers.ContainsKey(filename) && filename != "$grid$")
			{
        Document doc = buffers[filename];
				
        if (closecontainer)
        {
          foreach (IDocument c in doc.Views)
          {
            IDockContent tp = (c as Control).Tag as IDockContent;
            if (tp != null)
            {
              tp.Closing -= new CancelEventHandler(DocClose);
              tp.Close();
              current = null;
            }
            break;
          }
        }

        buffers.Remove(filename);
        doc.Close();
        
				IMenuService ms = ServiceHost.Menu;
				if (ms != null)
				{
          ToolStripMenuItem win = ms["Window"];
          foreach (ToolStripMenuItem pmi in win.DropDownItems)
					{
						if (pmi.Text == filename)
						{
							win.DropDownItems.Remove(pmi);
							break;
						}
					}
				}

        tc_SelectedIndexChanged(this, EventArgs.Empty);

        FileSystemWatcher fsw;
        if (watchers.TryGetValue(filename, out fsw))
        {
          fsw.EnableRaisingEvents = false;
          fsw.Dispose();
          watchers.Remove(filename);
        }

				if (Closed != null)
				{
					FileEventArgs fe = new FileEventArgs(filename);
					Closed(this, fe);
				}
			}
		}

		/// <summary>
		/// Gets a Control instance from a given file name, returns null if not found.
		/// </summary>
		public Control this[string filename]
		{
			get	
      { 
        if (filename == null || filename.Trim() == string.Empty)
        {
          return null;
        }
        filename = Normalize(filename);
        if (buffers.ContainsKey(filename))
        {
          return buffers[filename].ActiveView as Control;
        }
        else
        {
          return null;
        }
      }
		}

		void tc_SelectedIndexChanged(object sender, EventArgs e)
		{
			IMenuService ms = ServiceHost.Menu;

			if (ms != null)
			{
#if DEBUG
        //Trace.WriteLine("State change request: ActiveContent = {0}", ServiceHost.Window.Document.ActiveContent);
#endif
				if (ServiceHost.Window.Document.ActiveContent != null)
				{
					string curr = (ServiceHost.Window.Document.ActiveContent as IDockContent).Tag as string;
          if (curr != null)
          {
            current = curr;
            Control c = this[current];

            if (c == null)
            {
              ResetState();
              return;
            }

            c.Focus();

            foreach (ToolStripMenuItem mi in ms["Window"].DropDownItems)
            {
              mi.Checked = (mi.Text == current);
            }

            ApplicationState s = ServiceHost.State;

            if (c is IEdit)
            {
              s |= ApplicationState.Edit;
            }
            else
            {
              s &= ~ApplicationState.Edit;
            }
            
            if (c is IScroll)
            {
              s |= ApplicationState.Scroll;
            }
            else
            {
              s &= ~ApplicationState.Scroll;
            }

            if (c is INavigate)
            {
              s |= ApplicationState.Navigate;
            }
            else
            {
              s &= ~ApplicationState.Navigate;
            }

            if (c is IFile)
            {
              s |= ApplicationState.File;
            }
            else
            {
              s &= ~ApplicationState.File;
            }

            if (c is IDocument)
            {
              s |= ApplicationState.Document;
            }
            else
            {
              s &= ~ApplicationState.Document;
            }

            if (c is AdvancedTextBox)
            {
              s |= ApplicationState.Buffer;
            }
            else
            {
              s &= ~ApplicationState.Buffer;
            }
            ServiceHost.State = s;
         
          }
          else
          {
            ResetState();
          }
				}
				else
				{
          if (current != "$hack$")
          {
            if (ServiceHost.State != ApplicationState.Normal)
            {
              //current = null;
              ServiceHost.State &= ~(ApplicationState.File | ApplicationState.Document | ApplicationState.Buffer | 
                ApplicationState.Edit | ApplicationState.Navigate | ApplicationState.Scroll);
            }
          }
          if (current != null && !buffers.ContainsKey(current))
          {
            current = null;
          }
				}
			}
		}

    void ResetState()
    {
      if (ServiceHost.State != ApplicationState.Normal)
      {
        ServiceHost.State &= ~(ApplicationState.File | ApplicationState.Document | ApplicationState.Buffer |
          ApplicationState.Edit | ApplicationState.Navigate | ApplicationState.Scroll);
      }
      if (current != null && !buffers.ContainsKey(current))
      {
        current = null;
      }
    }

		void pb_DoubleClick(object sender, EventArgs e)
		{
			PictureBox pb = sender as PictureBox;
			pb.SizeMode = (PictureBoxSizeMode) (((int)pb.SizeMode + 1)%4);
			pb.Refresh();
			//this works kinda....
    }

    public void NextWindow()
    {
      IDockContent[] docs = ServiceHost.Window.Document.Documents;
      int i = Array.IndexOf(docs, ServiceHost.Window.Document.ActiveDocument);
      i = (i + 1)%docs.Length;
      while (docs[i].DockState != DockState.Document)
      {
        i = (i + 1)%docs.Length;
      }
      docs[i].Activate();
    }

    public void PreviousWindow()
    {
      IDockContent[] docs = ServiceHost.Window.Document.Documents;
      int i = Array.IndexOf(docs, ServiceHost.Window.Document.ActiveDocument);
      i = (i + docs.Length - 1)%docs.Length;
      while (docs[i].DockState != DockState.Document)
      {
        i = (i + docs.Length - 1)%docs.Length;
      }
      docs[i].Activate();
    }

    private void pmi_Click(object sender, EventArgs e)
    {
      NextWindow();
    }

    private void pmi_Click2(object sender, EventArgs e)
    {
      PreviousWindow();
    }




    internal void RefreshState()
    {
      tc_SelectedIndexChanged(this, EventArgs.Empty);
    }
  }

}
