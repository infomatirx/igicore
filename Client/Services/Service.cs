﻿using System.Threading.Tasks;

namespace IgiCore.Client.Services
{
	public abstract class Service : IService
	{
		private bool Busy { get; set; } = false;

		protected abstract Task OnTick(Client client);

		public async Task Tick(Client client)
		{
			if (this.Busy) return;

			this.Busy = true;
			await this.OnTick(client);
			this.Busy = false;
		}
	}
}
