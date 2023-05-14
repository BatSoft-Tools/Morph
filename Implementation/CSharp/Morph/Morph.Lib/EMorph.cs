using System;

namespace Morph
{
  public class EMorph : Exception
  {
    public EMorph(string message)
      : base(message)
    {
    }

    private EMorph(string message, string MorphTrace)
      : base(message)
    {
      fStackTrace = MorphTrace;
    }

    private int fErrorNumber = Any;
    public int ErrorNumber
    {
      get { return fErrorNumber; }
    }

    public const int None = 0;
    public const int Any = -1;

    private string fStackTrace = null;
    public override string StackTrace
    {
      get
      {
        if (fStackTrace != null)
          return fStackTrace;
        return base.StackTrace;
      }
    }

    static public void Throw(int ErrorNumber, string MorphTrace)
    {
      throw new EMorph("Morph error: " + ErrorNumber.ToString(), MorphTrace);
    }
  }

  public class EMorphImplementation : EMorph
  {
    public EMorphImplementation()
      : base("Morph implementation error")
    {
    }
  }

  public class EMorphUsage : EMorph
  {
    public EMorphUsage(string Message)
      : base(Message)
    {
    }
  }
}