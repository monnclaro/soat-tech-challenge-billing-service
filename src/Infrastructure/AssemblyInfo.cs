using System.Runtime.CompilerServices;

// DomainEventsDispatcher é internal ao assembly — o projeto de testes precisa da mesma
// visibilidade para instanciá-lo diretamente nos testes unitários.
[assembly: InternalsVisibleTo("Tests")]
