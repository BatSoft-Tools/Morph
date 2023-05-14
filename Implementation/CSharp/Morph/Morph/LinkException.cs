using Morph.Lib;

namespace Morph
{
  public class LinkException : Link
  {
    public LinkException(string ClassName, string Message, string StackTrace)
      : base(LinkTypeException.instance)
    {
      fClassName = ClassName;
      fMessage = Message;
      fStackTrace = StackTrace;
    }

    private string fClassName;
    public string ClassName
    {
      get { return fClassName; }
    }

    private string fMessage;
    public string Message
    {
      get { return fMessage; }
    }

    private string fStackTrace;
    public string StackTrace
    {
      get { return fStackTrace; }
    }

    #region Link implementation

    public override int Size()
    {
      int result = 1;
      if (fClassName != null)
        result += Functions.SizeOf(fClassName);
      if (fMessage != null)
        result += Functions.SizeOf(fMessage);
      if (fStackTrace != null)
        result += Functions.SizeOf(fStackTrace);
      return result;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, fClassName != null, fMessage != null, fStackTrace != null);
      if (fClassName != null)
        Writer.WriteString(fClassName);
      if (fMessage != null)
        Writer.WriteString(fMessage);
      if (fStackTrace != null)
        Writer.WriteString(fStackTrace);
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
      string str = "{Exception";
      if (ClassName != null)
        str += " ClassName=" + ClassName;
      if (Message != null)
        str += " Message=" + Message;
      if (StackTrace != null)
        str += " StackTrace=" + StackTrace;
      return str + '}';
    }
  }

  public class LinkTypeException : LinkType
  {
    static internal LinkTypeException instance = new LinkTypeException();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Exception; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool HasClassName, HasMessage, HasStackTrace;
      Reader.ReadLinkByte(out HasClassName, out HasMessage, out HasStackTrace);
      string ClassName = null, Message = null, StackTrace = null;
      if (HasClassName)
        ClassName = Reader.ReadString();
      if (HasMessage)
        Message = Reader.ReadString();
      if (HasStackTrace)
        StackTrace = Reader.ReadString();
      return new LinkException(ClassName, Message, StackTrace);
    }

    #endregion
  }
}