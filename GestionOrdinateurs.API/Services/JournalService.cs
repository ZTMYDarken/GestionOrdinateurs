using System.Globalization;
using GestionOrdinateurs.Shared;

namespace GestionOrdinateurs.API.Services;

public class JournalService
{
    private readonly string _repertoireJournal;
    private readonly object _verrou = new();

    public JournalService(IWebHostEnvironment environment)
        : this(environment.ContentRootPath)
    {
    }

    public JournalService(string contentRootPath)
    {
        _repertoireJournal = Path.Combine(
            contentRootPath,
            "Logs",
            "Reparations_Logs",
            "Ordinateurs");
    }

    public void Ecrire(string message)
    {
        EcrireDansFichier("reparations.log", message);
    }

    public void EcrireActionOrdinateur(string action, Ordinateur ordinateur)
    {
        var dateJour = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        EcrireDansFichier(
            "ordinateurs.log",
            $"{dateJour} - Ordinateur {ordinateur.Id} ({ordinateur.Nom}) : {action}");
    }

    private void EcrireDansFichier(string nomFichier, string message)
    {
        lock (_verrou)
        {
            Directory.CreateDirectory(_repertoireJournal);

            using var writer = new StreamWriter(
                Path.Combine(_repertoireJournal, nomFichier),
                append: true);

            writer.WriteLine(message);
        }
    }
}
