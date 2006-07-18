#region License
 /*	  xacc                																											*
 	*		Copyright (C) 2003-2006  Llewellyn@Pritchard.org                          *
 	*																																							*
	*		This program is free software; you can redistribute it and/or modify			*
	*		it under the terms of the GNU Lesser General Public License as            *
  *   published by the Free Software Foundation; either version 2.1, or					*
	*		(at your option) any later version.																				*
	*																																							*
	*		This program is distributed in the hope that it will be useful,						*
	*		but WITHOUT ANY WARRANTY; without even the implied warranty of						*
	*		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the							*
	*		GNU Lesser General Public License for more details.												*
	*																																							*
	*		You should have received a copy of the GNU Lesser General Public License	*
	*		along with this program; if not, write to the Free Software								*
	*		Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA */
#endregion


#region Includes
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Drawing;
using Xacc.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using Xacc.Controls;
using System.Xml;
using System.Xml.Serialization;
using Xacc.CodeModel;
using Xacc.Collections;

using Utility = Xacc.Runtime.Compression;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

using SR = System.Resources;

using Microsoft.Build.BuildEngine;
using BuildProject = Microsoft.Build.BuildEngine.Project;

#endregion

namespace Xacc.Build
{
  /// <summary>
  /// Defines the default input extension for a Project
  /// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
	public class DefaultExtensionAttribute : Attribute
	{
		readonly string ext;

    /// <summary>
    /// Creates an instance of DefaultExtensionAttribute
    /// </summary>
    /// <param name="ext">the input extension</param>
		public DefaultExtensionAttribute(string ext)
		{
			this.ext = ext;
		}

    /// <summary>
    /// Gets the input extension, if any
    /// </summary>
		public string Extension 
    {
      get {return ext;}
    }
	}

  /// <summary>
  /// EventArgs for Project related events
  /// </summary>
	public class ProjectEventArgs	: EventArgs
	{
		
	}

  /// <summary>
  /// EventHandler for Project related events
  /// </summary>
	public delegate void ProjectEventHandler(object prj, ProjectEventArgs e);

  class MsBuildProject : Project
  {
    [Name("Compile")]
    class Compile : CustomAction
    {
      public Compile()
      {
        AddOptionAction(new Reference(this));
        AddOptionAction(new Resource(this));
      }

      public override bool Invoke(params string[] files)
      {
        return true;
      }
    }

    [Name("Reference")]
    class Reference : OptionAction
    {
      public Reference(Compile ca)
        : base(ca, null)
      {
      }
    }

    [Name("Embedded Resource")]
    class Resource : OptionAction
    {
      public Resource(Compile ca)
        : base(ca, null)
      {
      }
    }

    readonly BuildProject prj;
    public MsBuildProject(BuildProject prj)
    {
      this.prj = prj;
      AddActionType(typeof(Compile));
      ProjectName = prj.GetEvaluatedProperty("ProjectName");
      Environment.CurrentDirectory = RootDirectory = prj.GetEvaluatedProperty("ProjectDir");

      Trace.WriteLine("Items");

      foreach (BuildItemGroup big in prj.ItemGroups)
      {
        //if (!big.IsImported)
        {
          Trace.WriteLine("(" + big.Condition + ")" + "================================");
          foreach (BuildItem bi in big)
          {
            //if (!bi.IsImported)
            {
              Trace.WriteLine("\t" + bi.Name + "(" + bi.Condition + "): " + bi.Include);
            }
          }
          Trace.WriteLine("");
        }
      }

      Trace.WriteLine("");

      Trace.WriteLine("Properties");

      foreach (BuildPropertyGroup big in prj.PropertyGroups)
      {
        //if (!big.IsImported)
        {
          Trace.WriteLine("(" + big.Condition + ")" + "================================");
          foreach (BuildProperty bi in big)
          {
            //if (!bi.IsImported)
            {
              Trace.WriteLine("\t" + bi.Name + "(" + bi.Condition + "): " + bi.Value);
            }
          }
          Trace.WriteLine("");
        }
      }

      Trace.WriteLine("");

      Trace.WriteLine("Targets");

      foreach (Target t in prj.Targets)
      {
        //if (!t.IsImported)
        {
          Trace.WriteLine(t.Name + " (" + t.Condition + ") deps: " + t.DependsOnTargets);
          
          foreach (BuildTask bt in t)
          {
            Trace.WriteLine("\t"+bt.Name + " (" + bt.Condition + "): " + bt.Type);

            foreach (string tt in bt.GetParameterNames())
            {
              Trace.WriteLine("\t\t" + tt + " = " + bt.GetParameterValue(tt));
            }
          }

          Trace.WriteLine("");
        }
      }

      Compile c = new Compile();

      foreach (BuildItem bi in prj.GetEvaluatedItemsByName("Compile"))
      {
        AddFile(bi.Include, c);
      }
      foreach (BuildItem bi in prj.GetEvaluatedItemsByName("EmbeddedResource"))
      {
        AddFile(bi.Include);
      }

      foreach (BuildItem bi in prj.GetEvaluatedItemsByName("Reference"))
      {
        AddFile(bi.Include);
      }

      foreach (BuildItem bi in prj.GetEvaluatedItemsByName("None"))
      {
        AddFile(bi.Include, Action.None);
      }
    }
  }

  /// <summary>
  /// Base class for all Projects
  /// </summary>
  [System.Xml.Serialization.XmlTypeAttribute(Namespace="xacc:build")]
	[Image("Project.Type.png")]
	public abstract class Project
	{

    /// <summary>
    /// Gets the string representation of the project
    /// </summary>
    /// <returns>the project name</returns>
    public override string ToString()
    {
      return ProjectName;
    }

