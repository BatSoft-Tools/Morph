using System.Net;
using System.Net.Sockets;
using Morph.Lib;

namespace Morph.Internet
{
  public class LinkInternetIPv4 : LinkInternet
  {
    public LinkInternetIPv4(IPEndPoint EndPoint)
      : base(EndPoint)
    {
    }

    static public LinkInternetIPv4 ReadNew(StreamReader Reader, bool HasURI, bool HasPort)
    {
      //  Read host
      IPAddress address;
      if (HasURI)
        try
        { //  String
          address = IPAddress.Parse(Reader.ReadString());
        }
        catch
        {
          throw new EMorph("Invalid IPv4 Address");
        }
      else
      { //  Binary
        byte[] host = new byte[4];
        host[0] = (byte)Reader.ReadInt8();
        host[1] = (byte)Reader.ReadInt8();
        host[2] = (byte)Reader.ReadInt8();
        host[3] = (byte)Reader.ReadInt8();
        address = new IPAddress(host);
      }
      //  Read port
      int port = LinkInternet.MorphPort;
      if (HasPort)
        port = (int)Reader.ReadInt16();
      //  Done
      return new LinkInternetIPv4(new IPEndPoint(address, port));
    }

    #region Link members

    public override int Size()
    {
      bool HasURI = false;// Host == null;  //  Is there ever a need for more than byte[4]?
      bool HasPort = EndPoint.Port != LinkInternet.MorphPort;
      int size = 1;
      if (HasURI)
        size += 4 + (EndPoint.Address.ToString().Length * 2);
      else
        size += 4;
      if (HasPort)
        size += 2;
      return size;
    }

    public override void Write(StreamWriter Writer)
    {
      bool IsIPv6 = false;
      bool IsString = false;// Host == null;  //  Is there ever a need for more than byte[4]?
      bool HasPort = EndPoint.Port != LinkInternet.MorphPort;
      //  Link byte
      Writer.WriteLinkByte((byte)LinkType.ID, IsIPv6, IsString, HasPort);
      //  Host
      if (IsString)
        Writer.WriteString(EndPoint.Address.ToString());
      else
      {
        byte[] host = EndPoint.Address.GetAddressBytes();
        Writer.WriteInt8(host[0]);
        Writer.WriteInt8(host[1]);
        Writer.WriteInt8(host[2]);
        Writer.WriteInt8(host[3]);
      }
      // Port
      if (HasPort)
        Writer.WriteInt16(EndPoint.Port);
    }

    public override void Action(LinkMessage Message)
    {
      //  If this link represents here, then move on to the next link
      if (Connections.IsEndPointOnThisProcess(this.EndPoint))
      {
        Message.MoveToNextLink();
        Message.ActionNext();
        return;
      }
      //  Don't confuse sequencing when we remove this address (next step)
      Message = Message.Clone();
      //  Remove this address (this is done only in IPv4 due to NAT)
      if (Message.PathTo.Peek().Equals(this))
        Message.PathTo.Pop();
      //  Send the message on
      Connections.Obtain(this.EndPoint).Write(Message);
    }

    #endregion
  }
}