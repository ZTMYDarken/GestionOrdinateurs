namespace GestionOrdinateurs.Shared;

public class Caracteristiques
{
    public int RamGo { get; set; }
    public TypeOrdinateur TypeOrdi { get; set; }
    public int StockageGo { get; set; }

    public Caracteristiques(int ramGo, TypeOrdinateur typeOrdi, int stockageGo){
        RamGo = ramGo;
        TypeOrdi = typeOrdi;
        StockageGo = stockageGo;
    }
}
