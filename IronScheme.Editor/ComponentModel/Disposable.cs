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

using System;
using System.Collections;
using System.Diagnostics;

namespace IronScheme.Editor.ComponentModel
{
	/// <summary>
	/// Base class for disposable objects
	/// </summary>
	public abstract class Disposable : IDisposable
	{
    bool disposed = false;

    /// <summary>
    /// Fires when object is about to be disposed
    /// </summary>
    public event EventHandler Disposing;

    /// <summary>
    /// Fires when object has been disposed
    /// </summary>
    public event EventHandler Disposed;

    /// <summary>
    /// Disposes the object
    /// </summary>
    public void Dispose()
    {
      InvokeDispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~Disposable()
    {
      InvokeDispose(false);
    }

    /// <summary>
    /// Gets whether object has been disposed
    /// </summary>
    protected bool IsDisposed
    {
      get {return disposed;}
    }

    void InvokeDispose(bool disposing)
    {
      if (!disposed)
      {
        if (Disposing != null)
        {
          Disposing(this, EventArgs.Empty);
        }
        Dispose(disposing);
        disposed = true;
        if (Disposed != null)
        {
          Disposed(this, EventArgs.Empty);
        }
      }
    }

    /// <summary>
    /// Called when object is disposed
    /// </summary>
    /// <param name="disposing">true is Dispose() was called</param>
    protected virtual void Dispose(bool disposing)
    {
    }

  }


  public abstract class RemoteDisposable : MarshalByRefObject, IDisposable
  {
     bool disposed = false;

    /// <summary>
    /// Fires when object is about to be disposed
    /// </summary>
    public event EventHandler Disposing;

    /// <summary>
    /// Fires when object has been disposed
    /// </summary>
    public event EventHandler Disposed;

    /// <summary>
    /// Disposes the object
    /// </summary>
    public void Dispose()
    {
      InvokeDispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~RemoteDisposable()
    {
      InvokeDispose(false);
    }

    /// <summary>
    /// Gets whether object has been disposed
    /// </summary>
    protected bool IsDisposed
    {
      get {return disposed;}
    }

    void InvokeDispose(bool disposing)
    {
      if (!disposed)
      {
        if (Disposing != null)
        {
          Disposing(this, EventArgs.Empty);
        }
        Dispose(disposing);
        disposed = true;
        if (Disposed != null)
        {
          Disposed(this, EventArgs.Empty);
        }
      }
    }

    /// <summary>
    /// Called when object is disposed
    /// </summary>
    /// <param name="disposing">true is Dispose() was called</param>
    protected virtual void Dispose(bool disposing)
    {
    }

  }
}
