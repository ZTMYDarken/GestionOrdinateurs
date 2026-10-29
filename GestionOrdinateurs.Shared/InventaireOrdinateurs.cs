using System.Collections;

namespace GestionOrdinateurs.Shared;

public class InventaireOrdinateurs : IReadOnlyCollection<Ordinateur>
{
    public List<Ordinateur> Ordinateurs { get; set; } = new();

    public int Count => Ordinateurs.Count;

    // Recherche un ordinateur par son identifiant.
    public Ordinateur this[int ordinateurId] =>
        Ordinateurs.FirstOrDefault(ordinateur => ordinateur.Id == ordinateurId)
        ?? throw new KeyNotFoundException(
            $"Aucun ordinateur trouvé avec l'ID {ordinateurId}.");

    // Recherche un ordinateur par son nom, sans tenir compte de la casse.
    public Ordinateur this[string nom] =>
        Ordinateurs.FirstOrDefault(
            ordinateur => string.Equals(
                ordinateur.Nom,
                nom,
                StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException(
            $"Aucun ordinateur trouvé avec le nom « {nom} ».");

    public void Add(Ordinateur ordinateur)
    {
        Ordinateurs.Add(ordinateur);
    }

    public IEnumerator<Ordinateur> GetEnumerator() => Ordinateurs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
