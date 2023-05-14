namespace Morph
{
  public class LinkInformation : Link
  {
    internal LinkInformation()
      : base(LinkTypeInformation.instance)
    {
    }

    #region Link

    public override int Size()
    {
      throw new System.Exception("The method or operation is not implemented.");
    }

    public override void Write(Morph.Lib.StreamWriter Writer)
    {
      throw new System.Exception("The method or operation is not implemented.");
    }

    public override void Action(LinkMessage Message)
    {
      throw new System.Exception("The method or operation is not implemented.");
    }

    #endregion

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      return "{Info}";
    }
  }

  public class LinkTypeInformation : LinkType
  {
    private LinkTypeInformation()
    {
      throw new System.Exception("The method or operation is not implemented.");
      //LinkTypes.Register(this);
    }

    static internal LinkTypeInformation instance = new LinkTypeInformation();

    static public void Register()
    {
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Information; }
    }

    public Link ReadLink(Morph.Lib.StreamReader Reader)
    {
      throw new System.Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}