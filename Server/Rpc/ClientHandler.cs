﻿using System;
using CitizenFX.Core;

namespace IgiCore.Server.Rpc
{
	public class ClientHandler
	{
		private readonly EventHandlerDictionary events; // TODO: Custom type?

		public ClientHandler(EventHandlerDictionary events)
		{
			this.events = events;
		}

		public void Attach(string @event, Delegate callback)
		{
			this.events[@event] += callback;
		}

		public void Detach(string @event, Delegate callback)
		{
			this.events[@event] -= callback;
		}
	}
}
