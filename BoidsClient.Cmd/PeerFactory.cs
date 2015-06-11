﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoidsClient.Cmd
{

    public class PeerProxy : MarshalByRefObject
    {
        private Peer _peer;
        public void Start(string name, string accountId, string app, string scene)
        {
            try
            {
                _peer = new Peer(name, accountId, app, scene);
                _peer.Stopped = () =>{
                    var stopped = Stopped;
                    if(stopped !=null)
                    {
                        stopped();
                    }
                };
                _peer.Start();
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                throw;
            }


        }

        public Action Stopped { get; set; }

        public void Stop()
        {
            _peer.Stop();
        }

    }
}
