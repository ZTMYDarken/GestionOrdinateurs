using GestionOrdinateurs.Shared;

namespace GestionOrdinateurs.Events;

public class OrdinateurAjouteEventArgs : EventArgs
{
    public Ordinateur Ordinateur { get; }

    public OrdinateurAjouteEventArgs(Ordinateur ordinateur)
    {
        Ordinateur = ordinateur;
    }
}