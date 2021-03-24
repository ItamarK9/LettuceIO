using System;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;

namespace LettuceIo.Dotnet.Core.Interfaces
{
    public interface IPlugin : IDisposable
    {
        public void SetConnection(string rabbitmqHost);
        public void DeclareTopology(string inExchange, string outExchange);
        public string Start(string rabbitmqHost, string outExchange);
        public void OnMessage(JObject message);

    }
}