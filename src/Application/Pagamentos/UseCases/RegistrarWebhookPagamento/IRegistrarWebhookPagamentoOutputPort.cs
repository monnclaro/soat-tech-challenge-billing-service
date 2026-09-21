namespace Application.Pagamentos.UseCases.RegistrarWebhookPagamento;

public interface IRegistrarWebhookPagamentoOutputPort
{
    void PayloadInvalido(string mensagem);
    void AssinaturaInvalida();
    void NaoEncontrado();
    void Processado();
}
