using System.IO;
using System.Text;

namespace Morph.Lib
{
  internal class MorphStreamReader
  {
    public MorphStreamReader(MemoryStream Stream, bool MSB)
    {
      fStream = Stream;
      fMSB = MSB;
    }

    internal MemoryStream fStream;

    internal bool fMSB;

    private bool fPeeked = false;
    private int fPeek;

    private const byte BitX = 0x10;
    private const byte BitY = 0x20;
    private const byte BitZ = 0x40;
    private const byte BitMSB = 0x80;

    internal byte DecodeLinkByte(int LinkByte, out bool x, out bool y, out bool z)
    {
      x = (LinkByte & BitX) != 0;
      y = (LinkByte & BitY) != 0;
      z = (LinkByte & BitZ) != 0;
      fMSB = (LinkByte & BitMSB) != 0;
      return (byte)(LinkByte & 0x0F);
    }

    public int PeekInt8()
    {
      if (!fPeeked)
      {
        fPeek = fStream.ReadByte();
        fPeeked = true;
      }
      return fPeek;
    }

    public int ReadInt8()
    {
      if (fPeeked)
      {
        fPeeked = false;
        return fPeek;
      }
      else
        return fStream.ReadByte();
    }

    public int ReadInt16()
    {
      int result = ReadInt8();
      if (fMSB)
      {
        result <<= 8;
        result |= fStream.ReadByte();
      }
      else
      {
        result |= fStream.ReadByte() << 8;
      }
      return result;
    }

    public int ReadInt32()
    {
      int result = ReadInt8();
      if (fMSB)
      {
        result <<= 8;
        result |= fStream.ReadByte();
        result <<= 8;
        result |= fStream.ReadByte();
        result <<= 8;
        result |= fStream.ReadByte();
      }
      else
      {
        result |= fStream.ReadByte() << 8;
        result |= fStream.ReadByte() << 16;
        result |= fStream.ReadByte() << 24;
      }
      return result;
    }

    public long ReadInt64()
    {
      long result = ReadInt8();
      if (fMSB)
      {
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
        result <<= 8;
        result |= (byte)fStream.ReadByte();
      }
      else
      {
        result |= (byte)(fStream.ReadByte() << 8);
        result |= (byte)(fStream.ReadByte() << 16);
        result |= (byte)(fStream.ReadByte() << 24);
        result |= (byte)(fStream.ReadByte() << 32);
        result |= (byte)(fStream.ReadByte() << 40);
        result |= (byte)(fStream.ReadByte() << 48);
        result |= (byte)(fStream.ReadByte() << 56);
      }
      return result;
    }

    public byte[] ReadBytes(int Count)
    {
      byte[] bytes = new byte[Count];
      ReadBuffer(bytes, Count);
      return bytes;
    }

    private void ReadBuffer(byte[] Buffer, int Count)
    {
      int ReadCount;
      if (fPeeked)
      {
        fPeeked = false;
        Buffer[0] = (byte)fPeek;
        ReadCount = fStream.Read(Buffer, 1, Count - 1) + 1;
      }
      else
        ReadCount = fStream.Read(Buffer, 0, Count);
      if (ReadCount < Count)
        throw new EMorph("EOS");
    }
  }

  public abstract class StreamReader
  {
    static protected Encoding ASCII = new ASCIIEncoding();
    static protected Encoding Unicode = new UnicodeEncoding();

    internal MorphStreamReader fReader;

    public bool MSB
    {
      get { return fReader.fMSB; }
    }

    public abstract bool CanRead
    { get; }

    public abstract byte PeekLinkByte(out bool x, out bool y, out bool z);

    public abstract byte ReadLinkByte(out bool x, out bool y, out bool z);

    public abstract int PeekInt8();

    public abstract int ReadInt8();

    public abstract int ReadInt16();

    public abstract int ReadInt32();

    public abstract long ReadInt64();

    public abstract string ReadString();

    public abstract string ReadChars(int Length, bool AsUnicode);

    public abstract byte[] ReadBytes(int Count);

    public abstract StreamReaderSized SubReader(int Size);
  }

  public class StreamReaderSized : StreamReader
  {
    public StreamReaderSized(byte[] data)
    {
      fReader = new MorphStreamReader(new MemoryStream(data), true);
      fRemaining = data.Length;
    }

    public StreamReaderSized(byte[] data, bool MSB)
    {
      fReader = new MorphStreamReader(new MemoryStream(data), MSB);
      fRemaining = data.Length;
    }

    public StreamReaderSized(MemoryStream Stream, int Size)
    {
      fReader = new MorphStreamReader(Stream, true);
      fRemaining = Size;
    }

