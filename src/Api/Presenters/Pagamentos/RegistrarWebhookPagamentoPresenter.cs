using Api.Extensions.Markers;
using Application.Pagamentos.UseCases.RegistrarWebhookPagamento;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Pagamentos;

public class RegistrarWebhookPagamentoPresenter : IRegistrarWebhookPagamentoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }

    public void PayloadInvalido(string mensagem) =>
        Result = new BadRequestObjectResult(new { erro = mensagem });

    public void AssinaturaInvalida() =>
        Result = new UnauthorizedResult();

    public void NaoEncontrado() =>
        // 200 (não 404): o Mercado Pago reenvia notificações que retornam erro. Um
        // orçamento/pagamento correspondente pode ainda não existir por uma condição de
        // corrida bem rara; não há nada de errado com a notificação em si.
        Result = new OkResult();

    public void Processado() =>
        Result = new OkResult();
}
