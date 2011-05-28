﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Kademlia.Messages
{
	/// <summary>
	/// Represents a generic DHT RPC message
	/// </summary>
	[DataContract]
	public abstract class Message
	{
		// All messages include sender id
		private ID senderID;
        private Uri nodeEndpoint;
		private ID conversationID;
		
		/// <summary>
		/// Make a new message, recording the sender's ID.
		/// </summary>
		/// <param name="senderID"></param>
		public Message(ID senderID, Uri nodeEndpoint) {
			this.senderID = senderID;
			conversationID = ID.RandomID();
            this.nodeEndpoint = nodeEndpoint;
		}
		
		/// <summary>
		/// Make a new message in a given conversation.
		/// </summary>
		/// <param name="senderID"></param>
		/// <param name="conversationID"></param>
		public Message(ID senderID, ID conversationID, Uri nodeEndpoint) {
			this.senderID = senderID;
			this.conversationID = conversationID;
            this.nodeEndpoint = nodeEndpoint;
		}
		
		/// <summary>
		/// Get the name of the message.
		/// </summary>
		/// <returns></returns>
        [DataMember]
        public abstract string Name
        {
            get;
            set;
        }
		
		/// <summary>
		/// Get the ID of the sender of the message.
		/// </summary>
		/// <returns></returns>
        [DataMember]
		public ID SenderID
        {
            get { return senderID; }
            set { this.senderID = value; }
		}
		
		/// <summary>
		/// Gets the ID of this conversation.
		/// </summary>
		/// <returns></returns>
        [DataMember]
		public ID ConversationID
        {
            get { return conversationID; }
            set { this.conversationID = value; }
		}

        [DataMember]
        public Uri NodeEndpoint
        {
            get { return nodeEndpoint; }
            set { this.nodeEndpoint = value; }
        }
	}
}