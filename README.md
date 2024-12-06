![Nuget](https://img.shields.io/nuget/v/Unicorn.Reporting.Allure?style=plastic) ![Nuget](https://img.shields.io/nuget/dt/Unicorn.Reporting.Allure?style=plastic)

# Allure agent

Unicorn has ability to generate powerful test results report using [Allure Framework](https://docs.qameta.io/allure/)

Just deploy Allure Framework instance, add tests project dependency to [Unicorn.Reporting.Allure](https://www.nuget.org/packages/Unicorn.Reporting.Allure) package and initialize reporter during tests assembly initialization.
***
Place **allureConfig.json** configuration file to directory with test assemblies. Sample content is presented below:
```json
{
    "allure": {
        "directory": "path_to_directory_with_report"
    }
}
```
then add code with reporting initialization to `[TestsAssembly]`
```csharp
using Unicorn.Taf.Api;
using Unicorn.Core.Testing.Tests.Attributes;
using Unicorn.Reporting.Allure;

namespace Tests
{
    [TestsAssembly]
    public static class TestsAssembly
    {
        private static ITestReporter reporter;

        [RunInitialize]
        public static void InitRun()
        {
            reporter = new AllureReporter(); // starts new launch in Allure.
        }

        [RunFinalize]
        public static void FinalizeRun()
        {
            reporter.Dispose(); // Unsubscribe allure reporter from unicorn events.
            reporter = null;
        }
    }
}  
```
