namespace Application.Orcamentos.UseCases.BuscarOrcamento;

public interface IBuscarOrcamentoOutputPort
{
    void NaoEncontrado();
    void Ok(OrcamentoOutput output);
}
