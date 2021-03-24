using System;
using System.Collections.Concurrent;
using System.IO;
using ElectronCgi.DotNet;
using LettuceIo.Dotnet.Base;
using LettuceIo.Dotnet.Core.Enums;
using LettuceIo.Dotnet.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;

namespace LettuceIo.Dotnet.ConsoleHost
{
    internal static class Program
    {
        private static readonly ConcurrentDictionary<string, IAction> ActiveActions =
            new ConcurrentDictionary<string, IAction>();

        private static readonly Connection Connection = new ConnectionBuilder().Build();
        
        private static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
                
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            Log.Information("Lettuce.IO up and running!");
            
            Connection.On<JToken, bool>("NewAction", NewAction);
            Connection.On<string>("TerminateAction", TerminateAction);
            Connection.Listen();
        }
        
        private static bool NewAction(JToken settings)
        {
            var id = settings.Value<string>("id");
            Log.Debug($"New action {id}");
            if (ActiveActions.ContainsKey(id))
                try { throw new Exception($"Key \"{id}\" already exists in the dictionary"); }
                catch (Exception e) { Log.Error($"Action resolved with an Exception:\n{e}"); }
            var action = new ActionFactory().Configure(settings).CreateAction();
            if (!ActiveActions.TryAdd(id, action))
                try { throw new Exception($"Key \"{id}\" already exists in the dictionary"); }
                catch (Exception e) { Log.Error($"Action resolved with an Exception:\n{e}"); }            
            action.Metrics.Subscribe(
                metrics => Connection.Send(id, JObject.FromObject(new {metrics})),
                error =>
                {
                    Log.Error($"Action resolved with an Exception:\n{error}");
                    TerminateAction(id);
                    Connection.Send(id, JObject.FromObject(new {error, metrics = new {isActive = false}}));
                },
                () =>
                {
                    Log.Information($"Action {id} completed successfully");
                    TerminateAction(id);
                    Connection.Send(id, JObject.FromObject(new {metrics = new {isActive = false}}));
                });
            try
            {
                Log.Information($"{id} Action started");
                action.Start();
            }
            catch (Exception e)
            {
                Log.Error($"Action resolved with an Exception:\n{e}");
                TerminateAction(id);
                throw e;
            }

            return true;
        }

        private static void TerminateAction(string id)
        {
            if (ActiveActions.TryRemove(id, out var action) && action.Status != Status.Stopped) action.Stop();
            Log.Information($"Action {id} as been terminated");
        }
    }
}