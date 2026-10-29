namespace GestionOrdinateurs.API.Services;

public class StockService
{
    private readonly object _stockLock = new();

    public int StockSSD { get; private set; } = 10;

    public async void ConsommerSSD()
    {
        Task.Delay(10).Wait();
        lock (_stockLock)
        {
            StockSSD--;
        }
    }
}
