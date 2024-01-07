/* Client side usage
 * 
 * ApartmentProxy Service = ApartmentProxy.ViaString("test.com", "Test", 600000);
 * ServletProxy Servlet = Service.DefaultServlet;
 * object result = Servlet.CallMethod("Method", InParams, out OutParams);
 */

using Morph.Lib;

namespace Morph.Endpoint
{
  public class ServletProxy : RegisterItemID
  {
    internal ServletProxy(ApartmentProxy ApartmentProxy, int ID, string TypeName)
    {
      _ApartmentProxy = ApartmentProxy;
      fID = ID;
      fTypeName = TypeName;
    }

    private ApartmentProxy _ApartmentProxy;
    public ApartmentProxy ApartmentProxy
    {
      get { return _ApartmentProxy; }
    }

    #region RegisterItemID Members

    private int fID;
    public int ID
    {
      get { return fID; }
    }

    #endregion

    #region Private

    private LinkData ParamsToLink(object Special, object[] Params)
    {
      if (Special != null)
        return new LinkData(Parameters.Encode(Params, Special, _ApartmentProxy.InstanceFactories));
      if (Params != null)
        return new LinkData(Parameters.Encode(Params, _ApartmentProxy.InstanceFactories));
      return null;
    }

    private void Send(LinkMember Member, object Special, object[] InParams)
    {
      LinkMessage Message = new LinkMessage(new LinkStack(), null);
      //  Params
      Message.PathTo.Push(ParamsToLink(Special, InParams));
      //  Method
      Message.PathTo.Push(Member);
      //  Servlet
      Message.PathTo.Push(new LinkServlet(ID));
      //  Sequence
      if (_ApartmentProxy.fSequenceSender != null)
        _ApartmentProxy.fSequenceSender.AddNextLink(false, Message);
      //  Request
      _ApartmentProxy.Send(Message);
    }

    private object Call(LinkMember Member, object Special, object[] InParams, out object[] OutParams)
    {
      //  Determine if we need a path to the apartment in the reply
      LinkStack FromPath = null;
      if (_ApartmentProxy.RequiresFromPath)
        FromPath = new LinkStack();
      //  Create the message
      LinkMessage Message = new LinkMessage(new LinkStack(), FromPath);
      //  Params
      Message.PathTo.Push(ParamsToLink(Special, InParams));
      //  Method
      Message.PathTo.Push(Member);
      //  Servlet
      Message.PathTo.Push(new LinkServlet(ID));
      //  Sequence
      if (_ApartmentProxy.fSequenceSender != null)
        _ApartmentProxy.fSequenceSender.AddNextLink(false, Message);
      //  Request/Response
      return _ApartmentProxy.Call(Message, out OutParams);
    }

    #endregion

    #region Public

    private string fTypeName = null;
    public string TypeName
    {
      get { return fTypeName; }
    }

    public object Facade = null;

    public LinkStack Path
    {
      get
      {
        LinkStack result = new LinkStack();
        result.Append(_ApartmentProxy.Path);
        result.Append(new LinkServlet(fID));
        return result;
      }
    }

    public void SendMethod(string MethodName, object[] InParams)
    {
#if LOG_MESSAGES
      Log.Default.Add(LinkTypes.AppName);
      Log.Default.Add(">>> Send: " + MethodName);
      Log.Default.Add(Log.nl);
#endif
      Send(new LinkMethod(MethodName), null, InParams);
    }

    public object CallMethod(string MethodName, object[] InParams, out object[] OutParams)
    {
#if LOG_MESSAGES
      Log.Default.Add(LinkTypes.AppName);
      Log.Default.Add(">>> Call: " + MethodName);
      Log.Default.Add(Log.nl);
#endif
      try
      {
        return Call(new LinkMethod(MethodName), null, InParams, out OutParams);
      }
      finally
      {
#if LOG_MESSAGES
        Log.Default.Add(LinkTypes.AppName);
        Log.Default.Add("<<< Call: " + MethodName);
        Log.Default.Add(Log.nl);
#endif
      }
    }

    public object CallMethod(string MethodName, object[] InParams)
    {
      object[] NoParams = null;
      return CallMethod(MethodName, InParams, out NoParams);
    }

    public void SendSetProperty(string PropertyName, object Value, object[] Index)
    {
      Send(new LinkProperty(PropertyName, true, Index != null), Value, Index);
    }

    public void CallSetProperty(string PropertyName, object Value, object[] Index)
    {
      Call(new LinkProperty(PropertyName, true, Index != null), Value, Index, out Index);
    }

    public object CallGetProperty(string PropertyName, object[] Index)
    {
      return Call(new LinkProperty(PropertyName, false, Index != null), null, Index, out Index);
    }

    #endregion
  }

  internal class ServletProxies
  {
    internal ServletProxies(ApartmentProxy ApartmentProxy)
    {
      fApartmentProxy = ApartmentProxy;
    }

    private ApartmentProxy fApartmentProxy;
    private RegisterItems<ServletProxy> fServletProxies = new RegisterItems<ServletProxy>();

    public ServletProxy Obtain(int ID, string TypeName)
    {
      ServletProxy result;
      lock (fServletProxies)
      {
        result = fServletProxies.Find(ID);
        if (result == null)
        {
          result = new ServletProxy(fApartmentProxy, ID, TypeName);
          fServletProxies.Add(result);
        }
      }
      return result;
    }

    public ServletProxy Find(int ID)
    {
      lock (fServletProxies)
        return fServletProxies.Find(ID);
    }
  }
}