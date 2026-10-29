using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionOrdinateurs.Shared;

[Table("Ordinateur")]
public class Ordinateur
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public EtatOrdinateur Etat { get; set; }
    public bool Vendu { get; set; }

    [DefaultValue(0)]
    public int Stock { get; set; }

    [DefaultValue(0)]
    public decimal Prix { get; set; }

    public Caracteristiques Caracteristiques { get; set; } = null!;

    public List<Reparation> Reparations { get; set; } = new();
}
