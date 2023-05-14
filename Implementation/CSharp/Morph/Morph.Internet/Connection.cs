using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Morph.Lib;
using StreamReader = Morph.Lib.StreamReader;
using StreamWriter = Morph.Lib.StreamWriter;

namespace Morph.Internet
{
  internal class DataInHandler
  {
    public DataInHandler(Connection Connection)
    {
      fConnection = Connection;
    }

    private Connection fConnection;

    private MemoryStream fStreamIn = null;
    private long fMessageSize = 0;

    public void AddData(byte[] data, int offset, int count)
    {
      if ((data == null) || (count == 0))
        return;
      //  New stream
      if (fStreamIn == null)
        fStreamIn = new MemoryStream();
      //  Write data to stream
      fStreamIn.Write(data, offset, count);
      //  New message
      if (fMessageSize == 0)
        try
        {
          //  Start reading from the beginning
          long Pos = fStreamIn.Position;
          fStreamIn.Position = 0;
          try
          {
            StreamReader Reader = new StreamReaderSizeless(fStreamIn);
            //  Read in the link type
            bool x, y, z;
            LinkTypeID LinkType = (LinkTypeID)Reader.PeekLinkByte(out x, out y, out z);
            //  If received a LinkEnd message to the socket, then end the socket
            if (LinkType == LinkTypeID.End)
            {
              fConnection.Close();
              return;
            }
            //  If received LinkMessage, then make sure there's enough data to determine the size for a complete message
            if (LinkType == LinkTypeID.Message)
            {
              if (fStreamIn.Length >= 5 + (x ? 4 : 0) + (y ? 4 : 0) + (z ? 4 : 0))
                fMessageSize = ((LinkMessage)LinkTypes.ReadLink(Reader)).Size();
            }
            else
              //  No other link types are accepted outside here
              throw new EMorph("Unexpected link type");
          }
          finally
          { //  Continue as we were
            fStreamIn.Position = Pos;
          }
        }
        catch
        {
          fConnection.Close();
          throw;
        }
      //  Received complete message
      if ((fMessageSize > 0) && (fMessageSize <= fStreamIn.Length))
      {
        //  Read the message from the stream
        fStreamIn.Position = 0;
        Link Link = LinkTypes.ReadLink(new StreamReaderSized(fStreamIn, (int)fMessageSize));
        //  Add the (now completely received) message to the action queue
        if (Link is LinkMessage)
        {
          LinkMessage Message = (LinkMessage)Link;
          //  First apply a NAT workaround for IPv4
          if (fConnection.Socket.AddressFamily == AddressFamily.InterNetwork)
          {
            //  Add the return IP address
            if (Message.HasPathFrom)
              Message.PathFrom.Push(new LinkInternetIPv4((IPEndPoint)fConnection.Socket.RemoteEndPoint));
          }
          //  Add it to the queue
          ActionHandler.Add(Message, fConnection);
        }
        else
          throw new EMorph("Unexpected link type");
        //  Work out what data was unused (don't want to loose it)
        int RemainingCount = (int)(fStreamIn.Length - fMessageSize);
        int RemainingOffset = offset + count - RemainingCount;
        //  Reset the reader
        fStreamIn = null;
        fMessageSize = 0;
        //  Don't loose the remaining data
        if (RemainingCount > 0)
          AddData(data, RemainingOffset, RemainingCount);
      }
    }
  }

  public class Connection : RegisterItemName
  {
    internal Connection(Socket Socket)
    {
      fSocket = Socket;
      fName = fSocket.RemoteEndPoint.ToString();
      fDataIn = new DataInHandler(this);
      SendMorphValidation();
      Connections.Add(this);
      StartReceivingData();
    }

    #region Connection validation

    //  "Morph"#0 + Major version:1 + Minor version:1 
    private static byte[] MorphValidation = new byte[] { 0x4D, 0x6F, 0x72, 0x70, 0x68, 0x00, 0x01, 0x01 };

    private ManualResetEvent fMorphValidationSent = new ManualResetEvent(false);

    private void SendMorphValidation()
    {
      fSocket.Send(MorphValidation);
      fMorphValidationSent.Set();
    }

