/**
 * An apartment proxy is the client side of a connection.
 * 
 * Use the Via... class methods to create a new apartment proxy, thereby establishing a connection to a service.
 * 
 * Parameters:
 * - Address:  This is the internet addess where the service is hosted.  This can be a domain name or an IPv4 or IPv6 internet address.
 * - ServiceName:  The name of the service that this apartment proxy aims to represent.
 * - Timeout:  How long to wait for replies to sent messages.  Timed outs raise exceptions.
 * - InstanceFactories:  A factory that converts parameters into values.  Must not be null.
 */

using System;
using System.Collections.Generic;
using System.Net;
using Morph.Internet;
using Morph.Lib;
using Morph.Params;
using Morph.Sequencing;

namespace Morph.Endpoint
{
  public class ApartmentProxy : RegisterItemID, IDisposable
  {
    #region Internal

    internal ApartmentProxy(Device Device, string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories)
    {
      //  Servlet proxies
      fServletProxies = new ServletProxies(this);
      fDefaultServlet = fServletProxies.Obtain(Servlet.DefaultID, null);
      //  Timeout
      fTimeout = Timeout;
      //  Path
      fApartmentProxyLink = new LinkApartmentProxy(fApartmentProxyID);
      fDevice = Device;
      ApartmentLink = null;
      //  Set Service link
      fServiceName = ServiceName;
      //  Registration
      lock (All)
        All.Add(this);
      //  Instance factories
      fInstanceFactories = InstanceFactories;
    }

    internal ApartmentProxy(Device Device, int ApartmentID, TimeSpan Timeout, InstanceFactories InstanceFactories)
    {
      //  Servlet proxies
      fServletProxies = new ServletProxies(this);
      fDefaultServlet = fServletProxies.Obtain(Servlet.DefaultID, null);
      //  Timeout
      fTimeout = Timeout;
      //  Path
      fApartmentProxyLink = new LinkApartmentProxy(fApartmentProxyID);
      fDevice = Device;
      ApartmentLink = new LinkApartment(ApartmentID);
      //  Set Service link
      fServiceName = null;
      //  Registration
      lock (All)
        All.Add(this);
      //  Instance factories
      fInstanceFactories = InstanceFactories;
    }

    static private bool ReadByteFromAddress(StringParser Parser, out byte Byte)
    {
      Byte = 0;
      string Str = Parser.ReadDigits();
      if ((Str == null) || (Str.Length > 3))
        return false;
      uint Num = uint.Parse(Str);
      if (256 <= Num)
        return false;
      Byte = (byte)Num;
      return true;
    }

    static private IPAddress ResolveIPv4(string String)
    {
      StringParser Parser = new StringParser(String);
      byte[] Address = new byte[4];
      if (ReadByteFromAddress(Parser, out Address[0]))
        if (Parser.ReadChar('.'))
          if (ReadByteFromAddress(Parser, out Address[1]))
            if (Parser.ReadChar('.'))
              if (ReadByteFromAddress(Parser, out Address[2]))
                if (Parser.ReadChar('.'))
                  if (ReadByteFromAddress(Parser, out Address[3]))
                    if (Parser.IsEnded())
                      return new IPAddress(Address);
      return null;
    }

    static private IPAddress Resolve(string String)
    {
      IPAddress Address = ResolveIPv4(String);
      if (Address != null)
        return Address;
      return Dns.GetHostEntry(String).AddressList[0];
    }

    private void EstablishConnection()
    {
      object[] OutParams;
      Call(new LinkMessage(new LinkStack(), new LinkStack()), out OutParams);
    }

    private void EndConnection()
    {
      LinkMessage Message = new LinkMessage(new LinkStack(), new LinkStack());
      Message.PathTo.Push(LinkTypeEnd.End);
      Send(Message);
    }

    internal void Send(LinkMessage Message)
    {
      //  Service/Apartment
      PushToPathTo(Message);
      //  No need to action the Apartment Proxy link
      Message.MoveToNextLink();
      //  Send it off
      Message.ActionNext();
    }

