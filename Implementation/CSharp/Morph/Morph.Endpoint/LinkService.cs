using System;
using System.Reflection;
using Morph.Lib;
using Morph.Sequencing;

namespace Morph.Endpoint
{
  public class LinkService : Link
  {
    protected internal LinkService(string ServiceName)
      : base(LinkTypeService.instance)
    {
      fServiceName = ServiceName;
    }

    private string fServiceName;
    public string ServiceName
    {
      get { return fServiceName; }
    }

    #region Link implementation

    public override int Size()
    {
      return 1 + Functions.SizeOf(fServiceName);
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, false, false, false);
      Writer.WriteString(fServiceName);
    }

    public override void Action(LinkMessage Message)
    {
      Apartment apartment = Services.Obtain(fServiceName).ApartmentFactory.ObtainDefault();
      Message.PathTo.Pop();
      Message.PathTo.Push(new LinkApartment(apartment));
      Message.ActionNext();
    }

    #endregion

    public override bool Equals(object obj)
    {
      return (obj is LinkService) && (((LinkService)obj).fServiceName.ToLower().Equals(fServiceName.ToLower()));
    }

    public override int GetHashCode()
    {
      return fServiceName.GetHashCode();
    }

    public override string ToString()
    {
      return "{Service Name=" + ServiceName + '}';
    }
  }

  public class LinkApartment : Link
  {
    public LinkApartment(int ApartmentID)
      : base(LinkTypeService.instance)
    {
      fApartmentID = ApartmentID;
    }

    public LinkApartment(Apartment Apartment)
      : base(LinkTypeService.instance)
    {
      fApartmentID = Apartment.ID;
      fApartment = Apartment;
    }

    private int fApartmentID = Apartment.DefaultID;
    public int ApartmentID
    {
      get { return fApartmentID; }
    }

    private Apartment fApartment = null;
    public Apartment Apartment
    {
      get
      {
        if (fApartment == null)
          throw new EMorphImplementation();
        return fApartment;
      }
    }

    #region Link implementation

    public override int Size()
    {
      return 5;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, true, false, false);
      Writer.WriteInt32(ApartmentID);
    }

    public override void Action(LinkMessage Message)
    {
      LinkStack ReplyLinks = new LinkStack();
      try
      {
        //  Obtain apartment
        if (fApartment == null)
          fApartment = ApartmentFactory.Obtain(fApartmentID);
        //  If it's a session apartment, then update the path
        Message.MoveToNextLink();
        if ((fApartment is ApartmentSession) && (Message.PathFrom != null))
        {
          Message.PathTo.PeekAll();
          ((ApartmentSession)fApartment).Path = Message.PathFrom.Clone();
        }
        //  Now examine what's next
        Link NextLink = Message.PathTo.Peek();
        //  Might be establishing a connection
        if (NextLink == null)
        {
          //  Apartment may want to start sequences
          fApartment.AppendSequenceLinks(Message.PathFrom);
          //  Send start ack
          SendReply(Message, Message.PathFrom, null, null);
          return;
        }
        //  Next might be a Sequence link
        if (NextLink is LinkSequence)
        {
          Message.PathTo.Pop();
          Message.PathTo.Push(this);
          NextLink.Action(Message);
          return;
        }
        //  Might be ending a connection
        if (NextLink is LinkEnd)
        {
          fApartment.Dispose();
          return;
        }
        //  Obtain servlet
        int ServletID = Servlet.DefaultID;
        if (NextLink is LinkServlet)
        {
          ReplyLinks.Push(NextLink);
          ServletID = ((LinkServlet)Message.PathTo.Pop()).ServletID;
        }
        Servlet servlet = fApartment.Servlets.Find(ServletID);
        if (servlet == null)
          throw new EMorph("Servlet does not exist");
        //  Obtain member (method/property)
        if (Message.PathTo.Peek() is LinkInformation)
          throw new EMorphImplementation();
        if (!(Message.PathTo.Peek() is LinkMember))
          throw new EMorph("Unexpected link type");
        ReplyLinks.Push(Message.PathTo.Peek());
        LinkMember member = (LinkMember)Message.PathTo.Pop();
        //  Get a device path
        LinkStack PathToSender = null;
        if (fApartment is ApartmentSession)
          PathToSender = ((ApartmentSession)fApartment).Path;
        else if (Message.HasPathFrom)
          PathToSender = Message.PathFrom;
        //  Obtain parameters
        LinkData DataIn = null;
        if (Message.PathTo.Peek() is LinkData)
        {
          ReplyLinks.Push(Message.PathTo.Peek());
          DataIn = (LinkData)Message.PathTo.Pop();
        }
        //  Invoke the method
        LinkData DataOut = member.Invoke(Message, PathToSender, fApartment, servlet, DataIn);
        //  Send a reply
        SendReply(Message, null, DataOut, null);
      }
      catch (EMorph x)
      {
        SendReply(Message, ReplyLinks.Reverse(), null, x);
      }
      catch (TargetInvocationException x)
      {
        Exception y = x.InnerException;
        SendReply(Message, null, new LinkException(y.GetType().FullName, y.Message, y.StackTrace), y);
      }
      catch (Exception x)
      {
        SendReply(Message, null, new LinkException(x.GetType().FullName, x.Message, x.StackTrace), x);
      }
    }

    private void SendReply(LinkMessage Message, LinkStack ReplyLinks, Link Payload, Exception Error)
    {
      //  Identify cases when we don't reply
      if (!Message.HasCallNumber)
        return; //  CallNumber is required on the calling end to match call and reply
      if (!Message.HasPathFrom && !(fApartment is ApartmentSession))
        return; //  Wouldn't know where to reply to        
      //  In this implementation, we only bother replying with a return path
      //  if the call had a return path.  (Does not apply to errors.)
      LinkStack PathFrom = null;
      if ((Error == null) && (Message.HasPathFrom))
        PathFrom = new LinkStack();
      //  
      if (Error is EMorph)
      {
        PathFrom = new LinkStack();
        PathFrom.Push(ReplyLinks);
      }
      //  Build a reply
      LinkStack PathTo = new LinkStack();
      //  Add payload (return parameters/exception)
      if (Payload != null)
        PathTo.Push(Payload);
      //  Add return path
      if (ReplyLinks != null)
        PathTo.Push(ReplyLinks);
      else if (fApartment is ApartmentSession)
      {
        PathTo.Push(((ApartmentSession)fApartment).Path);
      }
      else
        PathTo.Push(Message.PathFrom);
      //  Build reply message
      LinkMessage ReplyMessage = new LinkMessage(PathTo, PathFrom);
      if (Message.HasCallNumber)
        ReplyMessage.CallNumber = Message.CallNumber;
      if (Error is EMorph)
        ReplyMessage.ErrorNumber = ((EMorph)Error).ErrorNumber;
      //  Don't action the apartment link
      ReplyMessage.MoveToNextLink();
      //  Action the reply
      ReplyMessage.ActionNext();
    }

    #endregion

    public override bool Equals(object obj)
    {
      return (obj is LinkApartment) && (((LinkApartment)obj).fApartmentID == fApartmentID);
    }

    public override int GetHashCode()
    {
      return fApartmentID;
    }

    public override string ToString()
    {
      return "{Apartment ID=" + ApartmentID.ToString() + '}';
    }
  }

  public class LinkApartmentProxy : Link
  {
    protected internal LinkApartmentProxy(int ApartmentProxyID)
      : base(LinkTypeService.instance)
    {
      fApartmentProxyID = ApartmentProxyID;
    }

    private int fApartmentProxyID;
    public int ApartmentProxyID
    {
      get { return fApartmentProxyID; }
    }

    #region Link implementation

    public override int Size()
    {
      return 5;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, true, true, false);
      Writer.WriteInt32(fApartmentProxyID);
    }

    public override void Action(LinkMessage Message)
    {
      Message.MoveToNextLink();
      //  If the reply has no call number, then it's not a servlet member reply
      //  (It it likely to be a sequence reply.)
      if (!Message.HasCallNumber)
      {
        Message.ActionNext();
        return;
      }
      //  Get the apartment proxy
      ApartmentProxy ApartmentProxy = ApartmentProxy.Find(ApartmentProxyID);
      if (ApartmentProxy == null)
        throw new EMorph("Apartment proxy not found");
      //  Find call number to match response with waiting thread
      int CallNumber = Message.CallNumber;
      //  Handle Morph error
      if (Message.HasErrorNumber)
      { //  Don't add the reply if noone is waiting for it
        if (ApartmentProxy.Waits.Hold(CallNumber))
        {
          //  Convert byte array to link objects
          Message.PathTo.ToLinks();
          //  Update the proxy's path to the apartment
          string ReverseFromPath = null;
          if (Message.HasPathFrom)
          {
            ApartmentProxy.SetPath(Message.PathFrom);
            ReverseFromPath = Message.PathFrom.Reverse().ToString();
          }
          //  Add the morph error          
          ApartmentProxy.Replies.AssignMorphError(CallNumber, Message.ErrorNumber, ReverseFromPath);
          ApartmentProxy.Waits.End(CallNumber);
        }
        return;
      }
      //  Examine what comes next
      Link NextLink = Message.PathTo.Peek();
      //  Sequence
      if (NextLink is LinkSequence)
      {
        Message.ActionNext();
        ApartmentProxy.fSequenceSender = (SequenceSender)((LinkSequence)NextLink).FindLinkObject();
      }
      //  Examine what comes next
      NextLink = Message.PathTo.Pop();
      //  Information
      if (NextLink is LinkInformation)
        throw new EMorphImplementation();
      //  See if this is a reply
      if ((NextLink != null) && !(NextLink is LinkData) && !(NextLink is LinkException))
        throw new EMorph("Unexpected link type");
      //  Update the proxy's path to the apartment
      if (Message.HasPathFrom)
        ApartmentProxy.SetPath(Message.PathFrom);
      //  Tell the calling thread to hold on, to prevent a timeout while we're assigning the return data.
      //  If Hold() fails, then it's because no thread is waiting for this reply.
      if (!ApartmentProxy.Waits.Hold(CallNumber))
        throw new EMorph("No one waiting for reply");
      //  Handle different types of reply...
      try
      {
        //  ...reply has no parameters
        if (NextLink == null)
        {
          ApartmentProxy.Replies.AssignParams(CallNumber, null, null);
          return;
        }
        //  ...reply has parameters
        if (NextLink is LinkData)
        {
          LinkData DataIn = (LinkData)NextLink;
          object[] Params;
          object Special;
          Parameters.Decode(ApartmentProxy.InstanceFactories, DataIn.ValueTypeStringData, ApartmentProxy.Device.fPath, DataIn.Reader, out Params, out Special);
          ApartmentProxy.Replies.AssignParams(CallNumber, Params, Special);
          return;
        }
        //  ...reply is exception
        if (NextLink is LinkException)
        {
          LinkException x = (LinkException)NextLink;
          ApartmentProxy.Replies.AssignException(CallNumber, x.ClassName, x.Message, x.StackTrace);
          return;
        }
      }
      finally
      {
        ApartmentProxy.Waits.End(CallNumber);
      }
    }

    #endregion

    public override bool Equals(object obj)
    {
      return (obj is LinkApartmentProxy) && (((LinkApartmentProxy)obj).fApartmentProxyID == fApartmentProxyID);
    }

    public override int GetHashCode()
    {
      return fApartmentProxyID;
    }

    public override string ToString()
    {
      return "{ApartmentProxy ID=" + ApartmentProxyID.ToString() + '}';
    }
  }

  public class LinkTypeService : LinkType
  {
    static internal LinkTypeService instance = new LinkTypeService();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Service; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool IsApartment, IsProxy, z;
      Reader.ReadLinkByte(out IsApartment, out IsProxy, out z);
      if (IsApartment)
        if (IsProxy)
          return new LinkApartmentProxy(Reader.ReadInt32());
        else
          return new LinkApartment(Reader.ReadInt32());
      else
        return new LinkService(Reader.ReadString());
    }

    #endregion
  }
}