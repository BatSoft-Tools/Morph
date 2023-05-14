using System.IO;
using System.Text;

namespace Morph.Lib
{
  public class StreamWriter
  {
    public StreamWriter(MemoryStream Stream)
    {
      fStream = Stream;
    }

    static private Encoding ASCII = new ASCIIEncoding();
    static private Encoding Unicode = new UnicodeEncoding();

    internal MemoryStream fStream;
    private const int BufferSize = 2048;

    public Stream Stream
    {
      get { return fStream; }
    }

    private const byte Zero = 0x00;
    private const byte BitX = 0x10;
    private const byte BitY = 0x20;
    private const byte BitZ = 0x40;
    private const byte BitMSB = 0x80;

    public bool MSB
    {
      get { return true; }
    }

    public void WriteLinkByte(byte LinkType, bool x, bool y, bool z)
    {
      WriteInt8(BitMSB | (z ? BitZ : Zero) | (y ? BitY : Zero) | (x ? BitX : Zero) | (LinkType & 0x0F));
    }

    public void WriteInt8(int Value)
    {
      fStream.WriteByte((byte)Value);
    }

    public void WriteInt16(int Value)
    {
      fStream.WriteByte((byte)(Value >> 8));
      fStream.WriteByte((byte)(Value));
    }

    public void WriteInt32(int Value)
    {
      fStream.WriteByte((byte)(Value >> 24));
      fStream.WriteByte((byte)(Value >> 16));
      fStream.WriteByte((byte)(Value >> 8));
      fStream.WriteByte((byte)(Value));
    }

    public void WriteInt64(long Value)
    {
      fStream.WriteByte((byte)(Value >> 56));
      fStream.WriteByte((byte)(Value >> 48));
      fStream.WriteByte((byte)(Value >> 40));
      fStream.WriteByte((byte)(Value >> 32));
      fStream.WriteByte((byte)(Value >> 24));
      fStream.WriteByte((byte)(Value >> 16));
      fStream.WriteByte((byte)(Value >> 8));
      fStream.WriteByte((byte)(Value));
    }

    public void WriteString(string Value)
    {
      WriteInt32(Value.Length);
      WriteChars(Value, true);
    }

    public void WriteChars(string Chars, bool AsUnicode)
    {
      byte[] buffer;
      if (AsUnicode)
        buffer = Unicode.GetBytes(Chars);
      else
        buffer = ASCII.GetBytes(Chars);
      fStream.Write(buffer, 0, buffer.Length);
    }

    public void WriteBytes(byte[] Data)
    {
      fStream.Write(Data, 0, Data.Length);
    }

    public void WriteStream(StreamReaderSized Reader)
    {
      Reader.fReader.fStream.WriteTo(fStream);
    }

    public void WriteStream(StreamReaderSized Reader, int Count)
    {
      if (Count == 0)
        return;
      byte[] Buffer = new byte[BufferSize];
      int Length;
      do
      {
        Length = Reader.fReader.fStream.Read(Buffer, 0, BufferSize);
        fStream.Write(Buffer, 0, Length);
        Count -= Length;
      }
      while ((Count > 0) && (Length > 0));
      if (Count > 0)
        throw new EMorph("EOS");
    }

    public byte[] ToArray()
    {
      return fStream.ToArray();
    }
  }
}