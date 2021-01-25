using FluentValidation.Results;
using MediatR;
using NSE.Clientes.API.Application.Events;
using NSE.Clientes.API.Models;
using NSE.Core.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace NSE.Clientes.API.Application.Commands
{
    public class ClienteCommandHandler : CommandHandler, IRequestHandler<RegistrarClienteCommand, ValidationResult>
    {
        private readonly IClienteRepository _clienteRespository;

        public ClienteCommandHandler(IClienteRepository clienteRespository)
        {
            _clienteRespository = clienteRespository;
        }
        
        public async Task<ValidationResult> Handle(RegistrarClienteCommand message, CancellationToken cancellationToken)
        {
            if (!message.EhValido()) return message.ValidationResult;

            var cliente = new Cliente(message.Id, message.Nome, message.Email, message.Cpf);

            var clienteExistente = await _clienteRespository.ObterPorCpf(cliente.Cpf.Numero);
            if(clienteExistente != null)
            {
                AdicionarErro("Cliente já existe");
                return ValidationResult;
            }

            _clienteRespository.Adicionar(cliente);

            cliente.AdicionarEvento(new ClienteRegistradoEvent(message.Id, message.Nome, message.Email, message.Cpf));
            
            return await PersistirDados(_clienteRespository.UnitOfWork);
        }
    }
}
