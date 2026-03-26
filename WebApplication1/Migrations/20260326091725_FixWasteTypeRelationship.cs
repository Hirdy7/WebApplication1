using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class FixWasteTypeRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WasteTypeId1",
                table: "DisposalPointWasteTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisposalPointWasteTypes_WasteTypeId1",
                table: "DisposalPointWasteTypes",
                column: "WasteTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DisposalPointWasteTypes_WasteTypes_WasteTypeId1",
                table: "DisposalPointWasteTypes",
                column: "WasteTypeId1",
                principalTable: "WasteTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisposalPointWasteTypes_WasteTypes_WasteTypeId1",
                table: "DisposalPointWasteTypes");

            migrationBuilder.DropIndex(
                name: "IX_DisposalPointWasteTypes_WasteTypeId1",
                table: "DisposalPointWasteTypes");

            migrationBuilder.DropColumn(
                name: "WasteTypeId1",
                table: "DisposalPointWasteTypes");
        }
    }
}
