namespace Application.Pagamentos.UseCases.BuscarPagamento;

public interface IBuscarPagamentoOutputPort
{
    void NaoEncontrado();
    void Ok(PagamentoOutput output);
}
