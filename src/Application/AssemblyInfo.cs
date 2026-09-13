using System.Runtime.CompilerServices;

// Os handlers de domain event (Application.Pagamentos.EventHandlers.*) são internal ao
// assembly por padrão (só o Scrutor, via publicOnly: false em DependencyInjection, precisa
// enxergá-los em runtime) — o projeto de testes precisa da mesma visibilidade para
// instanciá-los diretamente nos testes unitários.
[assembly: InternalsVisibleTo("Tests")]
