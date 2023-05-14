using Morph.Endpoint;
using Morph.Params;

namespace Morph.Daemon
{
  public class DaemonFactory : InstanceFactories
  {
    public DaemonFactory()
      : base()
    {
      Add(new CallbackFactory());
    }

    private class CallbackFactory : IReferenceFactory
    {
      #region IReferenceFactory Members

      public bool CreateReference(ServletProxy Value, out object Reference)
      {
        if (!"ServiceCallback".Equals(Value.TypeName))
        {
          Reference = null;
          return false;
        }
        else
        {
          Reference = new ServiceCallback(Value);
          return true;
        }
      }

      #endregion
    }
  }
}