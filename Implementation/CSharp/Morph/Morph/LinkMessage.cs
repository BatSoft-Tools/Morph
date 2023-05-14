using Morph.Lib;
#if LOG_MESSAGES
using Bat.Library.Logging;
#endif

namespace Morph
{
  public class LinkMessage : Link
  {
    public LinkMessage(LinkStack PathTo, LinkStack PathFrom)
      : base(LinkTypeMessage.instance)
    {
      fPathTo = PathTo;
      fPathFrom = PathFrom;
    }

    internal protected LinkMessage(StreamReader Reader)
      : base(LinkTypeMessage.instance)
    {
      Read(Reader);
    }

    private LinkMessage(bool HasCallNumber, int CallNumber, EMorph Error, LinkStack PathTo)
      : base(LinkTypeMessage.instance)
    {
      fHasCallNumber = HasCallNumber;
      fCallNumber = CallNumber;
      fHasErrorNumber = true;
      fErrorNumber = Error.ErrorNumber;
      fPathTo = PathTo;
      fPathFrom = null;
    }

    private bool fHasCallNumber = false;
    public bool HasCallNumber
    {
      get { return fHasCallNumber; }
    }

    private int fCallNumber;
    public int CallNumber
    {
      get
      {
        if (!fHasCallNumber)
          throw new EMorphImplementation();
        return fCallNumber;
      }
      set
      {
        fCallNumber = value;
        fHasCallNumber = true;
      }
    }

    private bool fHasErrorNumber = false;
    public bool HasErrorNumber
    {
      get { return fHasErrorNumber; }
    }

    private int fErrorNumber;
    public int ErrorNumber
    {
      get
      {
        if (!fHasErrorNumber)
          throw new EMorphImplementation();
        return fErrorNumber;
      }
      set
      {
        fErrorNumber = value;
        fHasErrorNumber = true;
      }
    }

    private LinkStack fPathTo = null;
    public LinkStack PathTo
    {
      get { return fPathTo; }
    }

    public bool HasPathFrom
    {
      get { return fPathFrom != null; }
    }

    private LinkStack fPathFrom = null;
    public LinkStack PathFrom
    {
      get { return fPathFrom; }
    }

    public void ActionNext()
    {
      try
      { //  Action the next link
        Link Next = PathTo.Peek();
#if LOG_MESSAGES
        Log.Default.Add(LinkTypes.AppName);
        Log.Default.Add(this);
        Log.Default.Add(Log.nl);
#endif
        if (Next != null)
          Next.Action(this);
      }
      catch (EMorph x)
      {
#if LOG_MESSAGES
        Log.Default.Add(LinkTypes.AppName);
        Log.Default.Add(x);
        Log.Default.Add(Log.nl);
#endif
        //  If there's no return path, then can't return an error message
        if (!HasPathFrom)
          return;
        //  If we get an error in an error message, then don't know what to do with it.
        if (fHasErrorNumber)
          throw new EMorphImplementation();
        //  Rearrange the message to become an error message
        (new LinkMessage(fHasCallNumber, fCallNumber, x, PathFrom)).Action(null);
      }
    }

    public void MoveToNextLink()
    {
      if (fPathFrom != null)
        fPathFrom.Push(fPathTo.Pop());
      else
        fPathTo.Pop();
    }

    private void Read(StreamReader Reader)
    {
      //  Read link byte
      bool HasPathFrom;
      Reader.ReadLinkByte(out fHasCallNumber, out fHasErrorNumber, out HasPathFrom);
      //  Read link values
      //  - CallNumber
      if (fHasCallNumber)
        fCallNumber = Reader.ReadInt32();
      //  - ErrorNumber
      if (fHasErrorNumber)
        fErrorNumber = Reader.ReadInt32();
      //  - PathTo size
      int PathToSize = Reader.ReadInt32();
      //  - PathFrom size
      int PathFromSize = 0;
      if (HasPathFrom)
        PathFromSize = Reader.ReadInt32();
      //  Paths
      fPathTo = new LinkStack(Reader.SubReader(PathToSize));
      if (HasPathFrom)
        fPathFrom = new LinkStack(Reader.SubReader(PathFromSize));
    }

    #region Link

    public override int Size()
    {
      long TotalSize = 1;
      //  Call number
      if (HasCallNumber)
        TotalSize += 4;
      //  Error nomber
      if (HasErrorNumber)
        TotalSize += 4;
      //  Path To size
      TotalSize += 4 + PathTo.ByteSize;
      //  Path From size
      if (HasPathFrom)
        TotalSize += 4 + PathFrom.ByteSize;
      //  Analyze limits
      if (TotalSize > int.MaxValue)
        throw new EMorph("Message is too large");
      return (int)TotalSize;
    }

    public override void Write(StreamWriter Writer)
    {
      lock (Writer)
      {
        //  Message link byte
        Writer.WriteLinkByte((byte)LinkType.ID, fHasCallNumber, fHasErrorNumber, HasPathFrom);
        //  Call number
        if (fHasCallNumber)
          Writer.WriteInt32(CallNumber);
        //  Error number
        if (fHasErrorNumber)
          Writer.WriteInt32(ErrorNumber);
        //  Path to size
        Writer.WriteInt32(PathTo.ByteSize);
        //  Path from size
        if (HasPathFrom)
          Writer.WriteInt32(PathFrom.ByteSize);
        //  Path to
        PathTo.Write(Writer);
        PathTo.MoveToEnd();
        //  Path from
        if (HasPathFrom)
          PathFrom.Write(Writer);
      }
    }

    public override void Action(LinkMessage Message)
    {
      //  This check prevents LinkMessage's from being nested
      if (Message != null)
        throw new EMorph("Message link not expected inside a message");
      //  Action the next link in line
      ActionNext();
    }

    #endregion

    public LinkMessage Clone()
    {
      LinkMessage clone = new LinkMessage(PathTo, PathFrom);
      clone.fHasCallNumber = this.fHasCallNumber;
      clone.fCallNumber = this.fCallNumber;
      clone.fHasErrorNumber = this.fHasErrorNumber;
      clone.fErrorNumber = this.fErrorNumber;
      if (this.fPathTo != null)
        clone.fPathTo = this.fPathTo.Clone();
      if (this.fPathFrom != null)
        clone.fPathFrom = this.fPathFrom.Clone();
      return clone;
    }

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
      string str = "{Message ";
      if (HasCallNumber)
        str += " CallNumber=" + CallNumber.ToString();
      if (HasErrorNumber)
        str += " ErrorNumber=" + ErrorNumber.ToString();
      str += " To=" + PathTo.AsString();
      if (HasPathFrom)
        str += " From=" + PathFrom.AsString();
      return str + '}';
    }
  }

  public class LinkTypeMessage : LinkType
  {
    static internal LinkTypeMessage instance = new LinkTypeMessage();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Message; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      return new LinkMessage(Reader);
    }

    #endregion
  }
}