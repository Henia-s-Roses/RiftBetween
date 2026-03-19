// RiftNetworkDiscovery.cs

// extends mirror's network discovery for specific use

using Mirror;
using Mirror.Discovery;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RiftNetworkDiscovery : NetworkDiscovery
{
    // action when server is found
    public static event Action<ServerResponse> OnServerFound;

    // invokes the event when a server is found
    protected override void ProcessResponse(ServerResponse response, System.Net.IPEndPoint endpoint)
    {
        response.EndPoint = endpoint;
        OnServerFound?.Invoke(response);
    }
}