    #region Fields & Properties

		string name, location, rootdir;

		readonly Hashtable sources = new Hashtable();
    readonly TreeNode rootnode = new TreeNode();
    readonly Hashtable optionnodes = new Hashtable();
    readonly Hashtable actiontypes = new Hashtable();

		static readonly BinaryFormatter FORMATTER = new BinaryFormatter();
    static XmlSerializer ser;

    /// <summary>
    /// Event for when project is closed
    /// </summary>
		public event ProjectEventHandler Closed;

    /// <summary>
    /// Event for when project is opened
    /// </summary>
    public event ProjectEventHandler Opened;

    /// <summary>
    /// Event for when project is saved
    /// </summary>
		public event ProjectEventHandler Saved;

    NullAction nullaction = new NullAction();
    Action[] actions = {};
    FileSystemWatcher fsw = new FileSystemWatcher();

    [XmlIgnore]
    internal ICodeModule[] References
    {
      get {return data.references;}
      set {data.references = value;}
    }

    bool startup = false;

    /// <summary>
    /// Gets or sets whether project is the startup project
    /// </summary>
    [XmlAttribute("startup")]
    public bool Startup
    {
      get {return startup;}
      set 
      {
        if (startup != value)
        {
          startup = value;
          if ( rootnode.NodeFont == null)
          {
            rootnode.NodeFont = SystemInformation.MenuFont;
          }

          rootnode.NodeFont = new Font(rootnode.NodeFont, value ? FontStyle.Bold : FontStyle.Regular);
        }
      }
    }

    //readonly 
      ObjectTree projectautotree = new ObjectTree();

    internal ObjectTree ProjectAutoCompleteTree
    {
      get {return projectautotree;}
    }

    internal ObjectTree AutoCompleteTree
    {
      get {return data.autocompletetree;}
      set {data.autocompletetree = value;}
    }

    internal bool FileWatcherEnabled
    {
      get {return fsw.EnableRaisingEvents;}
      set {fsw.EnableRaisingEvents = value;}
    }

    /// <summary>
    /// Gets a breakpoint associate with the file
    /// </summary>
    /// <param name="filename">filename</param>
    /// <param name="linenr">the line number</param>
    /// <returns>the breakpoint if any</returns>
    public Breakpoint GetBreakpoint(string filename, int linenr)
    {
      Hashtable bps = GetBreakpoints(filename);
      if (bps == null)
      {
        return null;
      }
      return bps[linenr] as Breakpoint;
    }

    /// <summary>
    /// Get all breakpionts in a file
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <returns>the breakpoints</returns>
    public Hashtable GetBreakpoints(string filename)
    {
      filename = GetRelativeFilename(filename);
      return data.breakpoints[filename] as Hashtable;
    }

    internal void AddPairings(string filename, ArrayList pairings)
    {
      filename = GetRelativeFilename(filename);
      data.pairings[filename] = pairings;
    }

    /// <summary>
    /// Gets all the breakpoints in the project
    /// </summary>
    /// <returns>all the breakpoints</returns>
    public Breakpoint[] GetAllBreakpoints()
    {
      ArrayList bps = new ArrayList();

      foreach (Hashtable bph in data.breakpoints.Values)
      {
        bps.AddRange(bph.Values);
      }

      return bps.ToArray(typeof(Breakpoint)) as Breakpoint[];
    }

    /// <summary>
    /// Sets a breakpoint
    /// </summary>
    /// <param name="bp">the breakpoint to set</param>
    public void SetBreakpoint(Breakpoint bp)
    {
      string filename = GetRelativeFilename(bp.filename);
      Hashtable bps = data.breakpoints[filename] as Hashtable;
      if (bps == null)
      {
        data.breakpoints[filename] = bps = new Hashtable();
      }

      bps[bp.linenr - 1] = bp;
    }

    /// <summary>
    /// Removes a breakpoint from a file
    /// </summary>
    /// <param name="bp">the breakpoint to remove</param>
    public void RemoveBreakpoint(Breakpoint bp)
    {
      string filename = GetRelativeFilename(bp.filename);
      Hashtable bps = data.breakpoints[filename] as Hashtable;
      if (bps == null)
      {
        return;
      }

      bps.Remove(bp.linenr - 1);
    }

    /// <summary>
    /// Gets the CodelModel for the project
    /// </summary>
    [XmlIgnore]
    public ICodeModule CodeModel
    {
      get {return data.model;}
    }

    /// <summary>
    /// Gets the root node for the project
    /// </summary>
    public TreeNode RootNode
    {
      get {return rootnode;}
    }

    /// <summary>
    /// Gets the default extension of the project
    /// </summary>
    [XmlIgnore]
    public string DefaultExtension
    {
      get
      {
        foreach (DefaultExtensionAttribute e in GetType().GetCustomAttributes(typeof(DefaultExtensionAttribute), true))
        {
          return e.Extension;
        }
        return "*";
      }
    }

    /// <summary>
    /// Gets or sets the path of the project filename
    /// </summary>
    [XmlIgnore]
    public string Location
    {
      get	{ return location;}
      set { location = Normalize(Path.GetFullPath(value));	}
    }

    internal string DataLocation
    {
      get 
      {
        string l = RootDirectory + "\\obj";
        if (!Directory.Exists(l))
        {
          Directory.CreateDirectory(l);
        }
        return l;
      }
    }

    internal string OutputLocation
    {
      get 
      {
        string l = RootDirectory;
        if (!Directory.Exists(l))
        {
          Directory.CreateDirectory(l);
        }
        return ".";
      }
    }


