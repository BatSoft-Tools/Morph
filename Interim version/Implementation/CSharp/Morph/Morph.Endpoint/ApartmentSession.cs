using System;
using System.Threading;
using Morph.Lib.LinkedList;
using Morph.Params;
using Morph.Sequencing;

namespace Morph.Endpoint
{
  public class ApartmentSession : Apartment, IDisposable
  {
    internal protected ApartmentSession(ApartmentFactory Owner, object DefaultObject, SequenceLevel Level)
      : base(Owner, DefaultObject)
    {
      if (Level != SequenceLevel.None)
        fSequence = Sequences.New(null, Level == SequenceLevel.Lossless);
    }

    #region IDisposable Members

    public override void Dispose()
    {
      base.Dispose();
      if (fSequence != null)
        fSequence.Stop(true);
    }

    #endregion

    internal DateTime When;
    internal Bookmark Bookmark = null;

    public override void ResetTimeout()
    {
      ((ApartmentFactorySession)Owner).ResetTimeout(this);
    }

    public LinkStack fPath = null;
    public LinkStack Path
    {
      get { return fPath; }
      set
      {
        if (value != null)
          lock (this)
          {
            fPath = value;
            //  Apply path to Sequence
            if (fSequence != null)
              fSequence.PathToProxy = fPath;
          }
      }
    }

    public Sequence fSequence;
    public SequenceLevel SequenceLevel
    {
      get
      {
        if (fSequence == null)
          return SequenceLevel.None;
        if (fSequence.IsLossless)
          return SequenceLevel.Lossless;
        else
          return SequenceLevel.Lossy;
      }
    }

    internal override void AppendSequenceLinks(LinkStack Path)
    {
      if (SequenceLevel != SequenceLevel.None)
        Path.Append(fSequence.StartLink());
    }
  }

  public class ApartmentFactorySession : ApartmentFactory, IDisposable
  {
    public ApartmentFactorySession(DefaultServletObjectFactory DefaultServletObject, InstanceFactories InstanceFactories, TimeSpan Timeout, SequenceLevel sequenceLevel)
      : base(InstanceFactories)
    {
      fDefaultServletObjectFactory = DefaultServletObject;
      fTimeout = Timeout;
      fSequenceLevel = sequenceLevel;
      new Thread(new ThreadStart(ThreadExecute));
    }

    #region IDisposable Members

    public void Dispose()
    {
      ThreadRunning = false;
      fThreadWait.Set();
      fTimeouts.Dispose();
    }

    #endregion

    #region Timeouts

    private bool ThreadRunning = true;
    private AutoResetEvent fThreadWait = new AutoResetEvent(false);
    private TimeSpan fTimeout;
    private LinkedListTwoWay<ApartmentSession> fTimeouts = new LinkedListTwoWay<ApartmentSession>();

    private void ThreadExecute()
    {
      while (ThreadRunning)
      {
        //  Are there any apartments to wait for?
        ApartmentSession apartment;
        lock (fTimeouts)
          apartment = fTimeouts.PeekLeft();
        //  No, so wait until triggered
        if (apartment == null)
          fThreadWait.WaitOne();
        else
        { //  Might need to wait
          int Wait = apartment.When.Subtract(DateTime.Now).Milliseconds;
          if (Wait > 0)
            fThreadWait.WaitOne(Wait, false);
          else
          { //  Timed out, so remove apartment
            ApartmentFactory.UnregisterApartment(apartment);
            lock (fTimeouts)
              fTimeouts.Pop(apartment.Bookmark);
          }
        }
      }
    }

    internal void ResetTimeout(ApartmentSession Apartment)
    {
      Apartment.When = DateTime.Now.Add(fTimeout);
      lock (fTimeouts)
        fTimeouts.MoveToRightEnd(Apartment.Bookmark);
    }

    #endregion

    private DefaultServletObjectFactory fDefaultServletObjectFactory;

    protected virtual ApartmentSession CreateApartment(object DefaultObject, SequenceLevel Level)
    {
      return new ApartmentSession(this, DefaultObject, Level);
    }

    public override Apartment ObtainDefault()
    {
      //  Create session apartment
      object DefaultObject = fDefaultServletObjectFactory.ObtainServlet();
      ApartmentSession apartment = CreateApartment(DefaultObject, fSequenceLevel);
      if (DefaultObject is IMorphReference)
        ((IMorphReference)DefaultObject).MorphApartment = apartment;
      //  Track timeout
      apartment.When = DateTime.Now.Add(fTimeout);
      lock (fTimeouts)
      {
        if (!fTimeouts.HasData)
          fThreadWait.Set();
        apartment.Bookmark = fTimeouts.PushRight(apartment);
      }
      return apartment;
    }

    protected internal override void ShutDown()
    {
      base.ShutDown();
      //  If you wish to add in special shut down code for the service, 
      //  then you can make your own Apartment factory by extending any
      //  of the classes ApartmentFactory, ApartmentsShared, ApartmentsSession.
    }

    private SequenceLevel fSequenceLevel;
    public SequenceLevel SequenceLevel
    {
      get { return fSequenceLevel; }
    }
  }
}