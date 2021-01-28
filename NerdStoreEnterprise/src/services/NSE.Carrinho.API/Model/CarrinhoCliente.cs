using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NSE.Carrinho.API.Model
{
    public class CarrinhoCliente
    {
        internal const int MAX_QUANTIDADE_ITEM = 5;
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public decimal ValorTotal { get; set; }
        public List<CarrinhoItem> Itens { get; set; } = new List<CarrinhoItem>();
        public CarrinhoCliente(Guid clienteId)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
        }

        //EF
        public CarrinhoCliente() { }

        internal void CalcularValorCarrinho()
        {
            ValorTotal = Itens.Sum(p => p.CalcularValor());
        }

        internal bool CarrinhoItemExistente(CarrinhoItem item)
        {
            return Itens.Any(p => p.ProdutoId == item.ProdutoId);
        }

        internal CarrinhoItem ObterPorProdutoId(Guid produtoId)
        {
            return Itens.FirstOrDefault(p => p.ProdutoId == produtoId);
        }

        internal void AdicionarItem(CarrinhoItem item)
        {
            if (!item.EhValido()) return;
            item.AssociarCarrinho(item.Id);

            if (CarrinhoItemExistente(item))
            {
                var itemExistente = ObterPorProdutoId(item.ProdutoId);
                itemExistente.AdicionarUnidades(item.Quantidade);

                item = itemExistente;
                Itens.Remove(itemExistente);
            }
            Itens.Add(item);
            CalcularValorCarrinho();
        }

        internal void AtualizarItem(CarrinhoItem item)
        {
            if (!item.EhValido()) return;
            item.AssociarCarrinho(Id);

            var itemExistente = ObterPorProdutoId(item.ProdutoId);

            Itens.Remove(itemExistente);
            Itens.Add(item);

            CalcularValorCarrinho();
        }

        internal void AtualizarUnidades(CarrinhoItem item, int unidades)
        {
            item.AtualizarUnidades(unidades);
            AtualizarItem(item);
        }

        internal void RemoverItem(CarrinhoItem item)
        {
            var itemExistente = ObterPorProdutoId(item.ProdutoId);
            Itens.Remove(itemExistente);

            CalcularValorCarrinho();
        }

        public class CarrinhoClienteValidation : AbstractValidator<CarrinhoCliente>
        {

        }
    }
}
