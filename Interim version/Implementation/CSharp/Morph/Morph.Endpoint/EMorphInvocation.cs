namespace Morph.Endpoint
{
  public class EMorphInvocation : EMorph
  {
    public EMorphInvocation(string ClassName, string Message, string StackTrace)
      : base(Message)
    {
      fClassName = ClassName;
      fStackTrace = StackTrace;
    }

    private string fClassName;
    public string ClassName
    {
      get { return fClassName; }
    }

    private string fStackTrace;
    public override string StackTrace
    {
      get
      {
        return fStackTrace;
      }
    }
  }
}
