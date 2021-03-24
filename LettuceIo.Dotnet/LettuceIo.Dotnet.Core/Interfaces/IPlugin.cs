using System;
using RabbitMQ.Client;

namespace LettuceIo.Dotnet.Core.Interfaces
{
    public interface IPlugin : IDisposable
    {
        public void PassConnection(IConnection connection);
        public void DeclareInput();
        public void DeclareOutput();
        public void Start();
    }
}