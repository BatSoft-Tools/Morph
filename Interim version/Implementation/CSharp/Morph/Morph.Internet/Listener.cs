using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Morph.Lib;

namespace Morph.Internet
{
  public class Listener
  {
    internal Listener(IPEndPoint EndPoint)
    {
      fEndPoint = EndPoint;
    }

    private IPEndPoint fEndPoint;
    public IPEndPoint EndPoint
    {
      get { return fEndPoint; }
    }

    private Socket fListener;

    private Socket CreateSocket(IPEndPoint EndPoint)
    {
      Socket socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
      socket.Blocking = true;
      return socket;
    }

    private void AsynchListen()
    {
      try
      {
        try
        {
          fListener.Bind(fEndPoint);
          fListener.Listen(5);
          while (fIsStarted)
            Connections.Add(fListener.Accept());
        }
        finally
        {
          lock (this)
            fIsActive = false;
        }
      }
      catch (Exception x)
      {
        MorphErrors.NotifyAbout(this, x);
      }
    }

    private bool fIsStarted = false;
    private bool fIsActive = false;
    public bool IsActive
    {
      get { return fIsActive; }
    }

    public void Start()
    {
      lock (this)
        if (!fIsActive)
        {
          //  Create the listener
          fListener = CreateSocket(fEndPoint);
          //  Start the thread
          try
          {
            fIsStarted = true;
            (new Thread(new ThreadStart(AsynchListen))).Start();
            fIsActive = true;
          }
          //  Just in case
          catch
          {
            fIsStarted = false;
            fListener.Close();
            fListener = null;
            throw;
          }
        }
        else if (!fIsStarted)
          throw new EMorphUsage("Cannot start while still busy stopping");
    }

    public void Stop()
    {
      lock (this)
        if (fIsActive)
        {
          //  Tell the thread to stop
          fIsStarted = false;
          //  Close the listener
          fListener.Close();
          fListener = null;
        }
    }
  }

  public class Listeners
  {
    internal Listeners(List<Listener> Items)
    {
      fItems = Items;
    }

    private List<Listener> fItems;

    public int Count
    {
      get { return fItems.Count; }
    }

    public Listener this[int index]
    {
      get { return fItems[index]; }
    }

    public Listener[] ToArray()
    {
      return fItems.ToArray();
    }

    public void StartAll()
    {
      foreach (Listener Listener in fItems)
        Listener.Start();
    }

    public void StopAll()
    {
      foreach (Listener Listener in fItems)
        Listener.Stop();
    }
  }

  public class ListenerManager
  {
    static private List<Listener> All = new List<Listener>();

    static private bool IsLocal(IPAddress Address)
    {
      IPAddress[] LocalAddresses = GetAllLocalAddresses();
      foreach (IPAddress LocalAddress in LocalAddresses)
        if (Address.Equals(LocalAddress))
          return true;
      return IPAddress.IsLoopback(Address);
    }

    static public IPAddress[] GetAllLocalAddresses()
    {
      return Dns.GetHostEntry(Dns.GetHostName()).AddressList;
    }

    static public Listener Obtain(IPAddress Address, int Port)
    {
      return Obtain(new IPEndPoint(Address, Port));
    }

    static public Listener Obtain(IPEndPoint EndPoint)
    {
      if (!IsLocal(EndPoint.Address))
        throw new EMorphUsage("Not a local IP address");
      //  See if it already exists
      Listener PortListener = Find(EndPoint);
      if (PortListener == null)
      { //  Create and register the listener
        PortListener = new Listener(EndPoint);
        All.Add(PortListener);
      }
      return PortListener;
    }

    static public Listeners Obtain(int Port)
    {
      List<Listener> Items = new List<Listener>();
      //  Add all network addresses to result
      IPAddress[] Addresses = GetAllLocalAddresses();
      for (int i = 0; i < Addresses.Length; i++)
        Items.Add(Obtain(new IPEndPoint(Addresses[i], Port)));
      //  Ensure loopback is included
      bool AddLoopback = true;
      for (int i = 0; i < Items.Count; i++)
        if (Items[i].EndPoint.Address.Equals(IPAddress.Loopback))
        {
          AddLoopback = false;
          break;
        }
      if (AddLoopback)
        Items.Add(Obtain(new IPEndPoint(IPAddress.Loopback, Port)));
      //  Return a list of listeners
      return new Listeners(Items);
    }

    static public Listener Find(IPEndPoint EndPoint)
    {
      foreach (Listener Listener in All)
        if (Listener.EndPoint.Equals(EndPoint))
          return Listener;
      return null;
    }

    static public Listeners Find(IPAddress Address)
    {
      List<Listener> Items = new List<Listener>();
      foreach (Listener Listener in All)
        if (Listener.EndPoint.Address.Equals(Address))
          Items.Add(Listener);
      return new Listeners(Items);
    }

    static public Listeners Find(int Port)
    {
      List<Listener> Items = new List<Listener>();
      //  Add all network addresses to result
      foreach (Listener Listener in All)
        if (Listener.EndPoint.Port == Port)
          Items.Add(Listener);
      //  Return a list of listeners
      return new Listeners(Items);
    }

    static public Listeners FindAll()
    {
      return new Listeners(All);
    }

    static public void StartAll()
    {
      foreach (Listener Listener in All)
        Listener.Start();
    }

    static public void StopAll()
    {
      foreach (Listener Listener in All)
        Listener.Stop();
    }

    static public void RemoveAllInactive()
    {
      lock (All)
        for (int i = All.Count - 1; 0 <= i; i--)
        {
          Listener Listener = All[i];
          if (!Listener.IsActive)
            All.Remove(Listener);
        }
    }
  }
}