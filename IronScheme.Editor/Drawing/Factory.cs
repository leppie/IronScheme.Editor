#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion


using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

namespace IronScheme.Editor.Drawing
{
  /// <summary>
  /// Provides reusable disposable objects
  /// </summary>
  sealed class Factory
  {
    sealed class Container
    {
      readonly object[] stuff;
      readonly int hash;

      public Container(object[] args)
      {
        this.stuff = args;
        hash = CalcHash(args);
      }

      public override bool Equals(object obj)
      {
        Container two = (Container)obj;

        if (two.stuff.Length != stuff.Length)
        {
          return false;
        }

        for (int i = 0; i < stuff.Length; i++)
        {
          if (!stuff[i].Equals(two.stuff[i]))
          {
            return false;
          }
        }

        return true;
      }

      public override int GetHashCode()
      {
        return hash;
      }

      static int CalcHash(object[] stuff)
      {
        int l = 32 / stuff.Length;
        int hash = 0;

        for (int i = 0; i < stuff.Length; i++)
        {
          hash ^= stuff[i].GetHashCode() << (l * i);
        }
        return hash;
      }
    }

    static Dictionary<Type, Hashtable> typecache = new Dictionary<Type, Hashtable>();

#if DEBUG
    static int hit = 0;
    static int miss = 0;
    static int total = 0;
#endif

    Factory() { }

    public static T Get<T>(params object[] args) where T : class
    {
      return Get(typeof(T), args) as T;
    }

    /// <summary>
    /// Returns an object of type and constructor args and caches the reference
    /// </summary>
    /// <param name="type">the Type of the object to create</param>
    /// <param name="args">parameters of the constructor</param>
    /// <returns>a newly created or cached reference</returns>
    public static object Get(Type type, params object[] args)
    {
      Hashtable bin = null;
      if (!typecache.ContainsKey(type))
      {
        bin = new Hashtable();
        typecache[type] = bin;
      }
      else
      {
        bin = typecache[type];
      }

      object c = null;

      if (args.Length == 1)
      {
        c = args[0];
      }
      else
      {
        c = new Container(args);
      }

      object obj = bin[c];

      TRYAGAIN:
#if DEBUG
      total++;
#endif

      if (obj == null)
      {
#if DEBUG
        miss++;
#endif
#if CHECKED
					Console.Write("Creating: {0} ( ", type.Name);
					foreach (object o in args)
					{
						Console.Write("{0} ", o);
					}
					Console.WriteLine(")");
#endif
        obj = Activator.CreateInstance(type, args);
        bin[c] = obj;
      }
#if DEBUG
      else
      {
        hit++;
      }
#endif
      try
      {
        int i = obj.GetHashCode();
      }
      catch (ObjectDisposedException)
      {
        System.Diagnostics.Trace.WriteLine("Objects created in the factory should not be disposed");
        obj = null;
        goto TRYAGAIN;
      }
      return obj;
    }

    /// <summary>
    /// Gets a SolidBrush based on Color
    /// </summary>
    /// <param name="c">The color of the brush</param>
    /// <returns>the brush</returns>
    public static SolidBrush SolidBrush(Color c)
    {
      return Get<SolidBrush>(c);
    }

    /// <summary>
    /// Returns a font based on another font
    /// </summary>
    /// <param name="proto">the base font</param>
    /// <param name="style">the new style</param>
    /// <returns>the new font</returns>
    public static Font Font(Font proto, FontStyle style)
    {
      if (proto.Style == style)
      {
        return proto;
      }
      return Get<Font>(proto, style);
    }

    /// <summary>
    /// Returns a new Pen based on color and width
    /// </summary>
    /// <param name="color">the color of the pen</param>
    /// <param name="width">the width of the pen</param>
    /// <returns>the new pen</returns>
    public static Pen Pen(Color color, float width)
    {
      return Get<Pen>(color, width);
    }
  }
}
