using System.Reflection;
using Application;
using Domain.Orcamentos;
using Infrastructure.Database;

namespace Tests.Camadas;

public abstract class CamadasBaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(Orcamento).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(BillingServiceDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}
