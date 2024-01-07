using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Morph.Params;

namespace Morph.Daemon
{
  public class AwareObject<TKey> : IDisposable
  {
    protected AwareObject(AwareObjects<TKey> Owner, TKey Key)
    {
      _Key = Key;
      _Owner = Owner;
      lock (_Owner._Lock)
      {
        _Owner._Keys.Add(_Key);
        _Owner._Elems.Add(_Key, this);
      }
    }

    private AwareObjects<TKey> _Owner;

    internal TKey _Key;

    #region IDisposable Members

    public virtual void Dispose()
    {
      lock (_Owner._Lock)
      {
        _Owner._Keys.Remove(_Key);
        _Owner._Elems.Remove(_Key);
      }
    }

    #endregion
  }

  public class AwareSocketObject<TKey> : AwareObject<TKey>
  {
    protected AwareSocketObject(AwareObjects<TKey> Owner, TKey Key, Socket Socket)
      : base(Owner, Key)
    {
      _Socket = Socket;
      SocketObjectManager.Register(_Socket, this);
    }

    private Socket _Socket;
    public Socket Socket
    {
      get { return _Socket; }
    }

    public override void Dispose()
    {
      base.Dispose();
      SocketObjectManager.Deregister(_Socket, this);
    }
  }

  public class AwareObjects<TKey> : MorphReference
  {
    protected AwareObjects(string TypeName)
      : base(TypeName)
    {
    }

    internal object _Lock = new Object();
    internal List<TKey> _Keys = new List<TKey>();
    internal Hashtable _Elems = new Hashtable();

    protected void Clear()
    {
      lock (_Lock)
        _Elems.Clear();
    }

    protected AwareObject<TKey> FindByKey(TKey Key)
    {
      lock (_Lock)
        return (AwareObject<TKey>)_Elems[Key];
    }

    protected void DeleteByKey(TKey Key)
    {
      AwareObject<TKey> Obj = FindByKey(Key);
      if (Obj != null)
        Obj.Dispose();
    }

    protected TKey[] ListKeys()
    {
      lock (_Lock)
        return _Keys.ToArray();
    }

    protected AwareObject<TKey>[] ListElems()
    {
      lock (_Lock)
        if (_Elems.Count == 0)
          return null;
        else
        {
          AwareObject<TKey>[] array = new AwareObject<TKey>[_Elems.Count];
          _Elems.Values.CopyTo(array, 0);
          return array;
        }
    }
  }
}