    /// <summary>
    /// Gets or sets the root directopry of the project
    /// </summary>
    [XmlIgnore]
    public string RootDirectory
    {
      get	{	return rootdir;}
      set 
      { 
        rootdir = Normalize(Path.GetFullPath(value));	
        //fsw.EnableRaisingEvents = false;
        fsw.Path = rootdir;
        //fsw.EnableRaisingEvents = true;

      }
    }

    /// <summary>
    /// Gets a list of input files
    /// </summary>
    [XmlIgnore]
    public string[] Sources
    {
      get	{	return new ArrayList(sources.Keys).ToArray(typeof(string)) as string[];}
    }

    /// <summary>
    /// Gets or sets the project name
    /// </summary>
    [XmlAttribute("name")]
    public string ProjectName
    {
      get {return name;}
      set {CodeModel.Name = name = rootnode.Text = value;}
    }

	
    #endregion

    #region Action Config

    /// <summary>
    /// Gets or sets the array of Action for this project
    /// </summary>
    [XmlIgnore]
    public Action[] Actions
    {
      get {return actions;}
      set 
      {
        if (value == null)
        {
          actions = new Action[0];
        }
        else
        {
          actions = value;
        }
      }
    }

    Type[] types;

    internal Type[] ActionTypes
    {
      get 
      {
        if (types == null)
        {
          ArrayList alltypes = new ArrayList();
        
          foreach (ArrayList l in actiontypes.Values)
          {
            alltypes.AddRange(l);
          }

          types = new Set(alltypes).ToArray(typeof(Type)) as Type[];
        }
        return types;
      }
    }

    Type[] GetOptionActionTypes()
    {
      ArrayList l = new ArrayList();
      Type[] types = ActionTypes;
      foreach (Type t in types)
      {
        if (typeof(OptionAction).IsAssignableFrom(t))
        {
          l.Add(t);
        }
      }
      return l.ToArray(typeof(Type)) as Type[];
    }

    /// <summary>
    /// Add an action type to the project
    /// </summary>
    /// <param name="actiontype">the type of the Action</param>
    protected void AddActionType(Type actiontype)
    {
      string[] extt = InputExtensionAttribute.GetExtensions(actiontype);
      if (extt.Length == 0)
      {
        string ext = "*";
        ArrayList exts = actiontypes[ext] as ArrayList;
        if (exts == null)
        {
          actiontypes[ext] = (exts = new ArrayList());
        }
        if (!exts.Contains(actiontype))
        {
          exts.Add(actiontype);
        }
      }
      else
      {
        foreach (string ext in extt)
        {
          ArrayList exts = actiontypes[ext] as ArrayList;
          if (exts == null)
          {
            actiontypes[ext] = (exts = new ArrayList());
          }
          if (!exts.Contains(actiontype))
          {
            exts.Add(actiontype);
          }
        }
      }
    }

    Action this[int index]
    {
      get {return actions[index] as Action;}
      set { actions[index] = value; }
    }

    /// <summary>
    /// Gets the Action associated with a filename in the project
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <returns>the associate Action</returns>
    public Action GetAction(string filename)
    {
      filename = Normalize(Path.GetFullPath(filename));
      if (!sources.ContainsKey(filename))
      {
        return null;
      }

      return sources[filename] as Action;
    }

    internal Action SuggestAction(Type t)
    {
      if (t == null)
      {
        return Action.None;
      }
      Debug.Assert(actions != null);
      foreach (Action a in actions)
      {
        CustomAction ca = a as CustomAction;

        if (t == a.GetType())
        {
          if (ca != null)
          {
            if (ca.MultipleInput || ca.Input.Length == 0)
            {
              return a;
            }
          }
          else
          {
            return a;
          }
        }

        if (ca != null)
        {
          foreach (Type st in ca.ActionTypes)
          {
            if (st == t)
            {
              return ca.GetAction(t);
            }
          }
        }
      }
      return Activator.CreateInstance(t) as Action;
    }

    Action SuggestAction(string ext)
    {
      ArrayList t = actiontypes[ext] as ArrayList;
      if (t == null || t.Count == 0)
      {
        return nullaction;
      }
      return SuggestAction(t[0] as Type);
    }


    #endregion

    #region Dependency Tree

    void SortActions()
    {
      // copy list
      ArrayList actions = new ArrayList(Actions);
      
      Hashtable depcount = new Hashtable();
      Hashtable refcount = new Hashtable();

      foreach (Action a in actions)
      {
        depcount[a] = new ArrayList();
        refcount[a] = new ArrayList();
      }

      // build tree
      foreach (Action a in actions)
      {
        foreach (Action b in actions)
        {
          if (a == b)
          {
            continue;
          }

          CustomAction ca = a as CustomAction;
          CustomAction cb = b as CustomAction;


          if (ca != null && cb != null) 
          {
            if (ca.DependsOn(cb))
            {
              ((ArrayList)depcount[cb]).Add(ca);
              ((ArrayList)refcount[ca]).Add(cb);
            }
          }
        }
      }

      ArrayList nofriends = new ArrayList();

      foreach (Action a in actions)
      {
        if (((ArrayList)depcount[a]).Count == 0 && ((ArrayList)refcount[a]).Count == 0)
        {
          depcount.Remove(a);
          refcount.Remove(a);
          nofriends.Add(a);
        }
      }

      actions = new ArrayList(depcount.Keys);

      for (int i = 0; i < actions.Count - 1; i++)
      {
        Action a = (Action) actions[i];
        Action b = (Action) actions[i + 1];
        ArrayList adeps = (ArrayList) depcount[a];
        ArrayList arefs = (ArrayList) refcount[a];
        ArrayList bdeps = (ArrayList) depcount[b];
        ArrayList brefs = (ArrayList) refcount[b];
        
        if (adeps.Count < bdeps.Count || arefs.Count > brefs.Count)
        {
          actions[i] = b;
          actions[i + 1] = a;
          i -= 2;
          if (i < -1)
          {
            i = -1;
          }

        }
      }

      for (int i = 0; i < actions.Count - 1; i++)
      {
        CustomAction a = (CustomAction) actions[i];
        CustomAction b = (CustomAction) actions[i + 1];
        
        if (a.DependsOn(b))
        {
          actions[i] = b;
          actions[i + 1] = a;
          i -= 2;
          if (i < -1)
          {
            i = -1;
          }
        }
      }

      actions.AddRange(nofriends);
      this.actions = actions.ToArray(typeof(Action)) as Action[];
    }

