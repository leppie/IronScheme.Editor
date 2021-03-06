// Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, QUT 2005
// (see accompanying GPPGcopyright.rtf)


using System.Collections.Generic;
using System.Text;


namespace gpcc
{
  public class ProductionItem
  {
    public Production production;
    public int pos;
    public bool expanded = false;
    public Set<Terminal> LA = null;


    public ProductionItem(Production production, int pos)
    {
      this.production = production;
      this.pos = pos;
    }


    public override bool Equals(object obj)
    {
      ProductionItem item = (ProductionItem)obj;
      return item.pos == pos && item.production == production;
    }

    public override int GetHashCode()
    {
      return production.GetHashCode() + pos;
    }


    public static bool SameProductions(List<ProductionItem> list1, List<ProductionItem> list2)
    {
      if (list1 == list2)
      {
        return true;
      }
      
      if (list1.Count != list2.Count)
      {
        return false;
      }
      
      for (int i = 0; i < list1.Count; i++)
      {
        if (!list1[i].Equals(list2[i]))
        {
          return false;
        }
      }
      return true;
      //foreach (ProductionItem item1 in list1)
      //{
      //  bool found = false;
      //  foreach (ProductionItem item2 in list2)
      //  {
      //    if (item1.Equals(item2))
      //    {
      //      found = true;
      //      break;
      //    }
      //  }
      //  if (!found)
      //    return false;
      //}
      //return true;
    }


    public bool isReduction()
    {
      return pos == production.rhs.Count;
    }

    public string GetDebug()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendFormat("{1}: ", production.num, production.lhs);
      for (int i = 0; i < production.rhs.Count; i++)
      {
        if (i == pos)
          builder.Append(". ");
        builder.AppendFormat("{0} ", production.rhs[i]);
      }

      if (pos == production.rhs.Count)
        builder.Append(".");

      if (LA != null)
        builder.AppendFormat("		{0}", LA);

      return builder.ToString();

    }


    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendFormat("{0} {1}: ", production.num, production.lhs);
      for (int i = 0 ; i < production.rhs.Count ; i++)
      {
        if (i == pos)
          builder.Append(". ");
        builder.AppendFormat("{0} ", production.rhs[i]);
      }

      if (pos == production.rhs.Count)
        builder.Append(".");

      if (LA != null)
        builder.AppendFormat("		{0}", LA);

      return builder.ToString();
    }
  }
}