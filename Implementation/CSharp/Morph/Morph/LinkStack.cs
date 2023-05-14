using System.Collections.Generic;
using Morph.Lib;

namespace Morph
{
  public class LinkStack
  {
    public LinkStack()
    {
      fLinks = new List<Link>();
      fReader = null;
    }

    public LinkStack(StreamReaderSized Reader)
    {
      fLinks = new List<Link>();
      fReader = Reader;
    }

    public LinkStack(byte[] bytes)
    {
      fLinks = new List<Link>();
      fReader = new StreamReaderSized(bytes);
    }

    public LinkStack(List<Link> Links)
    {
      fLinks = Links;
      fReader = null;
    }

    private List<Link> fLinks;
    private StreamReaderSized fReader;

    private List<Link> CloneList(List<Link> SourceList)
    {
      List<Link> result = new List<Link>();
      for (int i = 0; i < SourceList.Count; i++)
        result.Add(SourceList[i]);
      return result;
    }

    public void PeekAll()
    {
      if (fReader != null)
        while (fReader.CanRead)
          fLinks.Insert(0, LinkTypes.ReadLink(fReader));
    }

    internal void MoveToEnd()
    {
      if (fReader != null)
        fReader.MoveToEnd();
    }

    public Link Peek()
    {
      if (fLinks.Count == 0)
      { //  Read next link
        if (fReader == null)
          return null;
        Link link = LinkTypes.ReadLink(fReader);
        if (link == null)
          return null;
        //  Add it
        fLinks.Add(link);
      }
      //  Return the top of the stack
      return fLinks[fLinks.Count - 1];
    }

    public Link Pop()
    {
      Link Link = Peek();
      if (fLinks.Count > 0)
        fLinks.RemoveAt(fLinks.Count - 1);
      return Link;
    }

    public void Push(Link Link)
    {
      if (Link != null)
        fLinks.Add(Link);
    }

    public void Push(LinkStack Stack)
    {
      if (Stack == null)
        return;
      //  Decode all the links
      Stack.PeekAll();
      //  Add the links to Stack
      for (int i = 0; i < Stack.fLinks.Count; i++)
        fLinks.Add(Stack.fLinks[i]);
    }

    public void Append(Link Link)
    {
      //  Decode all the links
      PeekAll();
      //  Add the link to Stack
      if (Link != null)
        fLinks.Insert(0, Link);
    }

    public void Append(LinkStack Stack)
    {
      //  Decode all the links
      Stack.PeekAll();
      //  Add the links to Stack
      for (int i = 0; i < Stack.fLinks.Count; i++)
        fLinks.Insert(i, Stack.fLinks[i]);  
    }

    public int ByteSize
    {
      get
      {
        int result = 0;
        for (int i = fLinks.Count - 1; 0 <= i; i--)
        {
          int size = fLinks[i].Size();
          result += size;
        }
        if (fReader != null)
          result += fReader.Remaining;
        return result;
      }
    }

    public void Write(StreamWriter Writer)
    {
      for (int i = fLinks.Count - 1; 0 <= i; i--)
        fLinks[i].Write(Writer);
      if (fReader != null)
        fReader.WriteTo(Writer);
    }

    public List<Link> ToLinks()
    {
      PeekAll();
      return CloneList(fLinks);
    }

    public LinkStack Clone()
    {
      PeekAll();
      return new LinkStack(CloneList(fLinks));
    }

    public LinkStack Reverse()
    {
      List<Link> Links = ToLinks();
      List<Link> Reverse = new List<Link>();
      for (int i = Links.Count - 1; i >= 0; i--)
        Reverse.Add(Links[i]);
      return new LinkStack(Reverse);
    }

    public string AsString()
    {
      string str = "(";
      for (int i = fLinks.Count - 1; 0 <= i; i--)
        str += fLinks[i].ToString();
      if ((fReader != null) && (fReader.CanRead))
        str += "...[" + fReader.Remaining.ToString() + " B]";
      return str + ')';
    }

    #region Object overrides

    public override bool Equals(object obj)
    {
      if (!(obj is LinkStack))
        return false;
      LinkStack Other = (LinkStack)obj;
      //  Convert binary to objects
      this.PeekAll();
      Other.PeekAll();
      //  Compare the objects that make up the paths
      if (this.fLinks.Count != Other.fLinks.Count)
        return false;
      for (int i = fLinks.Count - 1; i >= 0; i--)
        if (!fLinks[i].Equals(Other.fLinks[i]))
          return false;
      return true;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      PeekAll();
      string str = null;
      for (int i = fLinks.Count - 1; 0 <= i; i--)
        str += "\r\n" + fLinks[i].ToString();
      return str;
    }

    #endregion
  }
}