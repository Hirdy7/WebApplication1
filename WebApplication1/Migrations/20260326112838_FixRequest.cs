using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class FixRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WasteTypeId",
                table: "DisposalRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRequests_WasteTypeId",
                table: "DisposalRequests",
                column: "WasteTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisposalRequests_WasteTypes_WasteTypeId",
                table: "DisposalRequests",
                column: "WasteTypeId",
                principalTable: "WasteTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisposalRequests_WasteTypes_WasteTypeId",
                table: "DisposalRequests");

            migrationBuilder.DropIndex(
                name: "IX_DisposalRequests_WasteTypeId",
                table: "DisposalRequests");

            migrationBuilder.DropColumn(
                name: "WasteTypeId",
                table: "DisposalRequests");
        }
    }
}
