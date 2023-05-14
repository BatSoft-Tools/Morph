using Morph.Lib;

namespace Morph.Internet
{
  public class ActionMessage : Action
  {
    public ActionMessage(LinkMessage Message)
    {
      if (Message == null)
        throw new EMorphImplementation();
      fMessage = Message;
    }

    private LinkMessage fMessage;

    #region Action Members

    public void Execute()
    {
      fMessage.ActionNext();
    }

    #endregion
  }

  public delegate Action ActionCreator(LinkMessage Message, Connection Connection);

  public static class ActionHandler
  {
    static ActionHandler()
    {
      Actions = new ThreadedActionQueue();
      Actions.Error += ActionError;
    }

    static internal ThreadedActionQueue Actions;

    static private Action ActionMessageCreator(LinkMessage Message, Connection Connection)
    {
#if LOG_MESSAGES
      Log.Default.Add("..." + LinkTypes.AppName + " : " + Connection.RemoteEndPoint.ToString() + " -> " + Connection.LocalEndPoint.ToString());
      Log.Default.Add(Message);
      Log.Default.Add(Log.nl);
#endif
      return new ActionMessage(Message);
    }

    static private ActionCreator _ActionCreator = ActionMessageCreator;
    public static ActionCreator ActionCreator
    {
      get { return ActionHandler._ActionCreator; }
      set
      {
        if (value == null)
          throw new EMorphUsage("ActionHandler cannot be null.");
        ActionHandler._ActionCreator = value;
      }
    }

    static public void Add(LinkMessage Message, Connection Connection)
    {
      Actions.Push(_ActionCreator(Message, Connection));
    }

    static public int WaitingCount
    {
      get { return Actions.Count; }
    }

    static public void SetThreadCount(int ThreadCount)
    {
      Actions.SetThreadCount(ThreadCount);
    }

    static public void Stop()
    {
      Actions.WaitUntilNoThreads();
    }

    static private void ActionError(object sender, ExceptionArgs e)
    {
      MorphErrors.NotifyAbout(sender, e);
    }
  }
}