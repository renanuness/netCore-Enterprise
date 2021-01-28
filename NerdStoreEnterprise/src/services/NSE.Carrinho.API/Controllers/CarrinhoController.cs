using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSE.Carrinho.API.Data;
using NSE.Carrinho.API.Model;
using NSE.WebAPI.Core.Controllers;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Threading.Tasks;

namespace NSE.Carrinho.API.Controllers
{
    [Authorize]
    public class CarrinhoController : MainController
    {
        private readonly IAspNetUser _user;
        private readonly CarrinhoContext _contexto;

        public CarrinhoController(IAspNetUser user, CarrinhoContext context)
        {
            _user = user;
            _contexto = context;
        }

        [HttpGet("carrinho")]
        public async Task<CarrinhoCliente> ObterCarrinho()
        {
            return await ObterCarrinhoCliente() ?? new CarrinhoCliente();
        }

        [HttpPost("carrinho")]
        public async Task<IActionResult> AdicionarItemCarrinho(CarrinhoItem item)
        {
            var carrinho = await ObterCarrinhoCliente();

            if (carrinho == null)
                ManipularNovoCarrinho(item);
            else
                ManipularCarrinhoExistente(carrinho, item);

            if (!OperacaoValida()) return CustomResponse();

            var result = await _contexto.SaveChangesAsync();
            if (result <= 0) AdicionarErroProcessamento("Não foi possível persistir os dados no banco");
           
            return CustomResponse();
        }

        [HttpPut("carrinho/{produtoId}")]
        public async Task<IActionResult> AtualizarItemCarrinho(Guid produtoId, CarrinhoItem item)
        {
            var carrinho = await ObterCarrinhoCliente();
            var itemCarrinho = await ObterItemCarrinhoItemValidado(produtoId, carrinho, item);

            if(itemCarrinho == null) return CustomResponse();

            carrinho.AtualizarUnidades(itemCarrinho, item.Quantidade);

            _contexto.CarrinhoItens.Update(itemCarrinho);
            _contexto.CarrinhoCliente.Update(carrinho);

            await PersistirDados();
            return CustomResponse();
        }

        [HttpDelete("carrinho/{produtoId}")]
        public async Task<IActionResult> RemoverItemCarrinho(Guid produtoId)
        {
            var carrinho = await ObterCarrinhoCliente();

            var itemCarrinho = await ObterItemCarrinhoItemValidado(produtoId, carrinho);
            if (itemCarrinho == null) return CustomResponse();

            carrinho.RemoverItem(itemCarrinho);

            _contexto.CarrinhoItens.Remove(itemCarrinho);
            _contexto.CarrinhoCliente.Update(carrinho);

            await PersistirDados();

            return CustomResponse();
        }

        private async Task<CarrinhoCliente> ObterCarrinhoCliente()
        {
            return await _contexto.CarrinhoCliente
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.ClienteId == _user.ObterUserId());
        }
        private void ManipularNovoCarrinho(CarrinhoItem item)
        {
            var carrinho = new CarrinhoCliente(_user.ObterUserId());
            carrinho.AdicionarItem(item);

            _contexto.CarrinhoCliente.Add(carrinho);
        }
        private void ManipularCarrinhoExistente(CarrinhoCliente carrinho, CarrinhoItem item)
        {
            var produtoExistente = carrinho.CarrinhoItemExistente(item);

            carrinho.AdicionarItem(item);

            if (produtoExistente)
            {
                _contexto.CarrinhoItens.Update(carrinho.ObterPorProdutoId(item.ProdutoId));
            }
            else
            {
                _contexto.CarrinhoItens.Add(item);
            }

            _contexto.CarrinhoCliente.Update(carrinho);
        }
        private async Task<CarrinhoItem> ObterItemCarrinhoItemValidado(Guid produtoId, CarrinhoCliente carrinho, CarrinhoItem item = null)
        {
            if(item != null && produtoId != item.ProdutoId)
            {
                AdicionarErroProcessamento("O item não corresponde ao informado");
                return null;
            }

            if(carrinho == null)
            {
                AdicionarErroProcessamento("Carrinho não encontrado");
                return null;
            }

            var itemCarrinho = await _contexto.CarrinhoItens
                .FirstOrDefaultAsync(i => i.CarrinhoId == carrinho.Id && i.ProdutoId == produtoId);

            if(itemCarrinho == null || !carrinho.CarrinhoItemExistente(itemCarrinho))
            {
                AdicionarErroProcessamento("O item não está no carrinho");
                return null;
            }

            return itemCarrinho;
        }
    
        private async Task PersistirDados()
        {
            var result = await _contexto.SaveChangesAsync();
            if (result <= 0) AdicionarErroProcessamento("Não foi possível persistir os dados no banco");
        }
    }
}
