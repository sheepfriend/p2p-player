﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TransportService
{
    internal class PeerQueue
    {
        private Dictionary<string, PeerQueueElement> peerQueue;
        private AutoResetEvent peerQueueNotEmpty = new AutoResetEvent(true);

        public PeerQueue()
        {
        }

        public PeerQueue(Dictionary<string, float> peerQueue)
        {
            //this.peerQueue = new PeerQueueElement[peerQueue.Count];
            //var rangePartitioner = Partitioner.Create(0, peerQueue.Count);
            this.peerQueue = new Dictionary<string,PeerQueueElement>();
/*            Parallel.ForEach(peerQueue, p =>
            {
                this.peerQueue[p.Key] = new PeerQueueElement(p.Key, p.Value, ref peerQueueNotEmpty);
            });*/
            foreach (string key in peerQueue.Keys)
            {
                this.peerQueue[key] = new PeerQueueElement(key, peerQueue[key], ref peerQueueNotEmpty);
            }
        }

        public string GetBestPeer()
        {
            while (this.peerQueue.AsParallel().Where(p => p.Value.State == PeerQueueElement.ThreadState.FREE).Count() <= 0)
            {
                this.peerQueueNotEmpty.Reset();
                this.peerQueueNotEmpty.WaitOne();
            }
            PeerQueueElement best = this.peerQueue.AsParallel().Aggregate((l, r) => l.Value.PeerScore < r.Value.PeerScore ? l : (r.Value.PeerScore < l.Value.PeerScore ? r : (new Random().Next(2)) == 0 ? l : r )).Value;
            return best.PeerAddress;
        }

        public void ResetPeer(string key, int newScore)
        {
            this.peerQueue[key].PeerScore = newScore;
            this.peerQueue[key].Reset();
        }

        public PeerQueueElement this[string key]
        {
            get
            {
                return this.peerQueue[key];
            }
            set
            {
                this.peerQueue[key] = value;
            }
        }
    }
}