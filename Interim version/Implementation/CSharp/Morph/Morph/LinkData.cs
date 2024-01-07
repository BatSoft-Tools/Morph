using Morph.Lib;

namespace Morph
{
  public class LinkData : Link
  {
    public LinkData(byte[] Data, ValueTypeStringData ValueTypeStringData, bool MSB)
      : base(LinkTypeData.instance)
    {
      fData = Data;
      fMSB = MSB;
      fValueTypeStringData = ValueTypeStringData;
    }

    public LinkData(StreamWriter Writer)
      : base(LinkTypeData.instance)
    {
      fData = Writer.ToArray();
      fMSB = Writer.MSB;
      //  Values that describe this implementation
      fValueTypeStringData.ValueTypeStringLength = 0;
      fValueTypeStringData.ValueTypeIsUnicode = false;
    }

    private byte[] fData;
    public byte[] Data
    {
      get { return fData; }
    }

    private ValueTypeStringData fValueTypeStringData;
    public ValueTypeStringData ValueTypeStringData
    {
      get { return fValueTypeStringData; }
    }

    private bool fMSB;
    public bool MSB
    {
      get { return fMSB; }
    }

    public StreamReader Reader
    {
      get
      {
        return new StreamReaderSized(fData, fMSB);
      }
    }

    #region Link

    public override int Size()
    {
      return 5 + fData.Length;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, fValueTypeStringData.ValueTypeStringLengthX, fValueTypeStringData.ValueTypeStringLengthY, fValueTypeStringData.ValueTypeIsUnicode);
      Writer.WriteInt32(fData.Length);
      Writer.WriteBytes(fData);
    }

    public override void Action(LinkMessage Message)
    {
      //  Unlike most other link types, there is no action for data links.
      //  Data links are expected to contain data for other links, such as parameter data
      //  for a method link.
      throw new EMorph("Data links have no action.");
    }

    #endregion

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return fData.GetHashCode();
    }

    public override string ToString()
    {
      return "{Data Length=" + Data.Length.ToString() + '}';
    }
  }

  public struct ValueTypeStringData
  {
    public byte ValueTypeStringLength
    {
      get { return (byte)((ValueTypeStringLengthX ? 0x01 : 0x00) | (ValueTypeStringLengthY ? 0x02 : 0x00)); }
      set
      {
        ValueTypeStringLengthX = (ValueTypeStringLength & 0x01) != 0;
        ValueTypeStringLengthY = (ValueTypeStringLength & 0x02) != 0;
      }
    }
    public bool ValueTypeStringLengthX;
    public bool ValueTypeStringLengthY;

    public bool ValueTypeIsUnicode;

    static public ValueTypeStringData Default
    {
      get
      {
        ValueTypeStringData Result;
        Result.ValueTypeStringLengthX = false;
        Result.ValueTypeStringLengthY = false;
        Result.ValueTypeIsUnicode = false;
        return Result;
      }
    }
  }

  public class LinkTypeData : LinkType
  {
    static internal LinkTypeData instance = new LinkTypeData();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Data; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      ValueTypeStringData ValueTypeStringData;
      Reader.ReadLinkByte(out ValueTypeStringData.ValueTypeStringLengthX, out ValueTypeStringData.ValueTypeStringLengthY, out ValueTypeStringData.ValueTypeIsUnicode);
      int Size = Reader.ReadInt32();
      return new LinkData(Reader.ReadBytes(Size), ValueTypeStringData, Reader.MSB);
    }

    #endregion
  }
}