    internal object Call(LinkMessage Message, out object[] OutParams)
    {
      //  Call number
      int CallNumber = CallNumberSeed.Generate();
      Message.CallNumber = CallNumber;
      //  Sequence
      if (fSequenceSender != null)
        fSequenceSender.AddNextLink(false, Message);
      //  Service/Apartment path
      PushToPathTo(Message);
      //  Prepare to wait for the reply
      //  If the server end of this call is in this process, then this thread will end up replying to itself.
      //  Calling Prepare() here allows the server end to unblock the wait before Wait() is called.
      Waits.Prepare(CallNumber);
      try
      {
        //  No need to action the Apartment Proxy link
        Message.MoveToNextLink();
        //  Send the message off
        Message.ActionNext();
        Message = null;
        //  Wait for a response
        if (Waits.Wait(CallNumber, Timeout))
          //  Received a reply
          return Replies.GetParams(CallNumber, out OutParams);
        else
          //  No reply
          throw new EMorph("Timeout");
      }
      catch (Exception x)
      { //  Tidy up in case of error
        Replies.Remove(CallNumber);
        Waits.End(CallNumber);
        throw x;
      }
    }

    #endregion

    #region Constructors

    static public ApartmentProxy ViaLocal(string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories)
    {
      return ViaEndPoint(ServiceName, Timeout, InstanceFactories, new IPEndPoint(IPAddress.Loopback, LinkInternet.MorphPort));
    }

    static public ApartmentProxy ViaString(string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories, string Address)
    {
      return ViaAddress(ServiceName, Timeout, InstanceFactories, Resolve(Address));
    }

    static public ApartmentProxy ViaString(string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories, string Address, int Port)
    {
      return ViaEndPoint(ServiceName, Timeout, InstanceFactories, new IPEndPoint(Resolve(Address), Port));
    }

    static public ApartmentProxy ViaAddress(string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories, IPAddress Address)
    {
      return ViaEndPoint(ServiceName, Timeout, InstanceFactories, new IPEndPoint(Address, LinkInternet.MorphPort));
    }

    static public ApartmentProxy ViaEndPoint(string ServiceName, TimeSpan Timeout, InstanceFactories InstanceFactories, IPEndPoint EndPoint)
    {
      //  Add Internet link
      LinkStack DevicePath = new LinkStack();
      if (EndPoint != null)
        DevicePath.Push(LinkInternet.New(EndPoint));
      //  Obtain the device
      Device Device = Devices.Obtain(DevicePath);
      //  Create Apartment Proxy
      ApartmentProxy NewProxy = new ApartmentProxy(Device, ServiceName, Timeout, InstanceFactories);
      //  Establish connection
      NewProxy.EstablishConnection();
      //  Return the proxy object (removing redundancy)
      ApartmentProxy OldProxy = Device.Find(NewProxy.ApartmentID);
      if (OldProxy != NewProxy)
        NewProxy.Dispose();
      return OldProxy;
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      lock (All)
        All.Remove(this);
      if ((fDevice != null) && (ApartmentID != 0))
        lock (fDevice.fApartmentProxiesByApartmentID)
          if (fDevice.fApartmentProxiesByApartmentID[ApartmentID] == this)
            fDevice.fApartmentProxiesByApartmentID.Remove(ApartmentID);
      if (fSequenceSender != null)
      {
        fSequenceSender.Halt();
        fSequenceSender = null;
      }
      EndConnection();
    }

    #endregion

    #region Register

    ~ApartmentProxy()
    {
      Dispose();
    }

    #region RegisterItemID Members

    internal int ApartmentID
    {
      get
      {
        if (ApartmentLink == null)
          return 0;
        return ApartmentLink.ApartmentID;
      }
    }

    private int fApartmentProxyID = IDFactory.Generate();
    public int ID
    {
      get { return fApartmentProxyID; }
    }

    static public IIDFactory IDFactory = new IDSeed();

    #endregion

    static private RegisterItems<ApartmentProxy> All = new RegisterItems<ApartmentProxy>();

    static public ApartmentProxy Find(int ID)
    {
      lock (All)
        return All.Find(ID);
    }

    #endregion

    #region ServletProxies

