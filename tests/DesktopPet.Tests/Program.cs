using DesktopPet.Tests;

var suites = new Action[]
{
    PetStateMachineTests.Run,
    GestureClassifierTests.Run,
    EyeConstraintTests.Run,
    SettingsJsonTests.Run,
    ReminderDefinitionTests.Run,
    ReminderPersistenceTests.Run,
    ReminderSchedulerTests.Run,
    ReminderPresentationTests.Run,
    DropBatchTests.Run,
    ShellDeleteResultMapperTests.Run,
    CharacterAnimationMathTests.Run
};
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