    #endregion

    #region Constructor

    static Project()
    {
    }

    readonly bool invisible;

    internal bool IsInvisible
    {
      get {return invisible;}
    }

    BuildProject bp = new BuildProject();

    /// <summary>
    /// Creates an instance of Project
    /// </summary>
		protected Project()
		{
      invisible = GetType() == typeof(ScriptingService.ScriptProject);

      if (invisible)
      {
        rootnode = null;
      }
      else
      {

#if TRACE
        Opened	+=	new ProjectEventHandler(TraceOpened);
        Saved		+=	new ProjectEventHandler(TraceSaved);
        Closed	+=	new ProjectEventHandler(TraceClosed);
#endif
        IImageListProviderService ips = ServiceHost.ImageListProvider;
        if (ips != null)
        {
          ips.Add(GetType());
        }

        rootnode.SelectedImageIndex = rootnode.ImageIndex = ServiceHost.ImageListProvider[this];

        ServiceHost.Project.OutlineView.Nodes.Add(rootnode);

        rootnode.Tag = this;

        AddActionType(typeof(NullAction));

        fsw.IncludeSubdirectories = true;
        fsw.NotifyFilter = NotifyFilters.LastWrite;
        fsw.Changed +=new FileSystemEventHandler(fsw_Changed);
      }
		}

    void fsw_Changed(object sender, FileSystemEventArgs e)
    {
      if (ServiceHost.Window.MainForm.InvokeRequired)
      {
        try
        {
          ServiceHost.Window.MainForm.Invoke( new FileSystemEventHandler(fsw_Changed), new object[] { sender, e});
        }
        catch (ObjectDisposedException)
        {
          //stupid parking window on shutdown...
        }
        return;
      }

      AdvancedTextBox atb = ServiceHost.File[e.FullPath] as AdvancedTextBox;

      if (atb != null)
      {
        try
        {
          if (atb.LastSaveTime < File.GetLastWriteTime(e.FullPath))
          {
            atb.LoadFile(e.FullPath);
          }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
      }
    }

    #endregion
    
    #region Serialization
    
    internal void DeserializeProjectData()
    {
      string filename = DataLocation + "\\" + ProjectName + ".projectdata";
      if (File.Exists(filename))
      {
        try
        {
          using (System.IO.Stream s = System.IO.File.OpenRead(filename))
          {
            byte[] buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);

            buffer = Utility.Decompress(buffer);

            using (System.IO.Stream s2 = new System.IO.MemoryStream(buffer))
            {
              data = FORMATTER.Deserialize(s2) as ProjectData;

              EventHandler ev = (ServiceHost.Debug as DebugService).bpboundchange;

              foreach (Breakpoint bp in GetAllBreakpoints())
              {
                bp.bound = true;
                bp.boundchanged = ev;
              }
            }
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Trace.WriteLine(ex.Message, "Project data could not be loaded.");
        }
      }
      ServiceHost.CodeModel.Run(this);

      foreach (string file in data.openfiles)
      {
        OpenFile(file);
      }
    }

    internal void SerializeProjectData()
    {
      try
      {
        string filename = DataLocation + "\\" + ProjectName + ".projectdata";
        using (Stream s = System.IO.File.Create(filename))
        {
          using (MemoryStream s2 = new MemoryStream())
          {
            FORMATTER.Serialize(s2, data);

            byte[] buffer = s2.ToArray();

            buffer = Utility.Compress(buffer);

            s.Write(buffer, 0, buffer.Length);
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine(ex.Message, "Project data not serializable.");
      }
    }

    /// <summary>
    /// Saves the project
    /// </summary>
    public void Save()
    {
      Save(ServiceHost.Project.OpenProjects);
    }

    internal void Save(Project[] all)
    {
      if (Location == null)
      {
        return;
      }

      foreach (Project p in all)
      {
        p.fsw.EnableRaisingEvents = false;
      }

      Configuration.Projects pp = Activator.CreateInstance(Configuration.Projects.SerializerType) as Configuration.Projects;

      pp.projects = all;

      ArrayList acts = new ArrayList();

      if (actions != null)
      {

        foreach (Action a in actions)
        {
          CustomAction ca = a as CustomAction;
          if (ca != null && (ca.Input.Length > 0 || ca.Output != null))
          {
            acts.Add(a);
          }
        }
      }

      actions = acts.ToArray(typeof(Action)) as Action[];

      try
      {
        string bakfile = Location + ".bak";
        using (Stream s = File.Create(bakfile))
        {
          if (ser == null)
          {
            ser = new XmlSerializer(Configuration.Projects.SerializerType, new Type[] {typeof(RegexOptions)});
          }
          ser.Serialize(s, pp);
        }

        using (TextReader r = File.OpenText(bakfile))
        {
          using (TextWriter w = File.CreateText(Location))
          {
            string line = null;
            
            //clean up 'redundant' text
            while ((line = r.ReadLine()) != null)
            {
              if (line.IndexOf("xsi:nil=\"true\"") < 0)
              {
                w.WriteLine(line.Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ", 
                   string.Empty).Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ", 
                   string.Empty).Replace("startup=\"false\" ", string.Empty));
              }
            }
          }
        }

        File.Delete(bakfile);

        foreach (Project p in all)
        {
          p.SerializeProjectData();
        }

        if (Saved != null)
        {
          Saved(this, null);
        }
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex, "Project::Save");
      }

      foreach (Project p in all)
      {
        p.fsw.EnableRaisingEvents = true;
      }
    }


