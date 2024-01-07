using System.Collections.Generic;
using Morph.Lib;

namespace Morph.Endpoint
{
  public abstract class LinkMember : Link
  {
    public LinkMember(LinkType LinkType)
      : base(LinkType)
    {
    }

    public abstract string Name
    {
      get;
    }

    #region Link

    public override void Action(LinkMessage Message)
    {
      //  In this implementation we let LinkApartment do the work,
      //  so this Action should not be called, meaning that there's a mistake somewhere.
      throw new EMorph("Unexpected link type");
    }

    #endregion

    protected LinkStack DevicePathOf(LinkStack Path)
    {
      if (Path == null)
        return null;
      List<Link> Links = Path.ToLinks();
      for (int i = Links.Count - 1; i >= 0; i--)
      {
        Link Link = Links[i];
        if ((Link is LinkApartment) ||
            (Link is LinkApartmentProxy) ||
            (Link is LinkService) ||
            (Link is LinkMember) ||
            (Link is LinkData))
          Links.RemoveAt(i);
      }
      return new LinkStack(Links);
    }

    protected internal abstract LinkData Invoke(LinkMessage Message, LinkStack SenderPath, Apartment Apartment, Servlet Servlet, LinkData DataIn);

    public override bool Equals(object obj)
    {
      return (obj is LinkMember) && (((LinkMember)obj).Name.ToLower().Equals(Name.ToLower()));
    }

    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }
  }

  public class LinkTypeMember : LinkType
  {
    static internal LinkTypeMember instance = new LinkTypeMember();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Member; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool IsProperty, IsSet, HasIndex;
      Reader.ReadLinkByte(out IsProperty, out IsSet, out HasIndex);
      string Name = Reader.ReadString();
      if (IsProperty)
        return new LinkProperty(Name, IsSet, HasIndex);
      else
        return new LinkMethod(Name);
    }

    #endregion
  }
}