    internal StreamReaderSized(MorphStreamReader Reader, int Size)
    {
      fReader = Reader;
      fRemaining = Size;
    }

    private int fRemaining;
    public int Remaining
    {
      get { return fRemaining; }
    }

    private void Validate(int Size)
    {
      if (fRemaining < Size)
        throw new EMorph("EOS");
    }

    public override bool CanRead
    {
      get { return fReader.fStream.CanRead && (fRemaining > 0); }
    }

    public override byte PeekLinkByte(out bool x, out bool y, out bool z)
    {
      return fReader.DecodeLinkByte(PeekInt8(), out x, out y, out z);
    }

    public override byte ReadLinkByte(out bool x, out bool y, out bool z)
    {
      return fReader.DecodeLinkByte(ReadInt8(), out x, out y, out z);
    }

    public override int PeekInt8()
    {
      Validate(1);
      return fReader.PeekInt8();
    }

    public override int ReadInt8()
    {
      Validate(1);
      fRemaining--;
      return fReader.ReadInt8();
    }

    public override int ReadInt16()
    {
      Validate(2);
      fRemaining -= 2;
      return fReader.ReadInt16();
    }

    public override int ReadInt32()
    {
      Validate(4);
      fRemaining -= 4;
      return fReader.ReadInt32();
    }

    public override long ReadInt64()
    {
      Validate(8);
      fRemaining -= 8;
      return fReader.ReadInt64();
    }

    public override string ReadString()
    {
      int Length = ReadInt32();
      return ReadChars(Length, true);
    }

    public override string ReadChars(int Length, bool AsUnicode)
    {
      if (AsUnicode)
      {
        int Size = (int)Length * 2;
        Validate(Size);
        fRemaining -= Size;
        byte[] buffer = fReader.ReadBytes(Size);
        return StreamReader.Unicode.GetString(buffer, 0, buffer.Length);
      }
      else
      {
        int Size = (int)Length;
        Validate(Size);
        fRemaining -= Size;
        byte[] buffer = fReader.ReadBytes(Size);
        return StreamReader.ASCII.GetString(buffer, 0, buffer.Length);
      }
    }

    public override byte[] ReadBytes(int Count)
    {
      Validate(Count);
      fRemaining -= Count;
      return fReader.ReadBytes(Count);
    }

    public override StreamReaderSized SubReader(int Size)
    {
      if (Size > fRemaining)
        throw new EMorph("EOS");
      return new StreamReaderSized(fReader, Size);
    }

    public void MoveToEnd()
    {
      fReader.fStream.Seek(fRemaining, SeekOrigin.Current);
    }

    public void WriteTo(StreamWriter Writer)
    {
      fReader.fStream.WriteTo(Writer.fStream);
    }
  }

  public class StreamReaderSizeless : StreamReader
  {
    public StreamReaderSizeless(MemoryStream Stream)
    {
      fReader = new MorphStreamReader(Stream, true);
    }

    public override bool CanRead
    {
      get { return fReader.fStream.CanRead; }
    }

    public override byte PeekLinkByte(out bool x, out bool y, out bool z)
    {
      return fReader.DecodeLinkByte(fReader.PeekInt8(), out x, out y, out z);
    }

    public override byte ReadLinkByte(out bool x, out bool y, out bool z)
    {
      return fReader.DecodeLinkByte(fReader.ReadInt8(), out x, out y, out z);
    }

    public override int PeekInt8()
    {
      return fReader.PeekInt8();
    }

    public override int ReadInt8()
    {
      return fReader.ReadInt8();
    }

    public override int ReadInt16()
    {
      return fReader.ReadInt16();
    }

    public override int ReadInt32()
    {
      return fReader.ReadInt32();
    }

    public override long ReadInt64()
    {
      return fReader.ReadInt64();
    }

    public override string ReadString()
    {
      int Size = ReadInt32();
      return ReadChars(Size, true);
    }

    public override string ReadChars(int Length, bool AsUnicode)
    {
      if (AsUnicode)
      {
        byte[] buffer = fReader.ReadBytes(Length * 2);
        return StreamReader.Unicode.GetString(buffer, 0, buffer.Length);
      }
      else
      {
        byte[] buffer = fReader.ReadBytes(Length);
        return StreamReader.ASCII.GetString(buffer, 0, buffer.Length);
      }
    }

    public override byte[] ReadBytes(int Count)
    {
      return fReader.ReadBytes(Count);
    }

    public override StreamReaderSized SubReader(int Size)
    {
      return new StreamReaderSized(fReader, Size);
    }
  }
}