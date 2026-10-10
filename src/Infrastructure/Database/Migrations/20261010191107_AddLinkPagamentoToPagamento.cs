using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    // Migration gerada pelo EF Core (dotnet ef migrations add) — schema puro, sem regra de
    // negócio nem branching; testá-la significaria recriar o Postgres real via Testcontainers
    // só para exercitar chamadas de CreateTable, sem nenhum ganho de cobertura de lógica.
    [ExcludeFromCodeCoverage]
    public partial class AddLinkPagamentoToPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "link_pagamento",
                table: "pagamento",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "link_pagamento",
                table: "pagamento");
        }
    }
}