    #endregion

    #region Persisted data

    /// <summary>
    /// Adds references and generates project tree
    /// </summary>
    /// <param name="refs">the references</param>
    public void AddReferencesAndGenerateTree(params ICodeModule[] refs)
    {
      if (References == null)
      {
        References = refs;
      }
      else
      {
        ICodeModule[] newrefs = new ICodeModule[References.Length + refs.Length];
        Array.Copy(References, newrefs, References.Length);
        Array.Copy(refs, 0, newrefs, References.Length, refs.Length);

        References = newrefs;
      }

      Tree tree = new Tree();
      tree.tree = AutoCompleteTree;

      foreach (ICodeElement ele in refs)
      {
        GenerateTree(tree, ele);
      }
    }

    /// <summary>
    /// Generates the project tree
    /// </summary>
    public void GenerateProjectTree()
    {
      Tree tree = new Tree();
      projectautotree = new ObjectTree();
      tree.tree = ProjectAutoCompleteTree;

      foreach (ICodeElement ele in (CodeModel as ICodeContainerElement).Elements)
      {
        GenerateTree(tree, ele);
      }
    }

    void GenerateTree(Tree tree, ICodeElement ele)
    {
      if (!(ele is ICodeModule) && ele.Fullname.Length > 0)
      {
        tree.Add(ele.Fullname, ele);
      }

      ICodeContainerElement cce = ele as ICodeContainerElement;
      if (cce != null)
      {
        foreach (ICodeElement e in cce.Elements)
        {
          GenerateTree(tree, e);
        }
      }
    }

    ProjectData data = new ProjectData();

    [Serializable]
    class ProjectData
    {
      public CodeModule model  = new CodeModule(string.Empty);
      public ObjectTree autocompletetree = new ObjectTree();
      public ICodeModule[] references;
      public Hashtable breakpoints = new Hashtable();
      public Hashtable pairings    = new Hashtable();
      public ArrayList openfiles   = new ArrayList();
    }

    class AssemblyLoader : MarshalByRefObject
    {
      ICodeType LoadCodeType(ICodeModule cm, Type type)
      {
        string ns = type.Namespace;
        if (ns == null)
        {
          ns = string.Empty;
        }
        CodeNamespace cns = cm[ns] as CodeNamespace;
        if (cns == null)
        {
          cns = new CodeNamespace(ns);
          cm.Add(cns);
        }

        CodeType ct = null;
        if (type.IsClass)
        {
          ct = new CodeRefType(type.Name);
        }
        else if (type.IsEnum)
        {
          ct = new CodeEnum(type.Name);
        }
        else if (type.IsInterface)
        {
          ct = new CodeInterface(type.Name);
        }
        else if (type.IsValueType)
        {
          ct = new CodeValueType(type.Name);
        }
        else
        {
          ct = new CodeValueType(type.Name);
        }

        ct.Namespace = cns;
        return ct;
      }

      ICodeModule LoadAssembly(Assembly ass)
      {
        try 
        {
          ICodeModule cm = new CodeModule(Path.GetFileName(ass.CodeBase));
          foreach (Type type in ass.GetExportedTypes())
          {

            LoadCodeType(cm, type);
  
          }
          return cm;
        }
        catch (NotSupportedException)
        {
        
        }

        return null;
      }

      public ICodeModule[] LoadAssemblies(string[] names, params string[] path)
      {
        ArrayList modules = new ArrayList();
        foreach (string name in names)
        {
          string fn = Path.GetFullPath(name);
          for (int i = 0; i < path.Length & !File.Exists(fn); i++)
          {
            fn = path[i] + Path.DirectorySeparatorChar + name;
          }
          if (File.Exists(fn))
          {
            Assembly ass = Assembly.LoadFile(fn);
            ICodeModule mod = LoadAssembly(ass);
            modules.Add(mod);
          }
        }
        return modules.ToArray(typeof(ICodeModule)) as ICodeModule[];
      }
    }

    class Tree
    {
      internal ObjectTree tree;

      static string[] Tokenize(string name, params string[] delimiters)
      {
        return Algorithms.XString.Tokenize(name, delimiters);
      }

      public void Add(string name, ICodeElement o)
      {
        string[] b = Tokenize(name, ".");
//        Trace.WriteLine(string.Format("{0} : {1,-35} : {3,-15} : {4,-40} : {2}", 
//          o.GetHashCode(), o.Name, o.Fullname, o.GetType().Name, name));
//        Trace.WriteLine(o.Fullname);
        tree.Add(b, o);
      }

      public object this[string name]
      {
        get {return tree.Accepts(Tokenize(name, ".")); }
      }
    }

    /// <summary>
    /// Loads assemblies and add to project tree
    /// </summary>
    /// <param name="assnames">the names of the assemblys</param>
    public void LoadAssemblies(params string[] assnames)
    {
      if (assnames.Length > 0)
      {
        assnames = new Set(assnames).ToArray(typeof(string)) as string[];
        AppDomain assloader = AppDomain.CreateDomain("Assembly Loader");
        assloader.SetupInformation.LoaderOptimization = LoaderOptimization.MultiDomainHost;
        AssemblyLoader aa = assloader.CreateInstanceAndUnwrap("xacc", "Xacc.Build.Project+AssemblyLoader") as AssemblyLoader;
        ICodeModule[] mods = aa.LoadAssemblies(assnames, ServiceHost.Discovery.NetRuntimeRoot);
        AppDomain.Unload(assloader);

        AddReferencesAndGenerateTree(mods);
      }
    }

