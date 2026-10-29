using GestionOrdinateurs.API;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionOrdinateurs.API.Migrations;

[DbContext(typeof(ParcInformatiqueContext))]
[Migration("20261013114300_UpdateEtatOrdinateurValues")]
public class UpdateEtatOrdinateurValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Ordinateur"
            SET "Etat" = CASE "Etat"
                WHEN 1 THEN 2
                WHEN 2 THEN 3
                ELSE "Etat"
            END
            WHERE "Etat" IN (1, 2);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Ordinateur"
            SET "Etat" = CASE "Etat"
                WHEN 2 THEN 1
                WHEN 3 THEN 2
                ELSE "Etat"
            END
            WHERE "Etat" IN (2, 3);
            """);
    }
}
