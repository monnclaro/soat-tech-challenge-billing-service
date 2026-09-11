namespace Application.Orcamentos.UseCases.GerarOrcamento;

public interface IGerarOrcamentoOutputPort
{
    void ValorDivergente(string mensagem);
    void OrcamentoJaExiste(string mensagem);
    void Ok(OrcamentoOutput output);
}
