using System;
using System.Threading.Tasks;

namespace Calculator.Services;

public class EasterEggService
{
    private bool _isHackerModeActive = false;

    public async Task CheckAndTrigger1337Async(double result, Action<bool, string> updateStateCallback)
    {
        if (result == 1337 && !_isHackerModeActive)
        {
            _isHackerModeActive = true;
            
            updateStateCallback(true, "L33T H4X0R");

            await Task.Delay(3000);

            updateStateCallback(false, "1337");
            _isHackerModeActive = false;
        }
    }
}