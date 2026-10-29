using GestionOrdinateurs.Shared;

namespace GestionOrdinateurs.Events;

public class OrdinateurMisAJourEventArgs : EventArgs
{
    public Ordinateur Ordinateur { get; }

    public OrdinateurMisAJourEventArgs(Ordinateur ordinateur)
    {
        Ordinateur = ordinateur;
    }
}