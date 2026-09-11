using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InicialBillingService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orcamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_ordem_servico = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_orcamento = table.Column<Guid>(type: "uuid", nullable: false),
                    preference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_item_origem = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_item = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    id_orcamento = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_orcamento_item_orcamento_id_orcamento",
                        column: x => x.id_orcamento,
                        principalTable: "orcamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_id_ordem_servico",
                table: "orcamento",
                column: "id_ordem_servico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_item_id_orcamento",
                table: "orcamento_item",
                column: "id_orcamento");

            migrationBuilder.CreateIndex(
                name: "IX_pagamento_id_orcamento",
                table: "pagamento",
                column: "id_orcamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagamento_preference_id",
                table: "pagamento",
                column: "preference_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento_item");

            migrationBuilder.DropTable(
                name: "pagamento");

            migrationBuilder.DropTable(
                name: "orcamento");
        }
    }
}
