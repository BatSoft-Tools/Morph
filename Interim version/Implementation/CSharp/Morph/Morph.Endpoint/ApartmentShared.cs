using Morph.Params;

namespace Morph.Endpoint
{
  public class DefaultServletObjectFactoryShared : DefaultServletObjectFactory
  {
    public DefaultServletObjectFactoryShared(object DefaultServletObject)
    {
      fDefaultServletObject = DefaultServletObject;
    }

    #region DefaultServiceFactory Members

    private object fDefaultServletObject;
    public object ObtainServlet()
    {
      return fDefaultServletObject;
    }

    #endregion
  }

  public class ApartmentFactoryShared : ApartmentFactory
  {
    public ApartmentFactoryShared(object DefaultServletObject, InstanceFactories InstanceFactories)
      : base(InstanceFactories)
    {
      fApartment = new Apartment(this, DefaultServletObject);
      if (DefaultServletObject is IMorphReference)
        ((IMorphReference)DefaultServletObject).MorphApartment = fApartment;
    }

    private Apartment fApartment;

    public override Apartment ObtainDefault()
    {
      return fApartment;
    }

    protected internal override void ShutDown()
    {
      base.ShutDown();
      //  If you wish to add in special shut down code for the service, 
      //  then you can make your own Apartment factory by extending any
      //  of the classes ApartmentFactory, ApartmentsShared, ApartmentsSession.
    }
  }
}