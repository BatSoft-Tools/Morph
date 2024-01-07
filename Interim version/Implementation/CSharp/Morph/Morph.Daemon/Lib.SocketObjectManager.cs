/**
 * This keeps track of all objects that are related to each socket.
 * So when a socket is removed, for whatever reason, then this will free all
 * objects associated with that socket.  This is how the Morph Daemon remains
 * stable regardless of unreliabilities in networks or other applications.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Morph.Internet;
using Morph.Lib;

namespace Morph.Daemon
{
  public static class SocketObjectManager
  {
    #region Internal

    static SocketObjectManager()
    {
      MorphErrors.Event += OnDownedSocket;
    }

    private class SocketObjects : List<IDisposable>, IDisposable
    {
      public SocketObjects(Socket Socket)
      {
        _Socket = Socket;
      }

      public Socket _Socket;

      #region IDisposable Members

      public void Dispose()
      {
        lock (this)
          for (int i = Count - 1; i >= 0; i--)
            this[i].Dispose();
      }

      #endregion
    }

    static private Hashtable _SocketObjects = new Hashtable();

    static private SocketObjects Find(Socket Socket)
    {
      lock (_SocketObjects)
        return (SocketObjects)_SocketObjects[Socket];
    }

    static private void OnDownedSocket(object sender, ExceptionArgs e)
    {
      if (!(sender is Connection))
        return;
      Socket socket = ((Connection)sender).Socket;
      if (socket != null)
        CloseSocket(socket);
    }

    #endregion

    static public void Register(Socket Socket, IDisposable Obj)
    {
      //  Find objects related to the socket
      SocketObjects Objs;
      lock (_SocketObjects)
      {
        Objs = (SocketObjects)_SocketObjects[Socket];
        if (Objs == null)
        {
          Objs = new SocketObjects(Socket);
          _SocketObjects.Add(Socket, Objs);
        }
        //  Relate Obj to the socket
        lock (Objs)
          Objs.Add(Obj);
      }
    }

    static public void Deregister(Socket Socket, IDisposable Obj)
    {
      //  Find objects related to the socket
      SocketObjects Objs;
      lock (_SocketObjects)
      {
        Objs = (SocketObjects)_SocketObjects[Socket];
        //  Un-relate Obj to the socket
        if (Objs != null)
          lock (Objs)
            if (Objs.Remove(Obj))
              Obj.Dispose();
      }
    }

    static public void CloseSocket(Socket Socket)
    {
      SocketObjects Objs;
      //  Find objects related to the socket
      lock (_SocketObjects)
      {
        Objs = (SocketObjects)_SocketObjects[Socket];
        if (Objs != null)
          _SocketObjects.Remove(Objs._Socket);
      }
      //  Tidy them all up
      if (Objs != null)
        lock (Objs)
        {
          for (int i = 0; i < Objs.Count; i++)
            Objs[i].Dispose();
          Objs.Dispose();
        }
    }
  }
}