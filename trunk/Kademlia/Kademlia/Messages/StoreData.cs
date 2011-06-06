﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using Persistence.Tag;

namespace Kademlia.Messages
{
	/// <summary>
	/// Send along the data in response to an affirmative StoreResponse.
	/// </summary>
	[DataContract]
	public class StoreData : Response
	{
		private CompleteTag data;
		private DateTime publication;
		
		/// <summary>
		/// Make a mesage to store the given data.
		/// </summary>
		/// <param name="nodeID"></param>
		/// <param name="request"></param>
		/// <param name="theKey"></param>
		/// <param name="theDataHash"></param>
		/// <param name="theData"></param>
		/// <param name="originalPublication"></param>
		public StoreData(ID nodeID, StoreResponse request, CompleteTag theData, DateTime originalPublication, Uri nodeEndpoint) : base(nodeID, request, nodeEndpoint)
		{
			data = theData;
			publication = originalPublication;
		}
		
		/// <summary>
		/// Get the data to store.
		/// </summary>
		/// <returns></returns>
        [DataMember]
		public CompleteTag Data
		{
            get { return data; }
            set { this.data = value; }
		}
		
		/// <summary>
		/// Get when the data was originally published, in UTC.
		/// </summary>
		/// <returns></returns>
        [DataMember]
		public DateTime PublicationTime
		{
            get { return publication.ToUniversalTime(); }
            set { }
		}
		
        [DataMember]
		public override string Name
		{
            get { return "STORE_DATA"; }
            set { }
		}
	}
}