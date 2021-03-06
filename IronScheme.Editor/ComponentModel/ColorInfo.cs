#region License
/* Copyright (c) 2003-2015 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See license.txt. */
#endregion


using System.Drawing;

namespace IronScheme.Editor.ComponentModel
{
  /// <summary>
  /// Define color and font info for token types
  /// </summary>
	public struct ColorInfo
	{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:ColorInfo"/> class.
    /// </summary>
    /// <param name="forecolor">The forecolor.</param>
    /// <param name="backcolor">The backcolor.</param>
    /// <param name="bordercolor">The bordercolor.</param>
    /// <param name="style">The style.</param>
    public ColorInfo(Color forecolor, Color backcolor, Color bordercolor, FontStyle style)
    {
      this.BorderColor = bordercolor.Name == "0" ? Color.Empty : bordercolor;
      this.ForeColor = forecolor;
      this.BackColor = backcolor.Name == "0" ? Color.Empty : backcolor;
      this.Style = style;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:ColorInfo"/> class.
    /// </summary>
    /// <param name="forecolor">The forecolor.</param>
    /// <param name="backcolor">The backcolor.</param>
    /// <param name="style">The style.</param>
    public ColorInfo(Color forecolor, Color backcolor, FontStyle style)
    {
      this.BorderColor = Color.Empty;
      this.ForeColor = forecolor;
      this.BackColor = backcolor;
      this.Style = style;
    }
    /// <summary>
    /// The style to use
    /// </summary>
		public FontStyle		Style;		

    /// <summary>
    /// The foreground color to use
    /// </summary>
		public Color				ForeColor;

    /// <summary>
    /// The background color to use
    /// </summary>
		public Color				BackColor;

    /// <summary>
    /// The border color to use
    /// </summary>
    public Color BorderColor;
	
    /// <summary>
    /// Represents an empty ColorInfo
    /// </summary>
		public static readonly ColorInfo Empty = new ColorInfo(Color.Empty, Color.Empty, 0);

    /// <summary>
    /// Represents an invalid ColorInfo
    /// </summary>
    public static readonly ColorInfo Invalid = new ColorInfo(Color.Empty, Color.Empty, 0);

		static ColorInfo()
		{
			Invalid.BackColor = Color.LemonChiffon;
			Invalid.ForeColor = Color.Red;
		}

#if DEBUG
		public override string ToString()
		{
			return string.Format("{0}", ForeColor.Name);
		}
#endif
	}
}


