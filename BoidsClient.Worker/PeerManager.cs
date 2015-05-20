﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BoidsClient.Cmd;

namespace BoidsClient.Worker
{
    public class PeerManager
    {
        private List<Peer> _peers = new List<Peer>();
        public int RunningInstances
        {
            get
            {
                return _peers.Count;
            }
        }

        private int i;
        private int _currentTargetInstanceCount;

        public void SetInstanceCount(int count)
        {
            if(count < 0)
            {
                throw new ArgumentException("Count must be positive.");
            }
            if (count != _currentTargetInstanceCount)
            {
                _currentTargetInstanceCount = count;

                while(_currentTargetInstanceCount != RunningInstances)
                {
                    if(_currentTargetInstanceCount < RunningInstances)
                    {
                        RemoveInstance();
                        
                    }

                    if(_currentTargetInstanceCount > RunningInstances)
                    {
                        AddInstance();
                    }
                }

            }

        }

        private void RemoveInstance()
        {
            var peer = _peers.Last();
            _peers.Remove(peer);
            peer.Proxy.Stop();
            AppDomain.Unload(peer.Domain);
        }

        private void AddInstance()
        {
            var name = "peer" + i++;
            var domain = AppDomain.CreateDomain(name,null,AppDomain.CurrentDomain.BaseDirectory,"",true);
            var path = domain.BaseDirectory;
            var proxy = (PeerProxy)domain.CreateInstanceAndUnwrap(typeof(PeerProxy).Assembly.FullName, typeof(PeerProxy).FullName);
            var peer = new Peer { Domain = domain, Proxy = proxy };
            _peers.Add(peer);
            
            proxy.Start(name);
        }

      

        private class Peer
        {
            public AppDomain Domain { get; set; }
            public PeerProxy Proxy { get; set; }
        }
    }
}
