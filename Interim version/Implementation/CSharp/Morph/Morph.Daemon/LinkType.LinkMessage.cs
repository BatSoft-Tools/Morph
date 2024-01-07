using Morph.Internet;
using Morph.Lib;
using System.Net.Sockets;

namespace Morph.Daemon
{
  public class LinkMessageDaemon : LinkMessage
  {
    internal LinkMessageDaemon(StreamReader Reader)
      : base(Reader)
    {
    }

    public Socket SourceSocket = null;
  }

  public class ActionMessageDaemon : Action
  {
    public ActionMessageDaemon(LinkMessage Message, Socket Socket)
    {
      if (Message == null)
        throw new EMorphImplementation();
      _Message = Message;
      if (Socket == null)
        throw new EMorphImplementation();
      _Socket = Socket;
    }

    private LinkMessage _Message;
    private Socket _Socket;

    #region Action Members

    public void Execute()
    {
      ((LinkMessageDaemon)_Message).SourceSocket = _Socket;
      _Message.ActionNext();
    }

    #endregion
  }

  public class LinkTypeMessageDaemon : LinkType
  {
    static internal LinkTypeMessageDaemon instance = new LinkTypeMessageDaemon();

    static private Action CreateActionMessageDaemonCreator(LinkMessage Message, Connection Connection)
    {
      return new ActionMessageDaemon(Message, Connection.Socket);
    }

    static public void Register()
    {
      LinkTypes.Register(instance);
      ActionHandler.ActionCreator = CreateActionMessageDaemonCreator;
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Message; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      return new LinkMessageDaemon(Reader);
    }

    #endregion
  }
}