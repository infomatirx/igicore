﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CitizenFX.Core;
using IgiCore.SDK.Server.Controllers;
using IgiCore.Server.Configuration;
using IgiCore.Server.Controllers;
using IgiCore.Server.Diagnostics;
using IgiCore.Server.Plugins;
using IgiCore.Server.Rpc;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IgiCore.Server
{
	[UsedImplicitly]
	public class Program : BaseScript
	{
		private readonly Logger logger = new Logger();
		private readonly List<Controller> controllers = new List<Controller>();

		public Program()
		{
			// Set the AppDomain working directory to the current resource root
			Environment.CurrentDirectory = FileManager.ResolveResourcePath();

			RpcManager.Configure(this.EventHandlers);

			this.controllers.Add(new DatabaseController(new Logger("Database"), new RpcHandler(), Load<DatabaseConfiguration>("database")));
			this.controllers.Add(new SessionController(new Logger("Session"), new RpcHandler()));
			this.controllers.Add(new ClientController(new Logger("Client"), new RpcHandler()));

			//Client.Event(RpcEvents.GetServerInformation).On(ClientController.Ready);
			//Client.Event(RpcEvents.ClientDisconnect).On(ClientController.Disconnect);

			// Parse the master plugin definition file
			ServerPluginDefinition definition = PluginManager.LoadDefinition();

			// Resolve dependencies
			PluginDefinitionGraph dependencyGraph = definition.ResolveDependencies();
			//this.logger.Debug($"{JsonConvert.SerializeObject(dependencyGraph, Formatting.Indented)}");

			// Load plugins into the AppDomain
			foreach (ServerPluginDefinition plugin in dependencyGraph.Definitions)
			{
				// Load include files
				foreach (var includeName in plugin.Definition.Server.Include)
				{
					var includeFile = Path.Combine(plugin.Location, $"{includeName}.net.dll");
					if (!File.Exists(includeFile)) throw new FileNotFoundException(includeFile);

					AppDomain.CurrentDomain.Load(File.ReadAllBytes(includeFile));
				}

				// Load main files
				foreach (var mainName in plugin.Definition.Server.Main)
				{
					var mainFile = Path.Combine(plugin.Location, $"{mainName}.net.dll");
					if (!File.Exists(mainFile)) throw new FileNotFoundException(mainFile);

					// Find controllers
					foreach (Type controllerType in Assembly.LoadFrom(mainFile).GetTypes().Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(Controller)) || t.IsSubclassOf(typeof(ConfigurableController<>)))))
					{
						var constructorArgs = new List<object>
						{
							new Logger($"Plugin] [{plugin.Definition.Name}"),
							new RpcHandler()
						};

						if (controllerType.BaseType != null && controllerType.BaseType.IsGenericType && controllerType.BaseType.GetGenericTypeDefinition() == typeof(ConfigurableController<>))
						{
							var configurationType = controllerType.BaseType.GetGenericArguments()[0];

							var configFile = Path.Combine("config", $"{plugin.Definition.Name}.yml");
							if (!File.Exists(configFile)) throw new FileNotFoundException("Unable to find plugin configuration file", configFile);

							object config = Load(plugin.Definition.Name, configurationType);

							constructorArgs.Add(config);
						}

						var controller = (Controller)Activator.CreateInstance(controllerType, constructorArgs.ToArray());

						this.controllers.Add(controller);
					}
				}
			}

			this.logger.Info($"Plugins loaded, {this.controllers.Count} controller(s) created");
		}

		public static object Load(string name, Type type)
		{
			Deserializer deserializer = new DeserializerBuilder()
				.WithNamingConvention(new CamelCaseNamingConvention())
				//.IgnoreUnmatchedProperties()
				.Build();

			return deserializer.Deserialize(File.ReadAllText(Path.Combine("config", $"{name}.yml")), type);
		}

		public static T Load<T>(string name)
		{
			return (T)Load(name, typeof(T));
		}
	}
}