    private void TestMorphValidation()
    {
      byte[] Buffer = new byte[8];
      if (Buffer.Length != fSocket.Receive(Buffer))
        throw new EMorph("Remote connection appears to not be a Morph connection.");
      for (int i = 5; i >= 0; i--)
        if (Buffer[i] != MorphValidation[i])
          throw new EMorph("Remote connection appears to not be a Morph connection.");
      if (Buffer[6] != MorphValidation[6])
        throw new EMorph("Incompatible Major versions of Morph.");
      if (Buffer[7] != MorphValidation[7])
        throw new EMorph("Incompatible Minor versions of Morph.");
    }

    #endregion

    private Socket fSocket;
    public Socket Socket
    {
      get { return fSocket; }
    }

    private DataInHandler fDataIn;
    private const int BufferSize = 2048;

    public EndPoint LocalEndPoint
    {
      get { return fSocket.LocalEndPoint; }
    }

    public EndPoint RemoteEndPoint
    {
      get { return fSocket.RemoteEndPoint; }
    }

    private LinkInternet fLink = null;
    public LinkInternet Link
    {
      get
      {
        if (fLink == null)
          fLink = LinkInternet.New(RemoteEndPoint);
        return fLink;
      }
    }

    public void Write(LinkMessage Message)
    {
      //  Don't write until Morph validation information has been sent
      fMorphValidationSent.WaitOne();
      //  Get the data to send.
      //  Alot of optimisation could be done here.
      //  This ought to use BeginSend(), but that'll have to wait for now.
      //  Also, this is where one would batch messages
      //
      //  Create a stream
      MemoryStream Stream = new MemoryStream();
      //  Write the Message to the stream
#if LOG_MESSAGES
      Log.Default.Add(LinkTypes.AppName + "... " + " : " + LocalEndPoint.ToString() + " -> " + RemoteEndPoint.ToString());
      Log.Default.Add(Message);
      Log.Default.Add(Log.nl);
#endif
      Message.Write(new StreamWriter(Stream));
      //  Start from the beginning
      long TotalCount = Stream.Length;
      Stream.Position = 0;
      //  Write the stream to the socket
      byte[] Buffer = new byte[BufferSize];
      lock (fSocket)
        if (fSocket.Connected)
          while (TotalCount > 0)
          {
            int count = (int)(TotalCount < BufferSize ? TotalCount : BufferSize);
            count = Stream.Read(Buffer, 0, count);
            TotalCount -= count;
            fSocket.Send(Buffer, 0, count, SocketFlags.Partial);
          }
    }

    #region Receiving

    private byte[] fIncomingData = new byte[BufferSize];

    internal void StartReceivingData()
    {
      new Thread(new ThreadStart(ReceivingData)).Start();
    }

    private void ReceivingData()
    {
      try
      {
        //  Validate the connection
        TestMorphValidation();
        //  Read incoming data
        int NoDataIteration = 0;
        while (fSocket.Connected)
        {
          int count = fSocket.Receive(fIncomingData, 0, fIncomingData.Length, SocketFlags.Partial);
          //  Workaround for fSocket.Receive() continuously returning 0 instead of waiting for data.
          //  Seems to happen when connection has been abandoned by remote host.
          if (count > 0)
            NoDataIteration = 0;
          else if (NoDataIteration++ > 5)
            Close();
          //  Add the received data to the message stream
          fDataIn.AddData(fIncomingData, 0, count);
        }
      }
      catch (Exception x)
      {
        //  Whatever the problem, make sure the thread is closed
        try
        {
          Close();
        }
        catch (Exception)
        {
        }
        MorphErrors.NotifyAbout(this, x);
      }
    }

    #endregion

    public void Close()
    {
      //  Prevent other threads from writing to the socket
      Connections.Remove(this);
      lock (fSocket)
        if (fSocket.Connected)
        {
          //  Try to tell other end that the socket is closing
          try
          {
            fSocket.Send(new byte[] { 0 }); //  Sending LinkEnd
          }
          catch
          {
          }
          //  Now close the socket
          fSocket.Close();
        }
    }

