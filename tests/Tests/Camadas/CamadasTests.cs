using FluentAssertions;
using NetArchTest.Rules;

namespace Tests.Camadas;

public class CamadasTests : CamadasBaseTest
{
    [Fact]
    public void CamadaDomain_NaoDeveTerDependencia_CamadaApplication()
    {
        TestResult resultado = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Domain não deve depender da camada Application. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void CamadaDomain_NaoDeveTerDependencia_CamadaInfrastructure()
    {
        TestResult resultado = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Domain não deve depender da camada Infrastructure. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void CamadaDomain_NaoDeveTerDependencia_CamadaApresentacao()
    {
        TestResult resultado = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Domain não deve depender da camada de Apresentação. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void CamadaApplication_NaoDeveTerDependencia_CamadaInfrastructure()
    {
        TestResult resultado = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Application não deve depender da camada Infrastructure. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void CamadaApplication_NaoDeveTerDependencia_CamadaApresentacao()
    {
        TestResult resultado = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Application não deve depender da camada de Apresentação. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void CamadaInfrastructure_NaoDeveTerDependencia_CamadaApresentacao()
    {
        TestResult resultado = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "A camada Infrastructure não deve depender da camada de Apresentação. Violações:\n" +
                     string.Join("\n", resultado.FailingTypeNames ?? []));
    }
}
