#region Copyright (C) 2005 Rob Blackwell & Active Web Solutions.
//
// L Sharp .NET, a powerful lisp-based scripting language for .NET.
// Copyright (C) 2005 Rob Blackwell & Active Web Solutions.
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Library General Public
// License as published by the Free Software Foundation; either
// version 2 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Library General Public License for more details.
// 
// You should have received a copy of the GNU Library General Public
// License along with this library; if not, write to the Free
// Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
//
#endregion


namespace LSharp
{
  /// <summary>
  /// Built in macros
  /// </summary>
  [Macro]
	public sealed class Macros
	{
    Macros(){}

    [Symbol("defun")]
		public static Macro Defun(Environment environment) 
		{
			return (Macro) Runtime.EvalString("(macro (name args &rest body) `(= ,name (fn ,args ,@body)))",environment);
		}

    [Symbol("defmacro")]
		public static Macro DefMacro(Environment environment) 
		{
			return (Macro) Runtime.EvalString("(macro (name args &rest body) `(= ,name (macro ,args ,@body)))",environment);
		}

    [Symbol("listp")]
		public static Macro ListP(Environment environment) 
		{
			return (Macro) Runtime.EvalString("(macro (l) `(eq (typeof LSharp.Cons) (gettype ,l)))",environment);
		}
    

    [Symbol("++")]
    public static Macro Increment(Environment environment) 
    {
      return (Macro) Runtime.EvalString("(macro (x) `(= ,x (+ ,x 1)))",environment);
    }

    [Symbol("--")]
    public static Macro Decrement(Environment environment) 
    {
      return (Macro) Runtime.EvalString("(macro (x) `(= ,x (- ,x 1)))",environment);
    }


	}
}
