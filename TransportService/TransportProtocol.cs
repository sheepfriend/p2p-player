﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace TransportService
{
    // NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di classe "Service1" nel codice, nel file svc e nel file di configurazione contemporaneamente.
    public class TransportProtocol : ITransportProtocol
    {
        public string GetData(int value)
        {
            Console.WriteLine(string.Format("Received: {0}", value));
            return string.Format("You entered: {0}", value);
        }

        public ChunkResponse GetChunk(ChunkRequest chkr)
        {
            return new ChunkResponse();
        }

        //public CompositeType GetDataUsingDataContract(CompositeType composite)
        //{
        //    if (composite == null)
        //    {
        //        throw new ArgumentNullException("composite");
        //    }
        //    if (composite.BoolValue)
        //    {
        //        composite.StringValue += "Suffix";
        //    }
        //    return composite;
        //}

    }
}