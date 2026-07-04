using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dojo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeInvoiceVoidAndTransactionRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RefundOfTransactionId",
                table: "IncomeTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "IncomeTransactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundedByEmail",
                table: "IncomeTransactions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundedByName",
                table: "IncomeTransactions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundedOn",
                table: "IncomeTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Status",
                table: "IncomeTransactions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "IncomeInvoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedByEmail",
                table: "IncomeInvoices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedByName",
                table: "IncomeInvoices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedOn",
                table: "IncomeInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_RefundOfTransactionId",
                table: "IncomeTransactions",
                column: "RefundOfTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_RefundOfTransactionId",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "RefundOfTransactionId",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "RefundedByEmail",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "RefundedByName",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "RefundedOn",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "IncomeInvoices");

            migrationBuilder.DropColumn(
                name: "VoidedByEmail",
                table: "IncomeInvoices");

            migrationBuilder.DropColumn(
                name: "VoidedByName",
                table: "IncomeInvoices");

            migrationBuilder.DropColumn(
                name: "VoidedOn",
                table: "IncomeInvoices");
        }
    }
}