    #endregion

    #region Event Invokers

    /// <summary>
    /// Fires when file has been added
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="root">the treenode</param>
    protected virtual void OnFileAdded(string filename, TreeNode root)
    {

    }

    /// <summary>
    /// Fires when a file has been removed
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="root">the treenode</param>
    protected virtual void OnFileRemoved(string filename, TreeNode root)
    {

    }

    /// <summary>
    /// Fires when project is opened
    /// </summary>
    internal void OnOpened()
    {
      DeserializeProjectData();
      fsw.EnableRaisingEvents = true;
      if (Opened != null)
      {
        Opened(this, null);
      }
    }

    /// <summary>
    /// Fires when Project has been created
    /// </summary>
    internal void ProjectCreated()
    {
      foreach (Type t in GetOptionActionTypes())
      {
        optionnodes.Add(t, t);
      }
      rootnode.Text = ProjectName;
    }
    #endregion

    #region Misc

    /// <summary>
    /// Closes the project
    /// </summary>
 	  public void Close()
		{
      fsw.EnableRaisingEvents = false;
      fsw.Dispose();
			if (Closed != null)
			{
				Closed(this, null);
			}
		}

    void BuildThreaded(object state)
    {
      Build();
    }

    /// <summary>
    /// Builds the project
    /// </summary>
    /// <returns>true if success</returns>
    public bool Build()
    {
      ServiceHost.Error.ClearErrors(this);

      foreach (string file in Sources)
      {
        if (ServiceHost.File.IsDirty(file))
        {
          AdvancedTextBox atb = ServiceHost.File[file] as AdvancedTextBox;
          if (atb != null)
          {
            atb.SaveFile(file);
          }
        }
      }

      SortActions();

      foreach (Action a in Actions)
      {
        CustomAction ca = a as CustomAction;

        if (ca != null)
        {
//          if (ca.OutputOption != null)
//          {
//            string of = ca.Output;
//            if (of == null || of == Path.GetFileName(of))
//            {
//              ca.Output = OutputLocation + Path.DirectorySeparatorChar + of;
//            }
//          }
          if (!ca.Invoke(ca.Input))
          {
            ServiceHost.Error.OutputErrors( this, new ActionResult(ActionResultType.Error,0, 
              ProjectName + " : Build failed", GetRelativeFilename(Location)));
            (ServiceHost.Error as ErrorService).tbp.Show();
            return false;
          }
        }
      }

      ServiceHost.Error.OutputErrors(this, new ActionResult(ActionResultType.Ok,0, 
        ProjectName + " : Build succeeded", GetRelativeFilename(Location)));
      return true;
    }
		

    #endregion

    #region Source / file

    /// <summary>
    /// Gets a relative filename
    /// </summary>
    /// <param name="filename">the file path</param>
    /// <returns>the relative path</returns>
    public string GetRelativeFilename(string filename)
    {
      return Normalize(Path.GetFullPath(filename)).Replace(rootdir, string.Empty).TrimStart(Path.DirectorySeparatorChar);
    }

    static string Normalize(string filename)
    {
      if (filename.Length > 1)
      {
        if (filename[1] == ':')
        {
          return char.ToLower(filename[0])+filename.Substring(1);
        }
      }
      return filename;
    }

    /// <summary>
    /// Opens a file, and associates the file with the project
    /// </summary>
    /// <param name="relfilename">the filename</param>
    /// <returns>the Control hosting the file</returns>
    public Control OpenFile(string relfilename)
    {
      if (relfilename == null || relfilename == string.Empty)
      {
        return null;
      }

      relfilename = GetRelativeFilename(relfilename);
      relfilename = Normalize(relfilename);
      string filename = rootdir + Path.DirectorySeparatorChar + relfilename;

      IFileManagerService fms = ServiceHost.File;
      Control c = fms[filename];
      if (c == null)
      {
        c = fms.Open(filename);
        AdvancedTextBox atb = c as AdvancedTextBox;
        if (atb != null)
        {
          atb.ProjectHint = this;
          if (!data.openfiles.Contains(relfilename))
          {
            data.openfiles.Add(relfilename);
          }
          if (data.pairings.ContainsKey(relfilename))
          {
            ArrayList ppp = data.pairings[relfilename] as ArrayList; 
            atb.LoadPairings(ppp);
          }
        }
      }
      return c;
    }

    public void CloseFile(string filename)
    {
      filename = GetRelativeFilename(filename);
      if (data.openfiles.Contains(filename))
      {
        data.openfiles.Remove(filename);
      }
    }

    /// <summary>
    /// Adds a file to the project
    /// </summary>
    /// <param name="filename">the filename</param>
		public void AddFile(string filename)
		{
			string ext = Path.GetExtension(filename).TrimStart('.');
      AddFile(filename, SuggestAction(ext));
		}

    /// <summary>
    /// Adds a file to the project
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="action">the Action to associate the file with</param>
		public void AddFile(string filename, Action action)
		{
			AddFile(filename, action, false);
		}

