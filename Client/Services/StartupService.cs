﻿using System;
using System.Threading.Tasks;
using IgiCore.SDK.Client.Events;
using IgiCore.SDK.Client.Rpc;
using IgiCore.SDK.Client.Services;
using IgiCore.SDK.Core.Diagnostics;

namespace IgiCore.Client.Services
{
	public class StartupService : Service
	{
		public StartupService(ILogger logger, ITickManager ticks, IEventManager events, IRpcHandler rpc) : base(logger, ticks, events, rpc)
		{
			this.Ticks.Attach(Tick);
		}

		private async Task Tick()
		{
			this.Logger.Debug("TICK");

			await Delay(TimeSpan.FromSeconds(1));
		}
	}
}
