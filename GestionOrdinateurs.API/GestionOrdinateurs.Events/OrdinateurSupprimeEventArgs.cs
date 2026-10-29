using GestionOrdinateurs.Shared;

namespace GestionOrdinateurs.Events;

public class OrdinateurSupprimeEventArgs : EventArgs
{
    public Ordinateur Ordinateur { get; }

    public OrdinateurSupprimeEventArgs(Ordinateur ordinateur)
    {
        Ordinateur = ordinateur;
    }
}