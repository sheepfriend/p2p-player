﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using TransportService;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using Kademlia;
using System.ServiceModel.Description;
using Persistence;
using System.Threading.Tasks;
using log4net;
using System.Net;
using System.Net.Sockets;

namespace PeerPlayer
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    class Peer : IDisposable, IPeer
    {
        private TransportProtocol transportLayer;
        private static readonly ILog log = LogManager.GetLogger(typeof(Peer));
        private Dht kademliaLayer;
        private Stream localStream;
        private ServiceHost[] svcHosts = new ServiceHost[3];
        private bool single;
        private string btpNode;
        private Persistence.Repository trackRep;
        private string peerAddress;

        public Dictionary<string, string> ConfOptions {get; set;}

        public Peer(bool single = false, string btpNode = "")
        {
            log.Debug("Initializing peer structure");
            this.ConfOptions = new Dictionary<string, string>();
            this.ConfOptions["udpPort"] = PeerPlayer.Properties.Settings.Default.udpPort;
            this.ConfOptions["kadPort"] = PeerPlayer.Properties.Settings.Default.kademliaPort;
            this.localStream = new MemoryStream();
            this.single = single;
            this.btpNode = btpNode;
            AppSettingsReader asr = new AppSettingsReader();
            Persistence.RepositoryConfiguration conf = new Persistence.RepositoryConfiguration(new { data_dir = (string)asr.GetValue("TrackRepository", typeof(string)) });
            this.trackRep = Persistence.RepositoryFactory.GetRepositoryInstance("Raven", conf);
            IPHostEntry IPHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] listaIP = IPHost.AddressList;
            foreach (IPAddress ip in listaIP)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.peerAddress = ip.ToString();
                    break;
                }
            }
        }

        public void runLayers(bool withoutInterface=false)
        {
            log.Debug("Running layers...");
            svcHosts[0] = this.runKademliaLayer(single, btpNode);
            svcHosts[1] = this.runTransportLayer();
            if(!withoutInterface)
                svcHosts[2] = this.runInterfaceLayer();
        }

        #region layersInitialization
        private ServiceHost runInterfaceLayer()
        {
            log.Info("Running Interface Layer.");
            ServiceHost host = new ServiceHost(this);
            try
            {
                host.Open();
            }
            catch (AddressAlreadyInUseException aaiue)
            {
                log.Error("Unable to Connect as a Server because there is already one on this machine", aaiue);
            }
            foreach (Uri uri in host.BaseAddresses)
            {
                log.Info(uri.ToString());
            }
            return host;
        }

        private ServiceHost runTransportLayer()
        {
            log.Info("Running Transport Layer.");
            string udpPort = this.ConfOptions["udpPort"];
            Uri[] addresses = new Uri[1];
            addresses[0] = new Uri("soap.udp://"+this.peerAddress+":" + udpPort + "/transport_protocol/");
            TransportProtocol tsp = new TransportProtocol(addresses[0], this.trackRep);
            this.transportLayer = tsp;
            ServiceHost host = new ServiceHost(tsp, addresses);
            try
            {
                host.Open();
                #region Output dispatchers listening
                foreach (Uri uri in host.BaseAddresses)
                {
                    log.Info(uri.ToString());
                }
                log.Info("Number of dispatchers listening : " + host.ChannelDispatchers.Count);
                foreach (System.ServiceModel.Dispatcher.ChannelDispatcher dispatcher in host.ChannelDispatchers)
                {
                    log.Info(dispatcher.Listener.Uri.ToString());
                }
                #endregion
            }
            catch (AddressAlreadyInUseException aaiue)
            {
                log.Error("Unable to Connect as a Server because there is already one on this machine", aaiue);
                throw aaiue;
            }
            return host;
        }

        private ServiceHost runKademliaLayer(bool single, string btpNode)
        {
            log.Info("Running Kademlia layer.");
            string kademliaPort = this.ConfOptions["kadPort"];
            KademliaNode node = new KademliaNode(new EndpointAddress("soap.udp://"+this.peerAddress+":"+kademliaPort+"/kademlia"));
            ServiceHost kadHost = new ServiceHost(node, new Uri("soap.udp://"+this.peerAddress+":" + kademliaPort + "/kademlia"));
            try
            {
                kadHost.Open();
            }
            catch (AddressAlreadyInUseException aaiue)
            {
                log.Error("Unable to Connect as a Server because there is already one on this machine", aaiue);
                throw aaiue;
            }
            this.kademliaLayer = new Dht(node, single, btpNode);
            List<TrackModel.Track> list = new List<TrackModel.Track>();
            log.Debug("GetAll Response : " + this.trackRep.GetAll(list));
            Parallel.ForEach(list, t =>
            {
                this.kademliaLayer.Put(t.Filename);
            });
            return kadHost;
        }
        #endregion

        #region interface

        public void Configure(string udpPort = "-1", string kademliaPort = "-1")
        {
            log.Info("Reconfiguring peer with " + (udpPort != "-1" ? "udpPort=" + udpPort : "") + (kademliaPort != "-1" ? "kademliaPort=" + kademliaPort : ""));
            if (udpPort != "-1")
            {
                PeerPlayer.Properties.Settings.Default.udpPort = udpPort;
            }
            if (kademliaPort != "-1")
            {
                PeerPlayer.Properties.Settings.Default.kademliaPort = kademliaPort;
            }
            PeerPlayer.Properties.Settings.Default.Save();
        }

        public Stream ConnectToStream()
        {
            log.Info("Returning stream to requestor.");
            return this.localStream;
        }

        public void GetFlow(string RID, int begin, int length, Dictionary<string, float> nodes)
        {
            this.GetFlow(RID, begin, length, nodes, null);
        }
        public void GetFlow(string RID, int begin, int length, Dictionary<string, float> nodes, Stream stream = null)
        {
            log.Info("Beginning to get flow from the network");
            Stream handlingStream = stream;
            if (handlingStream == null)
            {
                handlingStream = this.localStream;
            }
            this.transportLayer.Start(RID, begin, length, nodes, handlingStream);
        }

        public void StopFlow()
        {
            log.Info("Stop flow.");
            this.transportLayer.Stop();
        }

        public void StoreFile(string filename)
        {
            log.Info("Storing file:" + filename);
            TrackModel track = new TrackModel(filename);
            this.trackRep.Save(track);
        }

        public IList<KademliaResource> SearchFor(string queryString)
        {
            return this.kademliaLayer.GetAll(queryString);
        }

        #endregion

        static void Main(string[] args)
        {
            bool withoutInterface = false;
            using (Peer p = new Peer())
            {
                if (args.Length % 2 != 0)
                {
                    log.Error("Error in parsing options");
                    return;
                }
                else
                {
                    bool storeConf = false;
                    for (int i = 0; i < args.Length; i += 2)
                    {
                        if (args[i] == "--udpPort" || args[i] == "-u")
                        {
                            p.ConfOptions["udpPort"] = args[i + 1];
                        }
                        else if (args[i] == "--kadPort" || args[i] == "-k")
                        {
                            p.ConfOptions["kadPort"] = args[i + 1];
                        }
                        else if ((args[i] == "--store" || args[i] == "-s") && (args[i + 1] == "1"))
                        {
                            storeConf = true;
                        }
                        else if ((args[i] == "--without_interface" || args[i] == "-i") && (args[i + 1] == "1"))
                        {
                            withoutInterface = true;
                        }
                    }
                    if (storeConf)
                    {
                        p.Configure(p.ConfOptions["udpPort"], p.ConfOptions["kadPort"]);
                    }
                }
                p.runLayers(withoutInterface);
                Console.WriteLine();
                Console.WriteLine("Press <ENTER> to terminate Host");
                Console.ReadLine();
            }
        }

        public void Dispose()
        {
            log.Info("Disposing Peer");
            foreach (ServiceHost svcHost in this.svcHosts)
            {
                if(svcHost != null)
                    svcHost.Close();
            }
            this.trackRep.Dispose();
        }
    }
}