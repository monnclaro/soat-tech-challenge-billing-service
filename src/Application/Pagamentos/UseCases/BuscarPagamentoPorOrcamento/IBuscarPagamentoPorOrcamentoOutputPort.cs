namespace Application.Pagamentos.UseCases.BuscarPagamentoPorOrcamento;

public interface IBuscarPagamentoPorOrcamentoOutputPort
{
    void NaoEncontrado();
    void Ok(PagamentoOutput output);
}
