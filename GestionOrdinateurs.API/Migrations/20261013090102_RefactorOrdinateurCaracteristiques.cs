using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionOrdinateurs.API.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOrdinateurCaracteristiques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reparations_Ordinateurs_UnOrdinateurId",
                table: "Reparations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ordinateurs",
                table: "Ordinateurs");

            migrationBuilder.DropColumn(
                name: "Marque",
                table: "Ordinateurs");

            migrationBuilder.DropColumn(
                name: "Processeur",
                table: "Ordinateurs");

            migrationBuilder.RenameTable(
                name: "Ordinateurs",
                newName: "Ordinateur");

            migrationBuilder.RenameColumn(
                name: "RamGo",
                table: "Ordinateur",
                newName: "Caracteristiques_RamGo");

            migrationBuilder.AlterColumn<int>(
                name: "Stock",
                table: "Ordinateur",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Caracteristiques_StockageGo",
                table: "Ordinateur",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Caracteristiques_TypeOrdi",
                table: "Ordinateur",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ordinateur",
                table: "Ordinateur",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reparations_Ordinateur_UnOrdinateurId",
                table: "Reparations",
                column: "UnOrdinateurId",
                principalTable: "Ordinateur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reparations_Ordinateur_UnOrdinateurId",
                table: "Reparations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ordinateur",
                table: "Ordinateur");

            migrationBuilder.DropColumn(
                name: "Caracteristiques_StockageGo",
                table: "Ordinateur");

            migrationBuilder.DropColumn(
                name: "Caracteristiques_TypeOrdi",
                table: "Ordinateur");

            migrationBuilder.RenameTable(
                name: "Ordinateur",
                newName: "Ordinateurs");

            migrationBuilder.RenameColumn(
                name: "Caracteristiques_RamGo",
                table: "Ordinateurs",
                newName: "RamGo");

            migrationBuilder.AlterColumn<int>(
                name: "Stock",
                table: "Ordinateurs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Marque",
                table: "Ordinateurs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Processeur",
                table: "Ordinateurs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ordinateurs",
                table: "Ordinateurs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reparations_Ordinateurs_UnOrdinateurId",
                table: "Reparations",
                column: "UnOrdinateurId",
                principalTable: "Ordinateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