    /// <summary>
    /// Adds a file to the project
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="action">the Action to associate the file with</param>
    /// <param name="select">whether the file should be made active</param>
    public void AddFile(string filename, Action action, bool select)
    {
      int i = filename.LastIndexOf("*.");
      if (i >= 0)
      {
        CustomAction ca = action as CustomAction;
        ca.Input = null;
        string pattern = filename.Substring(i);
        foreach (string file in Directory.GetFiles(i == 0 ? "." : filename.Substring(0, i), pattern))
        {
          AddFile(file, SuggestAction(action.GetType()));
        }
      }
      else
      {
        string oldfilename = filename;
        filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        filename = Path.GetFullPath(filename);
        filename = Normalize(filename);

        if (sources.ContainsKey(filename))
        {
          if (!(action is OptionAction))
          {
            MessageBox.Show(ServiceHost.Window.MainForm, "Project already contains file: " + filename, 
              "Error!", 0,MessageBoxIcon.Error);
          }
          return;
        }

        if (action != null)
        {
          if (action is CustomAction)
          {
            CustomAction pa = action as CustomAction;
            if (pa.Input != null)
            {
              if (Array.IndexOf(pa.Input, GetRelativeFilename(filename)) < 0)
              {
                ArrayList l = new ArrayList(pa.Input);
                l.Add(GetRelativeFilename(filename));
                pa.Input = l.ToArray(typeof(string)) as string[];
              }
            }
            else
            {
              pa.Input = new string[] { GetRelativeFilename(filename) };
            }
          }
          if (action is OptionAction)
          {
            OptionAction oa = action as OptionAction;
            string[] vals = oa.GetOption();
            if (vals == null || vals.Length == 0)
            {
              oa.SetOption(oldfilename);
              rootnode.Nodes.Insert(0, oa.OptionNode);
            }
            else
            {
              if (Array.IndexOf(vals, oldfilename) < 0)
              {
                ArrayList l = new ArrayList(vals);
                l.Add(oldfilename);
                oa.SetOption(l.ToArray(typeof(string)) as string[]);
              }
              else
              {
                // just reset the dam thing!
                if (!rootnode.Nodes.Contains(oa.OptionNode))
                {
                  rootnode.Nodes.Insert(0, oa.OptionNode);
                }
                oa.SetOption(vals);
              }
            }
          }
        }

        sources.Add(filename, action);

        if (Array.IndexOf(actions, action) < 0)
        {
          Action[] a = new Action[actions.Length + 1];
          Array.Copy(actions,a, actions.Length);
          a[actions.Length] = action;
          actions = a;
        }

        if (action is OptionAction)
        {
          return; // bye bye!
        }

        TreeNode root = rootnode;
				
        string[] reldirs = (Path.GetDirectoryName(filename) 
          + Path.DirectorySeparatorChar).Replace(rootdir, string.Empty).Trim(Path.DirectorySeparatorChar)
          .Split(Path.DirectorySeparatorChar);

        for (int j = 0; j < reldirs.Length; j++)
        {
          if (reldirs[j] != string.Empty)
          {
            TreeNode sub = FindNode(reldirs[j], root);
            if (sub == null)
            {
              root.Nodes.Add( sub = new TreeNode(reldirs[j],1,1) );
            }
            root = sub;
          }
        }
        
        root = root.Nodes.Add(Path.GetFileName(filename));
        root.Tag = filename;

        if (select)
        {
          root.TreeView.SelectedNode = root;
        }

        string ext = Path.GetExtension(filename).TrimStart('.');

        if (action != null)
        {
          root.SelectedImageIndex = root.ImageIndex = action.ImageIndex;
        }

        OnFileAdded(filename, root);

        root.Expand();
        root.EnsureVisible();
      }
    }

    static TreeNode FindNode(string name, TreeNode parent)
    {
      foreach (TreeNode child in parent.Nodes)
      {
        if (child.Text == name)
        {
          return child;
        }
      }
      return null;
    }

    /// <summary>
    /// Removes a file from the project
    /// </summary>
    /// <param name="filename">the filename to remove</param>
		public void RemoveFile(string filename)
		{
			filename = Path.GetFullPath(filename);
      filename = Normalize(filename);
      string relfile = GetRelativeFilename(filename);
      CustomAction ca = sources[filename] as CustomAction;
      if (ca != null)
      {
        ArrayList l = new ArrayList(ca.Input);
        
        l.Remove(relfile);
        ca.Input = l.ToArray(typeof(string)) as string[];
      }
      OptionAction oa = sources[filename] as OptionAction;
      if (oa != null)
      {
        string[] v = oa.GetOption();

        if (v == null || v.Length == 0)
        {

        }
        else
        {
          ArrayList l = new ArrayList(v);
        
          l.Remove(relfile);
          oa.SetOption(l.ToArray(typeof(string)) as string[]);
        }
      }

      if (data.pairings.ContainsKey(relfile))
      {
        data.pairings.Remove(relfile);
      }

      sources.Remove(filename);
      OnFileRemoved(filename, null);
		}


    #endregion

    #region Event Handling

#if TRACE
    void TraceSaved(object p, ProjectEventArgs e)
    {
      Trace.WriteLine(string.Format("Saved({0})",(p as Project).ProjectName), "Project");
    }
    void TraceOpened(object p, ProjectEventArgs e)
    {
      Trace.WriteLine(string.Format("Opened({0})",(p as Project).ProjectName), "Project");
    }
    void TraceClosed(object p, ProjectEventArgs e)
    {
      Trace.WriteLine(string.Format("Closed({0})",(p as Project).ProjectName), "Project");
    }
#endif

    internal void ShowProps(object sender, EventArgs e)
    {
      ProcessActionDialog pad = new ProcessActionDialog(this);
      pad.ShowDialog(ServiceHost.Window.MainForm);
    }

    internal void BuildProject(object sender, EventArgs e)
    {
      System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback(BuildThreaded));
    }

