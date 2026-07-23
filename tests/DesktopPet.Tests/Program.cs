var suites = Array.Empty<Action>();
var failed = 0;

foreach (var suite in suites)
{
    try
    {
        suite();
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine(ex);
    }
}

Console.WriteLine($"{suites.Length - failed}/{suites.Length} suites passed");
return failed == 0 ? 0 : 1;