    private ServletProxies fServletProxies;
    internal ServletProxies ServletProxies
    {
      get { return fServletProxies; }
    }

    public ServletProxy FindServlet(int ServletID)
    {
      return fServletProxies.Find(ServletID);
    }

    private ServletProxy fDefaultServlet;
    public ServletProxy DefaultServlet
    {
      get { return fDefaultServlet; }
    }

    #endregion

    #region Timeout

    private TimeSpan fTimeout;
    public TimeSpan Timeout
    {
      get { return fTimeout; }
    }

    #endregion

    #region Path

    private LinkApartmentProxy fApartmentProxyLink;

    private Device fDevice;
    public Device Device
    {
      get { return fDevice; }
    }

    private string fServiceName;

    private LinkApartment fApartmentLink = null;
    internal LinkApartment ApartmentLink
    {
      get { return fApartmentLink; }
      set
      {
        if (ApartmentID == (value == null ? 0 : value.ApartmentID))
          return;
        if ((fDevice != null) && (fApartmentLink != null))
          lock (fDevice.fApartmentProxiesByApartmentID)
            fDevice.fApartmentProxiesByApartmentID.Remove(ApartmentID);
        fApartmentLink = value;
        if ((fDevice != null) && (fApartmentLink != null))
          lock (fDevice.fApartmentProxiesByApartmentID)
            if (fDevice.fApartmentProxiesByApartmentID[ApartmentID] == null)
              fDevice.fApartmentProxiesByApartmentID.Add(ApartmentID, this);
      }
    }

    public bool RequiresFromPath
    {
      get { return true; }
    }

    public LinkStack Path
    {
      get
      {
        LinkStack FullPath = new LinkStack();
        if (ApartmentLink != null)
          FullPath.Push(ApartmentLink);
        else if (fServiceName != null)
          FullPath.Push(new LinkService(fServiceName));
        FullPath.Push(fDevice.fPath);
        FullPath.Push(fApartmentProxyLink);
        return FullPath;
      }
    }

    internal void SetPath(LinkStack Path)
    {
      if (Path == null)
        throw new EMorphUsage("A proper path to the apartment is expected");
      List<Link> Links = Path.ToLinks();
      //  Find this apartment proxy
      Link Link = null;
      if (Links.Count > 0)
      {
        Link = Links[Links.Count - 1];
        Links.RemoveAt(Links.Count - 1);
      }
      if ((Link == null) || !(Link is LinkApartmentProxy) || (((LinkApartmentProxy)Link).ApartmentProxyID != fApartmentProxyID))
        throw new EMorphImplementation();
      //  Find the apartment link
      for (int i = Links.Count - 1; i >= 0; i--)
      {
        Link = Links[0];
        Links.RemoveAt(0);
        if (Link is LinkApartment)
        {
          ApartmentLink = (LinkApartment)Link;
          fDevice.fPath = new LinkStack(Links);
          return;
        }
      }
      throw new EMorph("Path did not include apartment link");
    }

    public void PushToPathTo(LinkMessage Message)
    {
      if (ApartmentLink != null)
        Message.PathTo.Push(ApartmentLink);
      else if (fServiceName != null)
        Message.PathTo.Push(new LinkService(fServiceName));
      Message.PathTo.Push(fDevice.fPath);
      Message.PathTo.Push(fApartmentProxyLink);
    }

    #endregion

    #region Reply handling

    private IDSeed CallNumberSeed = new IDSeed();

    internal Replies Replies = new Replies();

    internal NumberedWaits Waits = new NumberedWaits();

    #endregion

    #region Instance factories

    private InstanceFactories fInstanceFactories;
    public InstanceFactories InstanceFactories
    {
      get { return fInstanceFactories; }
      set { fInstanceFactories = value; }
    }

    #endregion

    #region Sequencing

    internal SequenceSender fSequenceSender = null;
    public SequenceLevel SequenceLevel
    {
      get
      {
        if (fSequenceSender == null)
          return SequenceLevel.None;
        if (fSequenceSender.IsLossless)
          return SequenceLevel.Lossless;
        else
          return SequenceLevel.Lossy;
      }
    }

    #endregion
  }
}