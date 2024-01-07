using Morph.Lib;

namespace Morph.Endpoint
{
  internal abstract class Reply : RegisterItemID
  {
    public Reply(int ID)
    {
      fID = ID;
    }

    private int fID;

    #region RegisterItemID Members

    public int ID
    {
      get { return fID; }
    }

    #endregion
  }

  internal class ReplyParams : Reply
  {
    public ReplyParams(int ID, object[] Params, object Special)
      : base(ID)
    {
      fParams = Params;
      fSpecial = Special;
    }

    private object[] fParams;
    public object[] Params
    {
      get { return fParams; }
    }

    private object fSpecial;
    public object Special
    {
      get { return fSpecial; }
    }
  }

  internal class ReplyException : Reply
  {
    public ReplyException(int ID, string ClassName, string Message, string StackTrace)
      : base(ID)
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
  }

  internal class ReplyMorphError : Reply
  {
    public ReplyMorphError(int ID, int ErrorNumber, string MorphTrace)
      : base(ID)
    {
      fErrorNumber = ErrorNumber;
      fMorphTrace = MorphTrace;
    }

    private int fErrorNumber;
    public int ErrorNumber
    {
      get { return fErrorNumber; }
    }

    private string fMorphTrace;
    public string MorphTrace
    {
      get { return fMorphTrace; }
    }
  }

  public class Replies
  {
    private RegisterItems<Reply> fReplies = new RegisterItems<Reply>();

    private void AddReply(Reply Reply)
    {
      lock (fReplies)
        fReplies.Add(Reply);
    }

    public void AssignParams(int ID, object[] Params, object Special)
    {
      AddReply(new ReplyParams(ID, Params, Special));
    }

    public void AssignException(int ID, string ClassName, string Message, string StackTrace)
    {
      AddReply(new ReplyException(ID, ClassName, Message, StackTrace));
    }

    public void AssignMorphError(int ID, int ErrorNumber, string MorphTrace)
    {
      AddReply(new ReplyMorphError(ID, ErrorNumber, MorphTrace));
    }

    public object GetParams(int ID, out object[] Params)
    {
      //  Extract the Reply
      Reply Reply;
      lock (fReplies)
      {
        Reply = fReplies.Find(ID);
        fReplies.Remove(ID);
      }
      //  Examine the reply
      if (Reply == null)
        throw new EMorph("Implementation error");
      //  Reply might be a Morph error
      if (Reply is ReplyMorphError)
      {
        ReplyMorphError e = (ReplyMorphError)Reply;
        EMorph.Throw(e.ErrorNumber, e.MorphTrace);
      }
      //  Reply might be an exception
      if (Reply is ReplyException)
      {
        ReplyException x = (ReplyException)Reply;
        throw new EMorphInvocation(x.ClassName, x.Message, x.StackTrace);
      }
      //  Reply.Reply is ReplyParams
      ReplyParams p = (ReplyParams)Reply;
      Params = p.Params;
      return p.Special;
    }

    public void Remove(int ID)
    {
      lock (fReplies)
        fReplies.Remove(ID);
    }
  }
}