    internal void NewFile(object sender, EventArgs e)
    {
      NewFileWizard wiz = new NewFileWizard();
	
      Hashtable lnames = new Hashtable();

      foreach (Type l in ActionTypes)
      {
        lnames.Add(l.Name, l);
      }

      ArrayList ll = new ArrayList(lnames.Keys);
      ll.Sort();

      foreach (string lname in ll)
      {
        wiz.prjtype.Items.Add(lnames[lname]);
      }

      RESTART:

        if (wiz.ShowDialog(ServiceHost.Window.MainForm) == DialogResult.OK)
        {
          Type t = wiz.prjtype.SelectedItem as Type;
          Action a = SuggestAction(t);
          string fn = wiz.name.Text;
          string path = wiz.loc.Text.Trim();

          if (path == string.Empty)
          {
            path = Environment.CurrentDirectory;
          }

          string fullpath = path + Path.DirectorySeparatorChar + fn;

          if (Path.GetExtension(fullpath) == string.Empty)
          {
            CustomAction ca = a as CustomAction;
            if (ca != null)
            {
              fullpath += ("." + ca.InputExtension[0]);
            }
          }

          bool overwrite = true;

          if (File.Exists(fullpath))
          {
            switch ( MessageBox.Show(ServiceHost.Window.MainForm, "File already exists. Overwrite?", "Confirmation",
              MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
            {
              case DialogResult.Yes:
                overwrite = true;
                break;
              case DialogResult.No:
                overwrite = false;
                break;
              case DialogResult.Cancel:
                goto RESTART;
            }
          }

          if (overwrite)
          {
            using (StreamWriter w = new StreamWriter(File.Create(fullpath)))
            {
              string ext = Path.GetExtension(fullpath).TrimStart('.');
              w.WriteLine("");
              w.Flush();
            }

            AddFile(fullpath, a);

            ServiceHost.File.Open(fullpath);
          }
        }
    }

    internal void ExistingFile(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.InitialDirectory = Environment.CurrentDirectory;
      ofd.CheckFileExists = true;
      ofd.CheckPathExists = true;
      ofd.Multiselect = true;
      ofd.DereferenceLinks = true;
      ofd.RestoreDirectory = true;

      StringBuilder sb = new StringBuilder();
      StringBuilder ex = new StringBuilder();
      StringBuilder ab = new StringBuilder();

      string defext = DefaultExtension;
      Hashtable actions = new Hashtable();

      int count = 0;

      ab.Append("All supported files|");

      foreach (Type act in ActionTypes)
      {
        string[] extss = Xacc.Build.InputExtensionAttribute.GetExtensions(act);

        if (extss.Length > 0)
        {
          if (extss[0] != "*")
          {
            ex.AppendFormat("*.{0}", extss[0]);
            ab.AppendFormat("*.{0};", extss[0]);
          }

          for(int i = 1; i < extss.Length; i++)
          {
            if (extss[i] != "*")
            {
              ex.AppendFormat(";*.{0}", extss[i]);
              ab.AppendFormat("*.{0};", extss[i]);
            }
          }

          if (ex.Length > 0)
          {
            count++;
            sb.AppendFormat("{0} ({1})|{1}|", NameAttribute.GetName(act), ex);
            ex.Length = 0;
          }
        }
      }
      
      ab.Length--;
      ab.Append("|");
      sb.Append("Text files (*.txt)|*.txt|");
      sb.Append("All files (*.*)|*.*");

      ofd.Filter = ab.ToString() + sb.ToString();

      if (ofd.ShowDialog() == DialogResult.OK)
      {
        foreach (string file in ofd.FileNames)
        {
          AddFile(file);
        }
      }
    }

    internal void RunProject(object sender, EventArgs e)
    {
      foreach (Action a in Actions)
      {
        ProcessAction pa = a as ProcessAction;
        if (pa != null)
        {
          Option o = pa.OutputOption;
          if (o != null)
          {
            string outfile = pa.GetOptionValue(o) as string;

            if (outfile == null || outfile == string.Empty)
            {
              MessageBox.Show(ServiceHost.Window.MainForm, "No output specified.\nPlease specify an output file in the project properties",
                "Error", 0, MessageBoxIcon.Error);
              return;
            }

            outfile = rootdir + Path.DirectorySeparatorChar + outfile;

            if (Path.GetExtension(outfile) == ".exe")
            {

              bool rebuild = false;

              if (File.Exists(outfile))
              {
                DateTime build = File.GetLastWriteTime(outfile);
                foreach (string file in Sources)
                {
                  if (File.Exists(file))
                  {
                    if (File.GetLastWriteTime(file) > build || ServiceHost.File.IsDirty(file))
                    {
                      rebuild = true;
                      break;
                    }
                  }
                }
              }
              else
              {
                rebuild = true;
              }

              if (rebuild && !Build())
              {
                MessageBox.Show(ServiceHost.Window.MainForm, string.Format("Build Failed: Unable to run: {0}",
                  outfile), "Error", 0, MessageBoxIcon.Error);
                return;
              }

              try
              {
                Process.Start(outfile);
              }
              catch (Exception ex)
              {
                MessageBox.Show(ServiceHost.Window.MainForm, string.Format("Error running: {0}\nError: {1}",
                  outfile, ex.GetBaseException().Message), "Error", 0, MessageBoxIcon.Error);
              }
              return;		
            }
          }
        }
      }
    }

    internal void DebugProject(object sender, EventArgs e)
    {
      if (((MenuItem)sender).Checked)
      {
        ServiceHost.Debug.Exit();
      }
      else
      {
        ServiceHost.Debug.Start(this);
      }
    }



    #endregion

  }
}