    #region RegisterItemName Members

    private string fName;
    public string Name
    {
      get { return fName; }
    }

    #endregion
  }

  static public class Connections
  {
    #region internal

    static private RegisterItems<Connection> Conns = new RegisterItems<Connection>();
    static private Hashtable LocalEndPoints = new Hashtable();

    private class LocalEndPointCounter
    {
      public int Count = 1;
    }

    static private IPAddress[] CurrentLocalAddresses()
    {
      return Dns.GetHostEntry(Dns.GetHostName()).AddressList;
    }

    static private Socket NewSocket(IPEndPoint RemoteEndPoint)
    {
      ProtocolType Protocol;
      if (RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
        Protocol = ProtocolType.IP;
      else if (RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
        Protocol = ProtocolType.IPv6;
      else
        throw new Exception("Unsupported protocol");
      //  Create socket connection
      Socket socket = new Socket(RemoteEndPoint.AddressFamily, SocketType.Stream, Protocol);
      //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
      //socket.Bind(new IPEndPoint(IPAddress.Any, 0));
      socket.Connect(RemoteEndPoint);
      //  Create connection
      return socket;
    }

    static internal void Add(Connection Connection)
    {
      lock (Conns)
      {
        Conns.Add(Connection);
        //  If this is a server process, then multiple sockets may use the same local endpoint,
        //  so keep a reference count.
        lock (LocalEndPoints)
        {
          LocalEndPointCounter counter = (LocalEndPointCounter)LocalEndPoints[Connection.LocalEndPoint.ToString()];
          if (counter == null)
            LocalEndPoints.Add(Connection.LocalEndPoint.ToString(), new LocalEndPointCounter());
          else
            counter.Count++;
        }
      }
    }

    static internal void Remove(Connection Connection)
    {
      lock (Conns)
      {
        Conns.Remove(Connection);
        lock (LocalEndPoints)
        {
          LocalEndPointCounter counter = (LocalEndPointCounter)LocalEndPoints[Connection.LocalEndPoint.ToString()];
          if (counter != null)  //  Should not be possible to be null, but just just in case
          { //  Decrement the refence count
            counter.Count--;
            //  If there are no more, then remove the counter
            if (counter.Count == 0)
              LocalEndPoints.Remove(Connection.LocalEndPoint.ToString());
          }
        }
      }
    }

    static private Connection Find(IPEndPoint RemoteEndPoint)
    {
      Connection Connection;
      lock (Conns)
        Connection = Conns.Find(RemoteEndPoint.ToString());
      return Connection;
    }

    #endregion

    static public Connection Add(Socket Socket)
    {
      if (Socket == null)
        throw new EMorphUsage("Cannot connect with a null connection");
      //  Create (and register) the new connection
      return new Connection(Socket);
    }

    static public Connection Add(IPEndPoint RemoteEndPoint)
    {
      if (RemoteEndPoint == null)
        throw new EMorphUsage("Cannot connect to a null end point");
      //  Create (and register) the new connection
      return Add(NewSocket(RemoteEndPoint));
    }

    static public Connection Obtain(IPEndPoint RemoteEndPoint)
    {
      Connection Connection = Find(RemoteEndPoint);
      if (Connection == null)
        return new Connection(NewSocket(RemoteEndPoint));
      return Connection;
    }

    static public void CloseAll()
    {
      List<Connection> AllConns;
      lock (Conns)
        AllConns = Conns.List();
      foreach (Connection Conn in AllConns)
        Conn.Close();
    }

    static public bool IsEndPointOnThisDevice(IPEndPoint EndPoint)
    {
      IPAddress[] LocalAddresses = CurrentLocalAddresses();
      foreach (IPAddress LocalAddress in LocalAddresses)
        if (EndPoint.Address.Equals(LocalAddress))
          return true;
      return IPAddress.Loopback.Equals(EndPoint.Address);
    }

    static public bool IsEndPointOnThisProcess(IPEndPoint EndPoint)
    {
      lock (LocalEndPoints)
        return LocalEndPoints[EndPoint.ToString()] != null;
    }
  }
}